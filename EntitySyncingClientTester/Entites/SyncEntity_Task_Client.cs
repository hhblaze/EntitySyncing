using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DBreeze.Utils;
using EntitySyncingClient;

namespace EntitySyncingClientTester
{
    class SyncEntity_Task_Client: EntitySyncingClient.EntitySyncingBaseV1<Entity_Task>
    {
        public override void Init()
        {
            //Available:
            //this.tran
            //this.entityTable
            //this.GetContentTable
            //this.SyncingEngine
            //this.ptrContent            

            //Here extra DBreeze transaction tables can be synchronized (this.entityTable and probably this.entityContentTable (if it differes) must be also added in the SynchronizeTables)

            //List<string> tbls = new List<string>();
            //tbls.Add(this.entityTable);
            //tbls.Add(tblText);

            //tran.SynchronizeTables(tbls);

        }

        public override void OnInsertEntity(Entity_Task entity, Entity_Task oldEntity, byte[] nonDeserializedEntity, long changedID)
        {

            //That must be set first
            this.ptrContent = tran.InsertDataBlockWithFixedAddress(this.entityTable, this.ptrContent, entity); //Entity is stored in the same table                        
            
            //All sync indexes from here will be automatically filled up
        }

        //more overrides are available

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
