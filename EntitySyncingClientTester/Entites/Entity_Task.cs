using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntitySyncingClientTester
{
    [ProtoBuf.ProtoContract]
    public class Entity_Task_Client: EntitySyncingClient.ISyncEntity
    {
        
        [ProtoBuf.ProtoMember(1, IsRequired = true)]
        public long Id { get; set; }

        [ProtoBuf.ProtoMember(2, IsRequired = true)]
        public long SyncTimestamp { get; set; }

        [ProtoBuf.ProtoMember(3, IsRequired = true)]
        public bool Deleted { get; set; } = false;

        [ProtoBuf.ProtoMember(4, IsRequired = true)]
        public string Description { get; set; }
    }

    [ProtoBuf.ProtoContract]
    public class Entity_Task_Server : EntitySyncing.ISyncEntity
    {

        [ProtoBuf.ProtoMember(1, IsRequired = true)]
        public long Id { get; set; }

        [ProtoBuf.ProtoMember(2, IsRequired = true)]
        public long SyncTimestamp { get; set; }

        [ProtoBuf.ProtoMember(3, IsRequired = true)]
        public bool Deleted { get; set; } = false;

        [ProtoBuf.ProtoMember(4, IsRequired = true)]
        public string Description { get; set; }
    }

}
