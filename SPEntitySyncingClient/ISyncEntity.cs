using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    public interface ISyncEntity
    {
        long SyncTimestamp { get; set; }
        /// <summary>
        /// Must be always ID of the local storage (for Mobile - LocalID, for Server - ServerID)
        /// </summary>
        long Id { get; set; }

        bool Deleted { get; set; }
    }
}
