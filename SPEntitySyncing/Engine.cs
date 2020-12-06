using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBreeze;
using DBreeze.Utils;

namespace EntitySyncing
{
    public enum eSynchroDirectionType
    {
        FromClient,
        FromServer,
        Both
    }


    public class Engine
    {
        internal DBreezeEngine DBEngine = null;


        public Engine(ILogger logger, DBreeze.DBreezeEngine dbEngine)
        {
            Logger.log = logger;

            if (dbEngine == null)
            {
                Logger.LogException("EntitySyncing.Engine", "Init", new Exception("DBreezeEngine is not specified"), "");
                return;
            }

            DBEngine = dbEngine;
        }

        public void Init(ILogger logger, DBreeze.DBreezeEngine dbEngine)
        {
            Logger.log = logger;

            if (dbEngine == null)
            {
                Logger.LogException("EntitySyncing.Engine", "Init", new Exception("DBreezeEngine is not specified"), "");
                return;
            }

            DBEngine = dbEngine;
        }

        /// <summary>
        /// Fills up index 200. Entity desired ID and SyncTimestamp must be specified.
        /// Creation of transaction, synchronization of the table and transaction commit is outside of this function.
        /// </summary>
        /// <param name="table">where index 200 must be stored </param>
        /// <param name="entity">entity.Id and entity.SyncTimestamp must be filled up</param>
        /// <param name="ptrEntityContent">pointer to the entity content (16 bytes) gathered with DBreeze InsertDataBlockWithFixedAddress</param>
        public void InsertIndex4Sync(DBreeze.Transactions.Transaction tran, string table, ISyncEntity entity, byte[] ptrEntityContent, ISyncEntity oldEntity)
        {
            if (oldEntity == null)
                tran.Insert<byte[], byte[]>(table, 200.ToIndex(entity.Id), ptrEntityContent);

            tran.Insert<byte[], byte[]>(table, 201.ToIndex(entity.SyncTimestamp, entity.Id), null);
        }
     

