using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
   
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
       
        public string Type { get; set; }

        /// <summary>
        /// Serialized with protobuf some kind of an object, its type can be determined by Type
        /// </summary>
       
        public byte[] Body { get; set; }
    }
}
