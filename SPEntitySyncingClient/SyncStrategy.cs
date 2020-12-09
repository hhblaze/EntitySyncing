using System;
using System.Collections.Generic;
using System.Text;



namespace EntitySyncingClient
{
    public abstract class SyncStrategy<T>
    {
        protected long _newLocalSyncTimeStamp;

        

        public SyncStrategy()
        {

        }


        public abstract List<SyncOperation> GetSyncOperations(DBreeze.Transactions.Transaction tran, out bool repeatSync);

        public abstract long GetLastServerSyncTimeStamp(DBreeze.Transactions.Transaction tran);


        public abstract bool UpdateLocalDatabase(ExchangeData exData);

    }
}
