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
        /// <param name="entityTable"></param>
        /// <param name="entityKey"></param>
        /// <param name="entityValue"></param>
        /// <param name="oldEntity"></param>
        /// <param name="nonDeserializedEntity">Very important save exactly this entity, because it must have more properties then not updated client</param>
        public virtual void OnInsertEntity(T entity, T oldEntity, byte[] nonDeserializedEntity)
        {

        }

        public virtual void OnEntitySyncIsFinished()
        {

        }

        public virtual void BeforeCommit()
        {

        }

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
        public string GetContentTable
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
