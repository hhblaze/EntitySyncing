using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntitySyncingClientTester
{  
    public partial class Entity_Task : EntitySyncingClient.ISyncEntity
    {
        
    }

    [ProtoBuf.ProtoContract]
    public partial class Entity_Task
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


    public partial class Entity_Task : EntitySyncingClient.ISyncEntity
    {

    }

    ///// <summary>
    ///// Must be unremarked and Entity_Task_Server must be remarked on the server-side, having in both client and server one entity
    ///// </summary>
    //public partial class Entity_Task : Entity_Task, EntitySyncing.ISyncEntity
    //{

    //}


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


