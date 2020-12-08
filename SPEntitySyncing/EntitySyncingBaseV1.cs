using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncing
{    
    public abstract class EntitySyncingBaseV1<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="oldEntity"></param>        
        /// <returns>TRUE - indicates that incoming entity must be applied. FALSE means that this entity is not so strong by political reasons as server-side one and must be changed with server-side.</returns>
        public virtual bool OnInsertEntity(T entity, T oldEntity)
        {
            return true;
        }

        /// <summary>
        /// Will be set automatically and will be available in entity handler
        /// </summary>
        public DBreeze.Transactions.Transaction tran = null;

        public string entityTable = "";
        /// <summary>
        /// In case if value of the entity differs from the table where synchronizer holds data
        /// </summary>
        public string entityContentTable = "";
        /// <summary>
        /// Is set by synchronizer, in case if entity exists, then must be set from onInsertEntityFunction after repeat insert, refToValueDataBlockWithFixedAddress
        /// </summary>
        public byte[] ptrContent = null;
        //protected GccObjects.Net.UserManagement.GccUserRemoteView user = null;
        public object userToken = null;

        /// <summary>
        /// Chooses between entityContentTable and entityTable
        /// </summary>
        public string GetEntityContentTable
        {
            get { return String.IsNullOrEmpty(entityContentTable) ? entityTable : entityContentTable; }
        }

        public virtual void Init()
        {
           
        }


        /// <summary>
        /// Right before tran.Commit()
        /// </summary>
        public virtual void BeforeCommit()
        {
        }

        /// <summary>
        /// When transaction is closed
        /// </summary>
        public virtual void AfterCommit()
        {

        }
    }
}
