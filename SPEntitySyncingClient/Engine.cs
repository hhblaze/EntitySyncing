using DBreeze;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DBreeze.Utils;
using System.Linq;
using System.Reflection;

namespace EntitySyncingClient
{
    public class Engine
    {
        object lock_entitySynchro = new object();
        /// <summary>
        /// Indicates if InSync mode
        /// </summary>
        bool InEntitySynchro = false;

        /// <summary>
        /// For different technical message into LoggerClass
        /// </summary>
        public bool Verbose = false;

        long currentTryTime = DateTime.MinValue.Ticks;
        long anotherTryTime = DateTime.MinValue.Ticks;

        public Action SyncStarted;
        public Action<DateTime?> SyncStopped;
        public static Action<long> SyncProcess; //Entities count

        public DateTime LastSync = DateTime.MinValue;

        internal static long SyncOperationsCount = 0;
        /// <summary>
        /// Quantity of processed elements to raise SyncProcess  
        /// </summary>
        public static int RaiseSyncProcessEach = 50; //quantity of processed elements to raise SyncProcess  

        internal DBreezeEngine DBEngine = null;

        internal Func<string, string, object, Task<HttpCapsule>> _serverSender = null;
        internal Action _resetWebSession = null;
        internal Action _syncIsFinishing = null;

        Dictionary<string, EntityFold> lstToSync = new Dictionary<string, EntityFold>();


        public Engine(ILogger logger, DBreeze.DBreezeEngine dbEngine, Func<string, string, object, Task<HttpCapsule>> serverSender, Action resetWebSession, Action syncIsFinishing)
        {
            Logger.log = logger;

            if (dbEngine == null)
            {
                Logger.LogException("EntitySyncingClient.Engine", "Init", new Exception("DBreezeEngine is not specified"), "");
                return;
            }

            DBEngine = dbEngine;

            _serverSender = serverSender;
            _resetWebSession = resetWebSession;
            _syncIsFinishing = syncIsFinishing;
        }


        internal static void OnSyncProcess()
        {
            SyncProcess?.Invoke(System.Threading.Interlocked.Read(ref SyncOperationsCount));
        }

        /// <summary>
        /// Fills up index 200. Creation of transaction, synchronization of the table and transaction commit is outside of this function.
        /// Entity desired ID and SyncTimestamp must be specified
        /// </summary>
        /// <param name="table">Where must be stored index 200</param>
        /// <param name="entity">entity.Id and entity.SyncTimestamp must be filled up</param>
        /// <param name="ptrEntityContent">pointer to the entity content (16 bytes) gathered with DBreeze InsertDataBlockWithFixedAddress</param>
        /// <param name="oldEntity">old instance of the entity from DB</param>
        public void InsertIndex4Sync(DBreeze.Transactions.Transaction tran, string table, ISyncEntity entity, byte[] ptrEntityContent, ISyncEntity oldEntity)
        {
            if(oldEntity == null)
                tran.Insert<byte[], byte[]>(table, 200.ToIndex(entity.Id), ptrEntityContent);

            //Adding to value one byte (17) indicating that this is a new entity
            tran.Insert<byte[], byte[]>(table, 201.ToIndex(entity.SyncTimestamp, entity.Id), (oldEntity == null) ? new byte[] { 1 } : null);
        }


        class EntityFold
        {
            public Type type = null;

            public EntitySyncingBaseV1 entity = null;

            public MethodInfo SyncEntity = null;

            public object Instance = null;

           // public string Table;

