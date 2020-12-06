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
        //List<GM_IDoThings.TaskDescriptionTemplate> ents = new List<GM_IDoThings.TaskDescriptionTemplate>();
        //string tblText = "";
        //string mcTbl = "";
        //long companyId = -1;

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


        //public override bool OnInsertEntity(long entityKey, GM_IDoThings.TaskDescriptionTemplate entity, GM_IDoThings.TaskDescriptionTemplate oldEntity, long newEntitySyncTimestamp)
        //{
        //    entity.SyncTimestamp = newEntitySyncTimestamp;


        //    bool entityExist = oldEntity != null;

        //    if (entityExist)
        //    {
        //        //Update
        //        //Reforming secondary indicies                
        //        entity.CreatorId = oldEntity.CreatorId;
        //    }
        //    else
        //    {
        //        entity.CreatorId = this.user.Id;
        //    }

        //    //en.Id = entityKey; //!!!!!!!!!!!  THEY MUST BE EQUAL CHECK IF IT IS SO
        //    //en.CompanyID = this.user.CompanyId;
        //    //Inserting value
        //    //this.refToValueDataBlockWithFixedAddress is either set or null if entity is new
        //    this.refToValueDataBlockWithFixedAddress = tran.InsertDataBlockWithFixedAddress<GM_IDoThings.TaskDescriptionTemplate>(entityTable, this.refToValueDataBlockWithFixedAddress, entity);
        //    //Inserting primary key
        //    if (!entityExist)
        //        tran.Insert<byte[], byte[]>(entityTable, new byte[] { 200 }.Concat(entityKey.To_8_bytes_array_BigEndian()), this.refToValueDataBlockWithFixedAddress);

        //    //Handling search words
        //    // tran.TextInsert(this.entityTable + "_TEXT", entityKey.To_8_bytes_array_BigEndian(), en.GetSearchWordsContains(), fullMatchWords: en.GetFull()
        //    tran.TextInsert(tblText, entityKey.To_8_bytes_array_BigEndian(), entity.GetSearchWordsContains() ?? String.Empty, entity.GetSearchWordsFull() ?? String.Empty,
        //        deferredIndexing: true);

        //    ents.Add(entity);

        //    return true;    //Yes we insert new entity
        //}

        public override void AfterCommit()
        {
            base.AfterCommit();

            ////No MCache
            //using (var mctran = MCache.CreateTransaction())
            //{
            //    foreach (var ent in ents)
            //    {
            //        ISyncEntity sent = ent as ISyncEntity;

            //        mctran.Remove<string>(this.mcTbl, this.companyId + "," + sent.Id);

            //        if (!sent.Deleted)
            //            mctran.Insert<string, GM_IDoThings.TaskDescriptionTemplate>(this.mcTbl, this.companyId + "," + sent.Id, ent);
            //    }

            //    mctran.Publish();
            //}
        }

    }
}
