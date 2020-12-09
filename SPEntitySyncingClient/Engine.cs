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

        public Func<object, byte[]> Serialize = null;
        public Func<byte[], Type, object> Deserialize = null;

        /// <summary>
        /// Url / payload
        /// </summary>
        internal Func<string, byte[], Task<byte[]>> _serverSender = null;
        internal Action _resetWebSession = null;
        internal Action _syncIsFinishing = null;

        
        Dictionary<string, IEntityFold> lstToSync = new Dictionary<string, IEntityFold>();



        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbEngine"></param>
        /// <param name="serverSender"></param>
        /// <param name="resetWebSession"></param>
        /// <param name="syncIsFinishing"></param>
        /// <param name="logger">can be null</param>
        /// <param name="byteArraySerializer">can be null then DBreeze embedded serializer will be used</param>
        /// <param name="byteArrayDeSerializer">can be null then DBreeze embedded deserializer will be used</param>
        public Engine(DBreeze.DBreezeEngine dbEngine, Func<string, byte[], Task<byte[]>> serverSender, Action resetWebSession, Action syncIsFinishing, ILogger logger,
            Func<object, byte[]> byteArraySerializer = null, Func<byte[], Type, object> byteArrayDeSerializer = null)
        {
            if (logger == null)
                Logger.log = new LoggerWrapper();
            else
                Logger.log = logger;

            if (dbEngine == null)
            {
                Logger.LogException("EntitySyncingClient.Engine", "Init", new Exception("DBreezeEngine is not specified"), "");
                return;
            }


            if ((byteArraySerializer != null && byteArrayDeSerializer == null) || (byteArraySerializer != null && byteArrayDeSerializer == null))
                throw new Exception("EntitySyncing.Engine.Init: please supply both ByteArrayDeSerializator and ByteArraySerializator");

            if (byteArraySerializer == null && byteArrayDeSerializer == null)
            {
                //Trying to use DBreeze serializers
                if (DBreeze.Utils.CustomSerializator.ByteArrayDeSerializator == null || DBreeze.Utils.CustomSerializator.ByteArraySerializator == null)
                {
                    throw new Exception("EntitySyncing.Engine.Init: please supply both ByteArrayDeSerializator and ByteArraySerializator or embed serializers to DBreeze");
                }

                this.Serialize = DBreeze.Utils.CustomSerializator.ByteArraySerializator;
                this.Deserialize = DBreeze.Utils.CustomSerializator.ByteArrayDeSerializator;
            }
            else
            {
                this.Serialize = byteArraySerializer;
                this.Deserialize = byteArrayDeSerializer;
            }


            DBEngine = dbEngine;

            _serverSender = serverSender;
            _resetWebSession = resetWebSession;
            _syncIsFinishing = syncIsFinishing;
        }


        internal static void OnSyncProcess()
        {
            if (SyncProcess != null)
            {
                try
                {
                    SyncProcess?.Invoke(System.Threading.Interlocked.Read(ref SyncOperationsCount));
                }
                catch
                {}
                
            }
        }

        interface IEntityFold
        {
            Task<ESyncResult> Sync();
        }

        class EntityFold<T>:IEntityFold
        {
            public Type type = null;

            public EntitySyncingBaseV1<T> entity = null;

            public MethodInfo SyncEntity = null;

            public object Instance = null;

           // public string Table;

            public Task<ESyncResult> Sync()
            {
                return (Task<ESyncResult>)SyncEntity.Invoke(Instance, null);
            }
        }

        /// <summary>
        /// Adds entity 4 sync of StrategyV1
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        public void AddEntity4Sync<T>(EntitySyncingBaseV1<T> entity)
        {
            if (lstToSync.ContainsKey(typeof(T).ToString()))
                return;

            entity.SyncingEngine = this;

            EntityFold<T> igo = new EntityFold<T>()
            {
                type = typeof(T),
                entity = entity,
            };

            //Here SyncStrategyV1 will be used

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

            try
            {
                SyncStarted?.Invoke();
            }
            catch { }

            try
            {                


                var lt = new List<System.Threading.Tasks.Task<ESyncResult>>();

                System.Threading.Interlocked.Exchange(ref SyncOperationsCount, 0);


                //Running syncing strategies on all supplied entites
                foreach (var e2s in lstToSync)
                {      
                    lt.Add(Sync(e2s.Value));
                }
               


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

        async System.Threading.Tasks.Task<ESyncResult> Sync(IEntityFold syncStrategy)
        {
            var mres = ESyncResult.ERROR;
            while ((mres = await syncStrategy.Sync()) == ESyncResult.REPEAT) ;
            return mres;
        }
    }
}
