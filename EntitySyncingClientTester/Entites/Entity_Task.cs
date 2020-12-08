using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntitySyncingClientTester
{  
   

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

    /// <summary>
    /// Add on the client-side together with entity itself
    /// </summary>
    public partial class Entity_Task : EntitySyncingClient.ISyncEntity
    {

    }


    ///// <summary>
    ///// !!!!!!!!!!!! Add on the client-side together with entity itself
    ///// </summary>
    //public partial class Entity_Task : Entity_Task, EntitySyncing.ISyncEntity
    //{

    //}

    /// <summary>
    /// HERER IT IS ONLY FOR EMULATING REASONS
    /// For emulating reasons inside the same project entity receives another name Entity_Task_Server. 
    /// </summary>
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


