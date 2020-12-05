using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    public abstract class EntitySyncingBaseV1
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityTable"></param>
        /// <param name="entityKey"></param>
        /// <param name="entityValue"></param>
        /// <param name="oldEntity"></param>
        /// <param name="nonDeserializedEntity">Very important save exactly this entity, because it must have more properties then not updated client</param>
        public virtual void OnInsertEntity(long entityKey, object entityValue, object oldEntity, byte[] nonDeserializedEntity)
        {

        }

        public virtual void OnEntitySyncIsFinished()
        {

        }

        public virtual void BeforeCommit()
        {

        }

        protected DBreeze.Transactions.Transaction tran = null;
        protected string entityTable = "";
        public byte[] refToValueDataBlockWithFixedAddress = null;

        /// <summary>
        /// Example "/modules.http.GM_PersonalDevice/IDT_Actions"
        /// </summary>
        public string UrlSync = String.Empty;

        public virtual void Init(DBreeze.Transactions.Transaction tran, string entityTable)
        {
            this.tran = tran;
            this.entityTable = entityTable;
        }

    }
}
