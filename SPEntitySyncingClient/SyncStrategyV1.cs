using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBreeze.Utils;

namespace EntitySyncingClient
{
    public class SyncStrategyV1<T> : SyncStrategy<T>
    {
        private const int Entity = 200;
        private const int SyncLog = 201;
        private const int LocalSyncTS = 202; //202 1
        private const int ServerSyncTS = 202; //202 2

        public Engine SyncEngine { get; set; }        

        private EntitySyncingBaseV1<T> _entitySync;




        /// <summary>
        /// Fills up index 200. Creation of transaction, synchronization of the table and transaction commit is outside of this function.
        /// Entity desired ID and SyncTimestamp must be specified
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="table">Where must be stored index 200</param>
        /// <param name="entity">entity.Id and entity.SyncTimestamp must be filled up</param>
        /// <param name="ptrEntityContent">pointer to the entity content (16 bytes) gathered with DBreeze InsertDataBlockWithFixedAddress</param>
        /// <param name="oldEntity">old instance of the entity from DB !!!MUST!!! be supplied when update or null when new entity</param>
        public static void InsertIndex4Sync(DBreeze.Transactions.Transaction tran, string table, T entity, byte[] ptrEntityContent, T oldEntity)
        {
            ISyncEntity ent = ((ISyncEntity)entity);

            if (oldEntity == null)
                tran.Insert<byte[], byte[]>(table, 200.ToIndex(ent.Id), ptrEntityContent);

            //Adding to value one byte indicating that this is a new entity
            tran.Insert<byte[], byte[]>(table, 201.ToIndex(ent.SyncTimestamp, ent.Id), (oldEntity == null) ? new byte[] { 1 } : null);
        }

        public SyncStrategyV1(EntitySyncingBaseV1<T> entitySync)
        {            
            _entitySync = entitySync;
        }

        public override long GetLastServerSyncTimeStamp(DBreeze.Transactions.Transaction tran)
        {
            return tran.Select<byte[], long>(_entitySync.entityTable, new byte[] { ServerSyncTS, 2 }).Value;      // returns 0 if row does not exist
        }

        public override List<SyncOperation> GetSyncOperations(DBreeze.Transactions.Transaction tran, out bool repeatSync)
        {
            var syncList = new List<SyncOperation>();
            _newLocalSyncTimeStamp = DateTime.UtcNow.Ticks; //This value will be applied if there is nothing to synchronize
            repeatSync = false;

            var changedEntities = new Dictionary<long, Tuple<long,bool>>();    //Key is entityId, Value is Synctimestamp
            var entityType = typeof(T).FullName;

            var lastLocalSyncTimeStamp = tran.Select<byte[], long>(_entitySync.entityTable, new byte[] { LocalSyncTS, 1 }).Value;

            foreach (var row in
                tran.SelectForwardFromTo<byte[], byte[]>(_entitySync.entityTable,
                    new byte[] { SyncLog }.ConcatMany((lastLocalSyncTimeStamp + 1).To_8_bytes_array_BigEndian(), long.MinValue.To_8_bytes_array_BigEndian()),
                    true, //true, but LastLocalSyncTimeStamp+1 is used
                    new byte[] { SyncLog }.ConcatMany(long.MaxValue.To_8_bytes_array_BigEndian(), long.MaxValue.To_8_bytes_array_BigEndian()),
                    true))
            {
                if (changedEntities.Count > _entitySync.LimitationOfEntitesPerRound)
                {
                    repeatSync = true;
                    break;
                }

                //We will leave only last update of the particular entity
                if (!changedEntities.TryGetValue(row.Key.Substring(9, 8).To_Int64_BigEndian(), out var tpl1))
                {
                    changedEntities[row.Key.Substring(9, 8).To_Int64_BigEndian()] = new Tuple<long, bool>(row.Key.Substring(1, 8).To_Int64_BigEndian(),
                        (row.Value?.Length >= 1 && row.Value[0] == 1) ? true : false) //indicating that new entity was inserted
                        ;
                }
                else
                {
                    changedEntities[row.Key.Substring(9, 8).To_Int64_BigEndian()] = new Tuple<long, bool>(row.Key.Substring(1, 8).To_Int64_BigEndian(),
                        (tpl1.Item2 || (row.Value?.Length >= 1 && row.Value[0] == 1)) ? true : false) //indicating that new entity was inserted (from any of inserts that must be done for the id)
                        ;
                }

                _newLocalSyncTimeStamp = row.Key.Substring(1, 8).To_Int64_BigEndian();
            }

            foreach (var ent in changedEntities.OrderBy(r => r.Key))
            {
                var rowEntity = tran.Select<byte[], byte[]>(_entitySync.entityTable, new byte[] { Entity }.Concat(ent.Key.To_8_bytes_array_BigEndian()));

                var syncOperation = new SyncOperation()
                {
                    ExternalId = ent.Value.Item2 ? 0 : ent.Key, //if entity new ExternalId will be 0 otherwise will equal to InternalId and higher than 0
                    InternalId = ent.Key,
                    Operation = rowEntity.Exists ? SyncOperation.eOperation.INSERT : SyncOperation.eOperation.REMOVE,
                    Type = entityType,
                    SyncTimestamp = ent.Value.Item1
                };
                if (rowEntity.Exists)
                {                    
                    syncOperation.SerializedObject = rowEntity.GetDataBlockWithFixedAddress<byte[]>();
                }
                syncList.Add(syncOperation);
            }
            return syncList;
        }

