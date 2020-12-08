using System;
using System.Collections.Generic;
using System.Text;

using DBreeze.Utils;

namespace EntitySyncingClient
{
    public abstract class EntitySyncingBaseV1<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="oldEntity"></param>
        /// <param name="nonDeserializedEntity"></param>
        /// <param name="changedID">contains old ID of the entity after server changed it</param>
        public virtual void OnInsertEntity(T entity, T oldEntity, byte[] nonDeserializedEntity, long changedID)
        {

        }

        public virtual void OnEntitySyncIsFinished()
        {

        }

        public virtual void BeforeCommit()
        {

        }

        /// <summary>
        /// Will be set automatically and will be available in entity handler
        /// </summary>
        public DBreeze.Transactions.Transaction tran = null;
        public string entityTable = "";
        /// <summary>
        /// In case if value of the entity differs from the table where synchronizer holds data (refToValueDataBlockWithFixedAddress)
        /// </summary>
        public string entityContentTable = "";
        public byte[] ptrContent = null;

        public EntitySyncingClient.Engine SyncingEngine = null;

        /// <summary>
        /// Chooses between entityContentTable and entityTable
        /// </summary>
        public string GetEntityContentTable
        {
            get { return String.IsNullOrEmpty(entityContentTable) ? entityTable : entityContentTable; }
        }

        /// <summary>
        /// Example "/modules.http.GM_PersonalDevice/IDT_Actions"
        /// </summary>
        public string urlSync = String.Empty;

        //public virtual void Init(DBreeze.Transactions.Transaction tran)
        public virtual void Init()
        {
            //this.tran = tran;
            //this.entityTable = entityTable;
        }



    }
}
