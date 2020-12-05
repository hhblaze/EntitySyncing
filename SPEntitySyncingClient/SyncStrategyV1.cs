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
        private const int LocalSyncTS = 205;
        private const int ServerSyncTS = 206;

        public Engine Engine { get; set; }
        

        private EntitySyncingBaseV1 _entitySync;

        public SyncStrategyV1(string entityTable, EntitySyncingBaseV1 entitySync) : base(entityTable)
        {            
            _entitySync = entitySync;
            this._urlSync = entitySync.UrlSync;
        }

        public override long GetLastServerSyncTimeStamp(DBreeze.Transactions.Transaction tran)
        {
            return tran.Select<byte[], long>(_entityTable, new byte[] { ServerSyncTS }).Value;      // returns 0 if row does not exist
        }

        public override List<SyncOperation> GetSyncOperations(DBreeze.Transactions.Transaction tran, out bool repeatSync)
        {
            var syncList = new List<SyncOperation>();
            _newLocalSyncTimeStamp = DateTime.UtcNow.Ticks; //This value will be applied if there is nothing to synchronize
            repeatSync = false;

            var changedEntities = new Dictionary<long, long>();    //Key is entityId, Value is Synctimestamp
            var entityType = typeof(T).FullName;

            var lastLocalSyncTimeStamp = tran.Select<byte[], long>(_entityTable, new byte[] { LocalSyncTS }).Value;

            foreach (var row in
                tran.SelectForwardFromTo<byte[], byte[]>(_entityTable,
                    new byte[] { SyncLog }.ConcatMany((lastLocalSyncTimeStamp + 1).To_8_bytes_array_BigEndian(), long.MinValue.To_8_bytes_array_BigEndian()),
                    true, //true, but LastLocalSyncTimeStamp+1 is used
                    new byte[] { SyncLog }.ConcatMany(long.MaxValue.To_8_bytes_array_BigEndian(), long.MaxValue.To_8_bytes_array_BigEndian()),
                    true))
            {
                if (changedEntities.Count > LimitationOfEntitesPerRound)
                {
                    repeatSync = true;
                    break;
                }

                //We will leave only last update of the particular entity
                changedEntities[row.Key.Substring(9, 8).To_Int64_BigEndian()] = row.Key.Substring(1, 8).To_Int64_BigEndian();
                _newLocalSyncTimeStamp = row.Key.Substring(1, 8).To_Int64_BigEndian();
            }

            foreach (var ent in changedEntities.OrderBy(r => r.Key))
            {
                var rowEntity = tran.Select<byte[], byte[]>(_entityTable, new byte[] { Entity }.Concat(ent.Key.To_8_bytes_array_BigEndian()));
                var syncOperation = new SyncOperation()
                {
                    InternalId = ent.Key,
                    Operation = rowEntity.Exists ? SyncOperation.eOperation.INSERT : SyncOperation.eOperation.REMOVE,
                    Type = entityType,
                    SyncTimestamp = ent.Value
                };
                if (rowEntity.Exists)
                {
                    syncOperation.SerializedObject = rowEntity.GetDataBlockWithFixedAddress<T>().SerializeProtobuf();
                }
                syncList.Add(syncOperation);
            }
            return syncList;
        }

        public override void UpdateLocalDatabase(List<SyncOperation> syncList, long newServerSyncTimeStamp)
        {
            using (var tran = Engine.DBEngine.GetTransaction())
            {
                //Synchronization of all necessary tables must be in entitySync.Init 
                _entitySync.Init(tran, _entityTable);
                tran.ValuesLazyLoadingIsOn = false;

                //tran.Insert(_entityTable, new byte[] { LocalSyncTS }, _newLocalSyncTimeStamp);
                tran.Insert(_entityTable, new byte[] { LocalSyncTS }, _newLocalSyncTimeStamp  > newServerSyncTimeStamp ? _newLocalSyncTimeStamp : newServerSyncTimeStamp);
                tran.Insert(_entityTable, new byte[] { ServerSyncTS }, newServerSyncTimeStamp);
                T entity;
                T localEntity;

                int processedBeforeRaise = 0;

                foreach (var opr in syncList)
                {
                    switch (opr.Operation)
                    {
                        case SyncOperation.eOperation.INSERT:
                            var rowLocalEntity = tran.Select<byte[], byte[]>(_entityTable, new byte[] { Entity }.Concat(opr.InternalId.To_8_bytes_array_BigEndian()));
                            if (rowLocalEntity.Exists)
                            {
                                //Possible update                             
                                _entitySync.refToValueDataBlockWithFixedAddress = rowLocalEntity.Value;
                                localEntity = rowLocalEntity.GetDataBlockWithFixedAddress<T>();
                                entity = opr.SerializedObject.DeserializeProtobuf<T>();

                                if (((ISyncEntity)localEntity).SyncTimestamp < opr.SyncTimestamp)
                                {
                                    //Local version is weaker then server version                       
                                    _entitySync.OnInsertEntity(opr.InternalId, entity, localEntity, opr.SerializedObject);
                                }
                                else
                                {
                                    //  Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                                }
                            }
                            else
                            {
                                //Inserting new entity from server                 
                                _entitySync.refToValueDataBlockWithFixedAddress = null;
                                entity = opr.SerializedObject.DeserializeProtobuf<T>();
                                _entitySync.OnInsertEntity(opr.InternalId, entity, null, opr.SerializedObject);
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
        }



        public async Task<ESyncResult> SyncEntityWithUID()
        {
            try
            {
                var lastServerSyncTimeStamp = 0L;
                List<SyncOperation> syncList;
                bool repeatSynchro;

                using (var tran = Engine.DBEngine.GetTransaction())
                {
                    syncList = this.GetSyncOperations(tran, out repeatSynchro);
                    lastServerSyncTimeStamp = this.GetLastServerSyncTimeStamp(tran);
                }

                Dictionary<string, byte[]> toServer = new Dictionary<string, byte[]>();
                toServer.Add("LastServerSyncTimeStamp", lastServerSyncTimeStamp.To_8_bytes_array_BigEndian());
                toServer.Add("SyncLst", syncList.SerializeProtobuf());
                //syncStrategy.UrlSync
                //Sending Entities to server
                //var httpCapsule = await _engine._serverSender("/modules.http.GM_PersonalDevice/IDT_Actions",
                var httpCapsule = await Engine._serverSender(_urlSync,
                 "{'Action':'SYNCHRONIZE_ENTITIES';'EntityType':'" + typeof(T).FullName + "'}", toServer.SerializeProtobuf());

                if (httpCapsule == null)  //Synchro with error
                    return ESyncResult.ERROR;

                Dictionary<string, string> res = httpCapsule.Type.DeserializeJsonSimple();

                if (res["Result"] == "OK")
                {

                    //Unpacking all 
                    Dictionary<string, byte[]> fromServer = httpCapsule.Body.DeserializeProtobuf<Dictionary<string, byte[]>>();

                    if (!repeatSynchro && fromServer["RepeatSynchro"][0] == 1)
                        repeatSynchro = true;

                    var syncListFromServer = fromServer["SyncLst"].DeserializeProtobuf<List<SyncOperation>>();
                    var newServerSyncTimeStamp = fromServer["NewServerSyncTimeStamp"].To_Int64_BigEndian();

                    if(Engine.Verbose)
                        Console.WriteLine($"SyncEntityWithUID<{ typeof(T).Name }> ::: server returned {syncListFromServer.Count} items.");

                    this.UpdateLocalDatabase(syncListFromServer, newServerSyncTimeStamp);

                }
                else if (res["Result"] == "AUTH FAILED")
                {
                    Engine._resetWebSession?.Invoke();
                    //WebService.Instance.ResetWebSession();
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
                Logger.LogException("EntitySyncingClient.SyncStrategyV1", "SyncEntityWithUID", ex, $"type: {typeof(T).Name}");

                //Console.WriteLine($"SyncEntityWithUID<{ typeof(T).Name }> ::: {ex.ToString()}");
                return ESyncResult.ERROR;
            }

            if (Engine.Verbose)
                Console.WriteLine($"SyncEntityWithUID<{ typeof(T).Name }> ::: finished");

            return ESyncResult.OK;
        }


    }
}
