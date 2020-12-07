using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    [ProtoBuf.ProtoContract]
    public partial class HttpCapsule
    {

        public HttpCapsule()
        {
            Type = "EMPTY";
            Body = null;
        }

        /// <summary>
        /// Type of the HTTP request/response . Default value is EMPTY
        /// </summary>
        [ProtoBuf.ProtoMember(1, IsRequired = true)]
        public string Type { get; set; }

        /// <summary>
        /// Serialized with protobuf some kind of an object, its type can be determined by Type
        /// </summary>
        [ProtoBuf.ProtoMember(2, IsRequired = true)]
        public byte[] Body { get; set; }
    }
}