            public Task<ESyncResult> Sync()
            {
                return (Task<ESyncResult>)SyncEntity.Invoke(Instance, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        public void AddEntity4Sync<T>(EntitySyncingBaseV1 entity)
        {
            if (lstToSync.ContainsKey(typeof(T).ToString()))
                return;

            entity.SyncingEngine = this;

            EntityFold igo = new EntityFold()
            {
                type = typeof(T),
                entity = entity,
            };

            
            Type openGenericClass = typeof(SyncStrategyV1<>);
            Type dynamicClosedGenericClass = openGenericClass.MakeGenericType(igo.type);
            igo.Instance = Activator.CreateInstance(dynamicClosedGenericClass, entity);                     
            igo.SyncEntity = dynamicClosedGenericClass.GetMethod("SyncEntity");

            var pi = dynamicClosedGenericClass.GetProperty("SyncEngine");
            pi.SetValue(igo.Instance, this);

            lstToSync.Add(igo.type.ToString(), igo);
        }


        public async System.Threading.Tasks.Task SynchronizeEntities()
        {        

            lock (lock_entitySynchro)
            {
                if (InEntitySynchro)
                {
                    anotherTryTime = DateTime.UtcNow.Ticks;
                    return;
                }
                InEntitySynchro = true;

                currentTryTime = DateTime.UtcNow.Ticks;
            }

            Action onSyncFailed = () =>
            {
                lock (lock_entitySynchro)
                {
                    InEntitySynchro = false;
                }
                try
                {
                    SyncStopped?.Invoke(null);
                }
                catch { }

            };

            //var userId = User.Profile.ID;
            try
            {
                SyncStarted?.Invoke();
            }
            catch { }

            try
            {                


                var lt = new List<System.Threading.Tasks.Task<ESyncResult>>();

                System.Threading.Interlocked.Exchange(ref SyncOperationsCount, 0);

                //V2 sync strategy entities


                foreach (var e2s in lstToSync)
                {      
                    lt.Add(Sync(e2s.Value));
                }
               

                //lt.Add(Sync(
                //   new SyncStrategyV1<TaskDescriptionTemplate>(DBConstants.Table_TaskDescrTemplates(userId), new SyncTaskDescriptionTemplates())
                //   ));

                //lt.Add(Sync(
                //  new SyncStrategyV1<GM_IDoThings.Article>(DBConstants.Table_TaskArticles(userId), new SyncArticles())
                //  ));



                await System.Threading.Tasks.Task.WhenAll(lt);

                try
                {
                    _syncIsFinishing?.Invoke(); //Can be used special hacks for all entites to clean table and so on. Sync will be finished only after calling this function; not necessary in try catch
                }
                catch { }
                

                System.Threading.Interlocked.Exchange(ref SyncOperationsCount, 0);

                if (lt.Where(r => r.Result != ESyncResult.OK).Any())
                {
                    onSyncFailed();
                    return;
                }


            }
            catch (Exception ex)
            {
                if(Verbose)
                    Logger.LogException("EntitySyncingClient.Engine", "SynchronizeEntities", ex, "");
                
            }

            bool anotherTry = false;
            lock (lock_entitySynchro)
            {
                InEntitySynchro = false;
                LastSync = DateTime.UtcNow;
                anotherTry = anotherTryTime > currentTryTime;
            }

            try
            {

                if (anotherTry)
                    await SynchronizeEntities();

                SyncStopped?.Invoke(LastSync);
            }
            catch { }


        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="syncStrategy"></param>
        ///// <param name="serverSender"></param>
        ///// <returns></returns>
        //async System.Threading.Tasks.Task<ESyncResult> Sync<T>(SyncStrategy<T> syncStrategy)
        //{
        //    var mres = ESyncResult.ERROR;
        //    while ((mres = await SyncEntityWithUID(syncStrategy)) == ESyncResult.REPEAT);
        //    return mres;
        //}

        async System.Threading.Tasks.Task<ESyncResult> Sync(EntityFold syncStrategy)
        {
            var mres = ESyncResult.ERROR;
            //while ((mres = await SyncEntityWithUID(syncStrategy)) == ESyncResult.REPEAT) ;
            while ((mres = await syncStrategy.Sync()) == ESyncResult.REPEAT) ;
            return mres;
        }
    }
}
