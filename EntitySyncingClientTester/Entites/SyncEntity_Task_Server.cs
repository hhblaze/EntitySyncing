using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DBreeze;
using DBreeze.Utils;
using EntitySyncing;

namespace EntitySyncingClientTester
{ 

    class SyncEntity_Task_Server : EntitySyncingBaseV1<Entity_Task_Server>
    {
        public override void Init()
            
        {
            //Available:

            //this.tran
            //this.entityTable
            //this.GetContentTable
            //this.SyncingEngine
            //this.ptrContent
            //this.userToken



            //Here extra DBreeze transaction tables can be synchronized (this.entityTable and probably this.entityContentTable (if it differes) must be also added in the SynchronizeTables)

            //List<string> tbls = new List<string>();
            //tbls.Add(this.entityTable);
            //tbls.Add(tblText);

            //tran.SynchronizeTables(tbls);

        }

        public override bool OnInsertEntity(Entity_Task_Server entity, Entity_Task_Server oldEntity)
        {
            //at this moment
            if(oldEntity == null)
            {
                //It is possible (but not necessary by default) to re-assign entity.Id right here                
            }

            
            //this.entityValueTable in case if entity content is stored in the other table then indexes for sync operations
            this.ptrContent = tran.InsertDataBlockWithFixedAddress<Entity_Task_Server>(this.entityTable, this.ptrContent, entity); //Entity is stored in the same table

            //Sync indexes will be handled automatically if return true, also based on entity and this.refToValueDataBlockWithFixedAddress value

            //Other indexes can be handled here
            //tran.TextInsert(tblText, entityKey.To_8_bytes_array_BigEndian(), entity.GetSearchWordsContains() ?? String.Empty, entity.GetSearchWordsFull() ?? String.Empty,
            //  deferredIndexing: true);

            return true;    //Yes we insert new entity
        }


        public override void AfterCommit()
        {
            base.AfterCommit();

        }

    }
}
