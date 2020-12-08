using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBreeze.Utils;

namespace EntitySyncing
{
    public class SyncStrategyV1<T>
    {


        /// <summary>
        /// Fills up index 200. Creation of transaction, synchronization of the table and transaction commit is outside of this function.
        /// Entity desired ID and SyncTimestamp must be specified
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="table">Where must be stored index 200</param>
        /// <param name="entity">entity.Id and entity.SyncTimestamp must be filled up</param>
        /// <param name="ptrEntityContent">pointer to the entity content (16 bytes) gathered with DBreeze InsertDataBlockWithFixedAddress</param>
        /// <param name="oldEntity">old instance of the entity from DB</param>
        public static void InsertIndex4Sync(DBreeze.Transactions.Transaction tran, string table, ISyncEntity entity, byte[] ptrEntityContent, ISyncEntity oldEntity)
        {
            if (oldEntity == null)
                tran.Insert<byte[], byte[]>(table, 200.ToIndex(entity.Id), ptrEntityContent);

            tran.Insert<byte[], byte[]>(table, 201.ToIndex(entity.SyncTimestamp, entity.Id), null);
        }

    }//eo class
}
