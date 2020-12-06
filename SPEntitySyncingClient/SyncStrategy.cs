using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    public abstract class SyncStrategy<T>
    {
        protected string _entityTable;
        protected long _newLocalSyncTimeStamp;
        protected string _urlSync;

        protected const int LimitationOfEntitesPerRound = 100;    // can send no more than this number of entities to server in one request

        public SyncStrategy(string entityTable)
        {
            _entityTable = entityTable;
        }

        public Type GetEntityType()
        {
            return typeof(T);
        }

        public abstract List<SyncOperation> GetSyncOperations(DBreeze.Transactions.Transaction tran, out bool repeatSync);

        public abstract long GetLastServerSyncTimeStamp(DBreeze.Transactions.Transaction tran);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="syncList"></param>
        /// <param name="newServerSyncTimeStamp"></param>
        /// <returns>indicates that there were conflict entity IDs and we must re-run synchronization with the server</returns>
        public abstract bool UpdateLocalDatabase(List<SyncOperation> syncList, long newServerSyncTimeStamp);

        /// <summary>
        /// "/modules.http.GM_PersonalDevice/IDT_Actions"
        /// </summary>
        public string UrlSync
        {
            get { return this._urlSync ?? String.Empty; }
        }
    }
}
