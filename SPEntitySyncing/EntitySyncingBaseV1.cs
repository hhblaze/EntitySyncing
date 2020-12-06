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
        /// <param name="entityTable"></param>
        /// <param name="entityKey"></param>
        /// <param name="entity"></param>
        /// <param name="oldEntity"></param>
        /// <param name="newEntitySyncTimestamp"></param>
        /// <returns>TRUE - indicates that incoming entity must be applied. FALSE means that this entity is not so strong by political reasons as server-side one and must be changed with server-side.</returns>
        public virtual bool OnInsertEntity(T entity, T oldEntity)
        {
            return true;
        }


        protected DBreeze.Transactions.Transaction tran = null;
        public string entityTable = "";
        /// <summary>
        /// In case if value of the entity differs from the table where synchronizer holds data
        /// </summary>
        public string entityValueTable = "";
        /// <summary>
        /// Is set by synchronizer, in case if entity exists, then must be set from onInsertEntityFunction after repeat insert
        /// </summary>
        public byte[] refToValueDataBlockWithFixedAddress = null;
        //protected GccObjects.Net.UserManagement.GccUserRemoteView user = null;
        protected object user = null;

        public long TopicId = 0; //In case if synchronization comes from the specific APP
        public bool TopicIdDependantSync = false;

        public virtual bool CheckTopicIdEntity(T entityValue)
        {
            return true;
        }

        /// <summary>
        /// Also responisble for Synchronize table, by default we synchronize only one master table, so we don't need extra tran.Synchronize tables
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="entityTable"></param>
        /// <param name="companyId"></param>
        /// <param name="syncOperations"></param>
        public virtual void Init(DBreeze.Transactions.Transaction tran, List<SyncOperation> syncOperations, object user, string TopicName = "")
        {
            //GccObjects.Net.UserManagement.GccUserRemoteView
            this.tran = tran;
            this.user = user;
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
