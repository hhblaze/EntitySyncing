using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    public enum ESyncResult
    {
        ERROR = 0,
        OK = 1,
        REPEAT = 2,
        AUTH_FAIL = 3
    }
}
