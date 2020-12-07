using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncing
{
   
    public partial class ExchangeData
    {

        public ExchangeData()
        {
           
        }
        
        public long LastServerSyncTimeStamp { get; set; }
        public List<SyncOperation> SyncOperations { get; set; } = new List<SyncOperation>();
        public bool RepeatSynchro { get; set; }
        public long NewServerSyncTimeStamp { get; set; }
    }
}
