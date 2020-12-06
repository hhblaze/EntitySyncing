using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DBreeze.Utils;


namespace EntitySyncingClientTester
{
    class SyncEntity_Task_Client: EntitySyncingClient.EntitySyncingBaseV1
    {
        public override void Init(DBreeze.Transactions.Transaction tran, string entityTable)
        {
            base.Init(tran, entityTable);
        }

        public override void OnInsertEntity(long entityKey, object entityValue, object oldEntity, byte[] nonDeserializedEntity)
        {
            // base.OnInsertEntity(entityKey, entityValue, oldEntity, nonDeserializedEntity);
            var entity = (Entity_Task)entityValue;

            byte[] pBlob = null;
            pBlob = tran.InsertDataBlockWithFixedAddress(this.entityTable, pBlob, entity); //Entity is stored in the same table

            
            tran.Insert<byte[], byte[]>(this.entityTable, 200.ToIndex(entity.Id), pBlob);
            tran.Insert<byte[], byte[]>(this.entityTable, 201.ToIndex(entity.SyncTimestamp, entity.Id), pBlob);
        }

        //public override void OnEntitySyncIsFinished()
        //{
        //    base.OnEntitySyncIsFinished();
        //}

        //public override void BeforeCommit()
        //{
        //    base.BeforeCommit();
        //}
       
    }


   
}
