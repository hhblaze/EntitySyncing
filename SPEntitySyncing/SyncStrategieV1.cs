using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncing
{
    public class EntitySyncing<T>
    {
        EntitySyncingBaseV1<T> _entitySync = null;

        public EntitySyncing(EntitySyncingBaseV1<T> entitySync)
        {
            this._entitySync = entitySync;
        }
    }
}