        /// <summary>
        /// V2
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="capsIn"></param>
        /// <param name="entitySync"></param>
        /// <param name="user"></param>
        /// <param name="oneWaySynchronizer">only from client to server</param>
        /// <param name="entityMustBeReturnedBackToClientAfterCreation"></param>
        /// <param name="oneWaySynchronizerFromServer">only from server to client</param>
        /// <returns></returns>
        public HttpCapsule SyncEntityV1<T>(HttpCapsule capsIn, EntitySyncingBaseV1<T> entitySync, object userToken,
            eSynchroDirectionType syncDirection = eSynchroDirectionType.Both, bool entityMustBeReturnedBackToClientAfterCreation = false)
        {//SynchronizeEntityWithUID_V2
            HttpCapsule httpCapsule = new HttpCapsule();
            try
            {
                string entityType = typeof(T).FullName;

                Dictionary<string, byte[]> toServer = capsIn.Body.DeserializeProtobuf<Dictionary<string, byte[]>>();

                long LastServerSyncTimeStamp = toServer["LastServerSyncTimeStamp"].To_Int64_BigEndian();
                List<SyncOperation> syncLst
                    = toServer["SyncLst"].DeserializeProtobuf<List<SyncOperation>>();

                httpCapsule.Type = "{'Result':'OK'}";

                //Getting user
                //var user = MCacheSupport.GetUser(userId);

                //int limitationOfEntitesPerRound = 1000;
                int limitationOfEntitesPerRound = 10000;
                bool repeatSynchro = false;
                T newEntity;
                T existingEntity;

                //Defining timestamp for current transaction //It must be also returned back to the client
                long syncTimestamp = DateTime.UtcNow.Ticks; //Is set after SynchronizeTables again
                long newServerSync = 0;

                //Key is UID of the Entity
                Dictionary<long, SyncOperation> returnBackOperations = new Dictionary<long, SyncOperation>();
                HashSet<long> nonReturningBackEntites = new HashSet<long>();
                SyncOperation newSyncOper = null;

                Dictionary<long, long> srvSyncList = new Dictionary<long, long>(); //Stores backward Key is Entity UID and Value is SyncTimestamp  

                
                using (var tran = DBEngine.GetTransaction())
                {
                    //entitySync.SetSyncEntitesList(syncLst);
                    entitySync.tran = tran;
                    entitySync.userToken = userToken;
                    entitySync.Init();
                    //entitySync.Init(tran, syncLst, user);

                    //Synchronization of tables is inside of Init                   

                    syncTimestamp = DateTime.UtcNow.Ticks;

                    tran.ValuesLazyLoadingIsOn = false;

                    //Speeding up is off currently
                    //tran.Technical_SetTable_OverwriteIsNotAllowed(entitySync.entityTable);

                    ////EntityTable
                    //var et = tran.InsertTable<int>(entitySync.entityTable, 1, 0); //Index starts from byte 200
                    ////Synchro Table
                    //var st = tran.InsertTable<int>(entitySync.entityTable, 2, 0); //Index starts from byte 201

                    //_M_WDA._Log.LogInformation("GM_PersonalDevice.IDoThings", "SynchronizeEntityWithUID_V2", "Starting sync Task: " + typeof(T).FullName + "__" + (typeof(T) == typeof(GM_IDoThings.Task)).ToString(), "");                   

                    //Analyzing entites came from the client (Already limited quantity from client-side)
                    foreach (var row in syncLst)
                    {
                        if (row.SyncTimestamp > syncTimestamp)
                        {
                            //Clocks are quite forward on the client

                            //If diff is more then 20 seconds, we correct incoming time from client
                            if (Math.Abs(new DateTime(row.SyncTimestamp).Subtract(new DateTime(syncTimestamp)).TotalSeconds) > 20)
                                row.SyncTimestamp = syncTimestamp;

                        }

                        if (syncDirection != eSynchroDirectionType.FromServer)
                        {
                            switch (row.Operation)
                            {

                                case SyncOperation.eOperation.INSERT:

                                    //Checking if we got such row (row.InternalId must be always more then 0, it,s UID)
                                    var rowExistingEntity = tran.Select<byte[], byte[]>(entitySync.entityTable, new byte[] { 200 }.Concat(row.InternalId.To_8_bytes_array_BigEndian()));
                                    if (rowExistingEntity.Exists && row.ExternalId > 0)
                                    {

                                        //Possible update
                                        //existingEntity = rowExistingEntity.Value;
                                        entitySync.ptrContent = rowExistingEntity.Value;
                                        existingEntity = tran.SelectDataBlockWithFixedAddress<T>(entitySync.entityTable, rowExistingEntity.Value);

                                        if (((ISyncEntity)existingEntity).SyncTimestamp >= row.SyncTimestamp)
                                        {
                                            //Server-side entity is stronger then client-side, we do nothing (new data will come with from server-sync)   
                                            //adding this row to synchro list, to be returned back                                        
                                            srvSyncList[row.InternalId] = ++syncTimestamp;
                                            //_M_WDA._Log.LogInformation("GM_PersonalDevice.IDoThings", "SynchronizeEntityWithUID", "entity is stronger", "");

                                        }
                                        else
                                        {

                                            //Server-side entity is weaker then client-side
                                            //We must update server side and put into entityLog
                                            //We don't return back this entity


                                            newEntity = row.SerializedObject.DeserializeProtobuf<T>();
                                            //et.Insert<long, T>(row.InternalId, newEntity);

                                            ((ISyncEntity)newEntity).Id = row.InternalId; //just for a case
                                            ((ISyncEntity)newEntity).SyncTimestamp = row.SyncTimestamp;


                                            //if (entitySync.OnInsertEntity(row.InternalId, newEntity, existingEntity, row.SyncTimestamp))
                                            if (entitySync.OnInsertEntity(newEntity, existingEntity))
                                            {

                                                //Only in case if business logic allows us to apply newly incoming entity, we do that

                                                //Removing previous version of the row from synchro table (that newly connected user would not download one row whole history)
                                                //st.RemoveKey<long>(((ISyncEntity)existingEntity).SyncTimestamp);
                                                tran.RemoveKey(entitySync.entityTable, new byte[] { 201 }.ConcatMany(
                                                    ((ISyncEntity)existingEntity).SyncTimestamp.To_8_bytes_array_BigEndian(),
                                                    ((ISyncEntity)existingEntity).Id.To_8_bytes_array_BigEndian()
                                                    ));
                                                //Inserting into sync table this row                                            
                                                //st.Insert<long, long>(row.SyncTimestamp, row.InternalId);
                                                //tran.Insert<byte[], long>(entitySync.entityTable, new byte[] { 201 }.Concat(row.SyncTimestamp.To_8_bytes_array_BigEndian()), row.InternalId);
                                                tran.Insert<byte[], byte[]>(entitySync.entityTable, new byte[] { 201 }.ConcatMany(
                                                    row.SyncTimestamp.To_8_bytes_array_BigEndian(),
                                                    row.InternalId.To_8_bytes_array_BigEndian()
                                                    ), null);// entitySync.refToValueDataBlockWithFixedAddress);

                                                nonReturningBackEntites.Add(row.InternalId);  //Strong ID which we must ignored for sending
                                            }
                                            //else
                                            //{
                                            //    //Server side entity is stronger then client-side, we do nothing (new data will come with from server-sync)     
                                            //}

                                        }

                                    }
                                    else
                                    {
                                       

                                        //Insert new 
                                        newEntity = row.SerializedObject.DeserializeProtobuf<T>();
                                        entitySync.ptrContent = null;

                                        //We want to leave timestamp from client if server time less then clients, for new items only
                                        if (syncTimestamp <= row.SyncTimestamp)
                                            syncTimestamp = row.SyncTimestamp + 1;

                                        ((ISyncEntity)newEntity).SyncTimestamp = syncTimestamp;
                                        ((ISyncEntity)newEntity).Id = row.InternalId; //default ID


                                        if (rowExistingEntity.Exists)
                                        {
                                            //ID interfer with existing ID, client must solve it                                            

                                            newSyncOper = new SyncOperation()
                                            {
                                                ExternalId = syncTimestamp, //It will have another ExternalId more than 0, it is suggested time stamp
                                                InternalId = row.InternalId,                                                
                                                Operation = SyncOperation.eOperation.EXCHANGE,
                                                SyncTimestamp = syncTimestamp,
                                                Type = typeof(T).FullName,
                                                // SerializedObject = row.SerializedObject
                                                SerializedObject = tran.SelectDataBlockWithFixedAddress<byte[]>(entitySync.entityTable, rowExistingEntity.Value)
                                            };

                                            returnBackOperations.Add(syncTimestamp, newSyncOper);
                                        }
                                        else
                                        {
                                            //Saving renewed entity                                                                                                 
                                            // if (entitySync.OnInsertEntity(row.InternalId, newEntity, default(T), syncTimestamp))
                                            if (entitySync.OnInsertEntity(newEntity, default(T)))
                                            {
                                                //entitySync.refToValueDataBlockWithFixedAddress must be filled after calling OnInsertEntity

                                                tran.Insert<byte[], byte[]>(entitySync.entityTable, 200.ToIndex(((ISyncEntity)newEntity).Id), entitySync.ptrContent);

                                                tran.Insert<byte[], byte[]>(entitySync.entityTable, 201.ToIndex(((ISyncEntity)newEntity).SyncTimestamp, ((ISyncEntity)newEntity).Id), null);
                                            }

                                            //tran.Insert<byte[], byte[]>(entitySync.entityTable, new byte[] { 201 }.ConcatMany(
                                            //    syncTimestamp.To_8_bytes_array_BigEndian(),
                                            //    row.InternalId.To_8_bytes_array_BigEndian()
                                            //    ), null); 

                                            //entitySync.refToValueDataBlockWithFixedAddress);

                                            if (!entityMustBeReturnedBackToClientAfterCreation) //In case if newly created on the client-side entity must not be returned back
                                                nonReturningBackEntites.Add(row.InternalId);
                                        }
                                    }
                                    break;
                                case SyncOperation.eOperation.REMOVE:

                                    //------------DOING NOTHING, WE DONT DELETE ENTITIES
                                    break;
                            }//eo switch row operation
                        }

                    }//eo foreach rows for sync

                    //Collecting limit quantity of entities which must be delivered to client from server

                    newServerSync = syncTimestamp;

                    if (syncDirection != eSynchroDirectionType.FromClient)
                    {                      

                        foreach (var row in
                            tran.SelectForwardFromTo<byte[], byte[]>(entitySync.entityTable,
                            new byte[] { 201 }.ConcatMany((LastServerSyncTimeStamp + 1).To_8_bytes_array_BigEndian(), long.MinValue.To_8_bytes_array_BigEndian()),
                            true, //but LastServerSyncTimeStamp+1 is used
                            new byte[] { 201 }.ConcatMany(long.MaxValue.To_8_bytes_array_BigEndian(), long.MaxValue.To_8_bytes_array_BigEndian()),
                            true
                            ))                        
                        {
                            if (srvSyncList.Count >= limitationOfEntitesPerRound)
                            {
                                repeatSynchro = true;
                                break;
                            }

                            newServerSync = row.Key.Substring(1, 8).To_Int64_BigEndian();

                            if (
                                nonReturningBackEntites.Contains(row.Key.Substring(9, 8).To_Int64_BigEndian())
                                //||
                                //returnBackOperations.ContainsKey(row.Key.Substring(9, 8).To_Int64_BigEndian()) //??? it's not possible here
                                ||
                                srvSyncList.ContainsKey(row.Key.Substring(9, 8).To_Int64_BigEndian())
                                )
                                continue;   //Entities not for synchronization

                            srvSyncList[row.Key.Substring(9, 8).To_Int64_BigEndian()] = row.Key.Substring(1, 8).To_Int64_BigEndian();

                        }


                        foreach (var ent in srvSyncList.OrderBy(r => r.Key))
                        {
                            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!   MUST BE CHANGED, we dont need row entity, 
                            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!   cause we already in 201 have reference to entity, 
                            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!   so srvSyncList must be Tuple containig SyncTimeStamp and refToDataBlockWithFixedAddres

                            var rowEntity = tran.Select<byte[], byte[]>(entitySync.entityTable, new byte[] { 200 }.Concat(ent.Key.To_8_bytes_array_BigEndian()));
                            //if (entitySync.entityTable == "2t35")
                            //{
                            //    Console.WriteLine("task sync return: {0}", new DateTime(ent.Value).ToString("dd.MM.yyyy HH:mm:ss"));
                            //}

                            newSyncOper = new SyncOperation()
                            {
                                //ExternalId = 0,
                                InternalId = ent.Key,
                                //SerializedObject = null, //??? do we need that - NO (We need a check if serialized object != null)
                                Operation = SyncOperation.eOperation.REMOVE,
                                SyncTimestamp = ent.Value,
                                Type = typeof(T).FullName
                            };

                            if (rowEntity.Exists)
                            {
                                //_M_WDA._Log.LogInformation("GM_PersonalDevice.IDoThings", "SynchronizeEntityWithUID", "InternalId:" + newSyncOper.InternalId + "; STS:" + newSyncOper.SyncTimestamp, "");

                                newSyncOper.Operation = SyncOperation.eOperation.INSERT;
                                //newSyncOper.SerializedObject = rowEntity.Value.SerializeProtobuf();
                                newSyncOper.SerializedObject = rowEntity.GetDataBlockWithFixedAddress<T>().SerializeProtobuf();
                            }

                            returnBackOperations.Add(ent.Key, newSyncOper);
                        }
                    }

                    entitySync.BeforeCommit();
                    tran.Commit();
                }//eo using

                entitySync.AfterCommit();

                Dictionary<string, byte[]> toClient = new Dictionary<string, byte[]>();

              
                toClient.Add("NewServerSyncTimeStamp", newServerSync.To_8_bytes_array_BigEndian());
                toClient.Add("RepeatSynchro", (repeatSynchro) ? new byte[] { 1 } : new byte[] { 0 });
                toClient.Add("SyncLst", returnBackOperations.Values.ToList().SerializeProtobuf());

                httpCapsule.Body = toClient.SerializeProtobuf();
            }
            catch (Exception ex)
            {
                httpCapsule.Type = "{'Result':'NOT OK';}";                
                Logger.LogException("EntitySyncing.Engine", "SyncEntityV1", ex, "");
            }


            return httpCapsule;
        }
    }
}
