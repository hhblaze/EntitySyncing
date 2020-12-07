using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    public partial class SyncOperation
    {
        public enum eOperation
        {
            INSERT,
            REMOVE,
            EXCHANGE
        }

        public eOperation GetOperation()
        {
            return (eOperation)Operation;
        }

        public static int SetOperation(eOperation operation)
        {
            return (int)operation;
        }

        public SyncOperation()
        {
            //Operation = eOperation.INSERT;
            Operation = 1;
            SyncTimestamp = 0;
            SerializedObject = null;
            InternalId = 0;
            ExternalId = 0;
        }


        /// <summary>
        /// TimeStamp when Entity operation has occured; Synchronization ID (for all entities inside of company)
        /// </summary>

        public long SyncTimestamp { get; set; }

        /// <summary>
        /// 
        /// </summary>

        public byte[] SerializedObject { get; set; }

        /// <summary>
        /// 
        /// </summary>

        public int Operation { get; set; }

        /// <summary>
        /// Entity type
        /// </summary>

        public string Type { get; set; }

        /// <summary>
        /// Roles as UID (for disconnected unique IDs)
        /// Internal ID of the entity in disconnected client (External ID is unified entity ID on the server).
        /// 0 if not set
        /// </summary>

        public long InternalId { get; set; }

        /// <summary>
        /// Server side ID
        /// </summary>

        public long ExternalId { get; set; }

    }
}
