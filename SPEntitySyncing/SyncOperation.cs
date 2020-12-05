using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncing
{
    [ProtoBuf.ProtoContract]
    public class SyncOperation
    {
        public enum eOperation
        {
            INSERT,
            REMOVE
        }

        public SyncOperation()
        {
            Operation = eOperation.INSERT;
            SyncTimestamp = 0;
            SerializedObject = null;
            InternalId = 0;
            ExternalId = 0;
        }


        /// <summary>
        /// TimeStamp when Entity operation has occured; Synchronization ID (for all entities inside of company)
        /// </summary>
        [ProtoBuf.ProtoMember(1, IsRequired = true)]
        public long SyncTimestamp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoBuf.ProtoMember(2, IsRequired = true)]
        public byte[] SerializedObject { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoBuf.ProtoMember(3, IsRequired = true)]
        public eOperation Operation { get; set; }

        /// <summary>
        /// Entity type
        /// </summary>
        [ProtoBuf.ProtoMember(4, IsRequired = true)]
        public string Type { get; set; }

        /// <summary>
        /// Roles as UID (for disconnected unique IDs)
        /// Internal ID of the entity in disconnected client (External ID is unified entity ID on the server).
        /// 0 if not set
        /// </summary>
        [ProtoBuf.ProtoMember(5, IsRequired = true)]
        public long InternalId { get; set; }

        /// <summary>
        /// Server side ID
        /// </summary>
        [ProtoBuf.ProtoMember(6, IsRequired = true)]
        public long ExternalId { get; set; }

    }
}
