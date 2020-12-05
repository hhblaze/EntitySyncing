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


        ///// <summary>
        ///// Check serverSender destination
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="syncStrategy"></param>
        ///// <param name="serverSender">External function that sends via HTTP </param>
        ///// <param name="resetWebSession">if supplied will be called when need to kill websession</param>
        ///// <returns></returns>
        //async Task<ESyncResult> SyncEntityWithUID<T>(SyncStrategy<T> syncStrategy)
        //{
        //    try
        //    {
        //        var lastServerSyncTimeStamp = 0L;
        //        List<SyncOperation> syncList;
        //        bool repeatSynchro;

        //        using (var tran = DBEngine.GetTransaction())
        //        {
        //            syncList = syncStrategy.GetSyncOperations(tran, out repeatSynchro);
        //            lastServerSyncTimeStamp = syncStrategy.GetLastServerSyncTimeStamp(tran);
        //        }

        //        Dictionary<string, byte[]> toServer = new Dictionary<string, byte[]>();
        //        toServer.Add("LastServerSyncTimeStamp", lastServerSyncTimeStamp.To_8_bytes_array_BigEndian());
        //        toServer.Add("SyncLst", syncList.SerializeProtobuf());
        //        //syncStrategy.UrlSync
        //        //Sending Entities to server
        //        var httpCapsule = await _serverSender("/modules.http.GM_PersonalDevice/IDT_Actions",
        //         "{'Action':'SYNCHRONIZE_ENTITIES';'EntityType':'" + typeof(T).FullName + "'}", toServer.SerializeProtobuf());

        //        if (httpCapsule == null)  //Synchro with error
        //            return ESyncResult.ERROR;

        //        Dictionary<string, string> res = httpCapsule.Type.DeserializeJsonSimple();

        //        if (res["Result"] == "OK")
        //        {

        //            //Unpacking all 
        //            Dictionary<string, byte[]> fromServer = httpCapsule.Body.DeserializeProtobuf<Dictionary<string, byte[]>>();

        //            if (!repeatSynchro && fromServer["RepeatSynchro"][0] == 1)
        //                repeatSynchro = true;

        //            var syncListFromServer = fromServer["SyncLst"].DeserializeProtobuf<List<SyncOperation>>();
        //            var newServerSyncTimeStamp = fromServer["NewServerSyncTimeStamp"].To_Int64_BigEndian();

        //            Console.WriteLine($"SyncEntityWithUID<{ typeof(T).Name }> ::: server returned {syncListFromServer.Count} items.");

        //            syncStrategy.UpdateLocalDatabase(syncListFromServer, newServerSyncTimeStamp);

        //        }
        //        else if (res["Result"] == "AUTH FAILED")
        //        {
        //            _resetWebSession?.Invoke();
        //            //WebService.Instance.ResetWebSession();
        //            return ESyncResult.AUTH_FAIL;
        //        }

        //        //Repeat call of the procedure
        //        if (repeatSynchro)
        //        {
        //            return ESyncResult.REPEAT;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"SyncEntityWithUID<{ typeof(T).Name }> ::: {ex.ToString()}");
        //        return ESyncResult.ERROR;
        //    }
        //    Console.WriteLine($"SyncEntityWithUID<{ typeof(T).Name }> ::: finished");
        //    return ESyncResult.OK;
        //}


        
        Dictionary<string, EntityFold> lstToSync = new Dictionary<string, EntityFold>();

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
        public void AddSyncEntityV1<T>(EntitySyncingBaseV1 entity)
        {
            if (lstToSync.ContainsKey(typeof(T).ToString()))
                return;

            EntityFold igo = new EntityFold()
            {
                type = typeof(T),
                entity = entity,
               // Table = table
            };

            
            Type openGenericClass = typeof(SyncStrategyV1<>);
            Type dynamicClosedGenericClass = openGenericClass.MakeGenericType(igo.type);
            igo.Instance = Activator.CreateInstance(dynamicClosedGenericClass, entity);
            //igo.SyncEntity = dynamicClosedGenericClass.GetMethod("SyncEntityWithUID");           
            igo.SyncEntity = dynamicClosedGenericClass.GetMethod("SyncEntityWithUID");

            var pi = dynamicClosedGenericClass.GetProperty("Engine");
            pi.SetValue(igo.Instance, this);

            lstToSync.Add(igo.type.ToString(), igo);
        }


        public async System.Threading.Tasks.Task SynchronizeEntities()
        {
            //Removing check from here
            //if (User.Profile == null)
            //    return;

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

                _syncIsFinishing?.Invoke(); //Can be used special hacks for all entites to clean table and so on. Sync will be finished only after calling this function; not necessary in try catch

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
