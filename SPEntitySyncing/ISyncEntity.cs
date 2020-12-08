using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncing
{
    public interface ISyncEntity
    {
        long SyncTimestamp { get; set; }
       
        long Id { get; set; }

        bool Deleted { get; set; }
    }
}
