using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    public partial class HttpCapsule
    {

        public HttpCapsule()
        {
        }

        public string Action { get; set; } = String.Empty;
        public string EntityType { get; set; } = String.Empty;
        public bool IsOk { get; set; } = true;
        public byte[] Body { get; set; }

    }
}