        public override bool UpdateLocalDatabase(ExchangeData exData) //(List<SyncOperation> syncList, long newServerSyncTimeStamp)
        {
            
            var now = DateTime.UtcNow.Ticks;
            bool reRunSync = false;

            using (var tran = SyncEngine.DBEngine.GetTransaction())
            {
                //Synchronization of all necessary tables must be in entitySync.Init 
                _entitySync.tran = tran;
                _entitySync.Init();
                tran.ValuesLazyLoadingIsOn = false;
                                
                tran.Insert(_entitySync.entityTable, new byte[] { LocalSyncTS, 1 }, _newLocalSyncTimeStamp  > exData.NewServerSyncTimeStamp ? _newLocalSyncTimeStamp : exData.NewServerSyncTimeStamp);
                tran.Insert(_entitySync.entityTable, new byte[] { ServerSyncTS, 2 }, exData.NewServerSyncTimeStamp);
                T entity;
                T localEntity;

                int processedBeforeRaise = 0;

                //Taking care changed IDs by server
                foreach (var opr in exData.SyncOperations.Where(r => r.Operation == SyncOperation.eOperation.EXCHANGE))
                {
                    if (opr.ExternalId > 0)
                    {
                        //opr.ExternalID will help to determine new ID
                        var rowLocalEntity = tran.Select<byte[], byte[]>(_entitySync.entityTable, new byte[] { Entity }.Concat(opr.InternalId.To_8_bytes_array_BigEndian()));                        
                        if (rowLocalEntity.Exists)
                        {
                            var oldEntity = rowLocalEntity.GetDataBlockWithFixedAddress<T>();

                            _entitySync.ptrContent = null;

                            //New GeneratedID must be stored for the new sync
                            ((ISyncEntity)oldEntity).Id = opr.ExternalId; //Theoretically on this place can be called a user-function to get another ID type
                            ((ISyncEntity)oldEntity).SyncTimestamp = ++now;  //must be returned back, overriding SyncTimeStamp                                     

                            _entitySync.OnInsertEntity(oldEntity, default(T), SyncEngine.Serialize(oldEntity), opr.InternalId);

                            InsertIndex4Sync(tran, _entitySync.entityTable, oldEntity, _entitySync.ptrContent, default(T));


                            //Setting value from the server for the existing ID (real entity that must belong to that id)
                            _entitySync.ptrContent = rowLocalEntity.Value;
                            
                            _entitySync.OnInsertEntity((T)SyncEngine.Deserialize(opr.SerializedObject, typeof(T)), default(T),
                                opr.SerializedObject, 0);

                            reRunSync = true;
                        }
                    }
                 
                }

                //standard entites
                foreach (var opr in exData.SyncOperations.Where(r=>r.Operation != SyncOperation.eOperation.EXCHANGE))
                {
                    switch (opr.Operation)
                    {
                        case SyncOperation.eOperation.INSERT:
                            var rowLocalEntity = tran.Select<byte[], byte[]>(_entitySync.entityTable, new byte[] { Entity }.Concat(opr.InternalId.To_8_bytes_array_BigEndian()));
                            if (rowLocalEntity.Exists)
                            {
                                //Possible update                             
                                _entitySync.ptrContent = rowLocalEntity.Value;
                                localEntity = rowLocalEntity.GetDataBlockWithFixedAddress<T>();
                                
                                entity = (T)SyncEngine.Deserialize(opr.SerializedObject, typeof(T));

                                if (((ISyncEntity)localEntity).SyncTimestamp < opr.SyncTimestamp)
                                {
                                    //Local version is weaker then server version                       
                                    _entitySync.OnInsertEntity(entity, localEntity, opr.SerializedObject, 0);

                                    InsertIndex4Sync(tran, _entitySync.entityTable, entity, _entitySync.ptrContent, localEntity);
                                }
                                else
                                {
                                    ////------------Nothing
                                }
                            }
                            else
                            {
                                //Inserting new entity from server                 
                                _entitySync.ptrContent = null;
                               // entity = opr.SerializedObject.DeserializeProtobuf<T>();
                                entity = (T)SyncEngine.Deserialize(opr.SerializedObject, typeof(T));
                                
                                _entitySync.OnInsertEntity(entity, default(T), opr.SerializedObject, 0);
                                InsertIndex4Sync(tran, _entitySync.entityTable, entity, _entitySync.ptrContent, default(T));

                            }
                            break;
                        case SyncOperation.eOperation.REMOVE:

                            //------------DOING NOTHING, WE DONT DELETE ENTITIES

                            break;                      
                    }

                    //Computing processed elements and raises event once per SyncEntitiesMgr.RaiseSyncProcessEach
                    System.Threading.Interlocked.Increment(ref Engine.SyncOperationsCount);
                    processedBeforeRaise++;
                    if (processedBeforeRaise >= Engine.RaiseSyncProcessEach)
                    {
                        processedBeforeRaise = 0;
                        Engine.OnSyncProcess();
                    }
                }

                _entitySync.BeforeCommit();

                tran.Commit();
            }
            _entitySync.OnEntitySyncIsFinished();

            return reRunSync;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<ESyncResult> SyncEntity()
        {
            try
            {              
                bool repeatSynchro;

                var toServer = new ExchangeData();

                using (var tran = SyncEngine.DBEngine.GetTransaction())
                {
                    toServer.SyncOperations = this.GetSyncOperations(tran, out repeatSynchro);
                    toServer.LastServerSyncTimeStamp = this.GetLastServerSyncTimeStamp(tran);
                }



                //Sending Entities to server
                //var httpCapsule = await _engine._serverSender("/modules.http.GM_PersonalDevice/IDT_Actions",

                var caps = new HttpCapsule
                {
                    Action = "SYNC",
                    EntityType = typeof(T).FullName,                    
                    Body = toServer.BiserEncoder().Encode()
                };

                var httpCapsuleBt = await SyncEngine._serverSender(_entitySync.urlSync, caps.BiserEncoder().Encode());                

                if (httpCapsuleBt == null)  //Synchro with error
                    return ESyncResult.ERROR;

                var httpCapsule = HttpCapsule.BiserDecode(httpCapsuleBt);

                //if (httpCapsule == null)  //Synchro with error
                //    return ESyncResult.ERROR;

                //Dictionary<string, string> res = httpCapsule.Type.DeserializeJsonSimple();

                if (httpCapsule.IsOk)
                {

                    var exData = ExchangeData.BiserDecode(httpCapsule.Body);

                    if (!repeatSynchro && exData.RepeatSynchro)
                        repeatSynchro = true;

                    if(SyncEngine.Verbose)
                        Console.WriteLine($"SyncEntity<{ typeof(T).Name }> ::: server returned {exData.SyncOperations?.Count} items.");

                    if(this.UpdateLocalDatabase(exData))
                    {
                        return await SyncEntity(); //this is a rare exeuting place, only in case if clientSideEntityID equals to existing serverSideEntityID, and even in this case it should be executed only once
                    }

                }
                else if (!httpCapsule.IsOk && httpCapsule.Action == "AUTH FAILED")
                {
                    if (SyncEngine._resetWebSession != null)
                    {
                        try
                        {
                            SyncEngine._resetWebSession?.Invoke();
                        }
                        catch
                        {}                        
                    }
                    
                    return ESyncResult.AUTH_FAIL;
                }

                //Repeat call of the procedure
                if (repeatSynchro)
                {
                    return ESyncResult.REPEAT;
                }

            }
            catch (Exception ex)
            {
                Logger.LogException("EntitySyncingClient.SyncStrategyV1", "SyncEntity", ex, $"type: {typeof(T).Name}");

                return ESyncResult.ERROR;
            }

            if (SyncEngine.Verbose)
                Console.WriteLine($"SyncEntity<{ typeof(T).Name }> ::: finished");

            return ESyncResult.OK;
        }



        


    }//eo class
}
