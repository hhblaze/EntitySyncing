using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DBreeze;
using DBreeze.Utils;

namespace EntitySyncingClientTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
                
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitSyncEngine();
        }

        public class LoggerWrapper : EntitySyncingClient.ILogger
        {
            public void LogException(string className, string methodName, Exception ex, string description)
            {
              
            }
        }

        public static EntitySyncingClient.Engine SyncEngineClient = null;
        public static EntitySyncingClient.Engine SyncEngineClient2 = null;
        public static EntitySyncing.Engine SyncEngine = null;

        EntitySyncingClient.ILogger LoggerClient = null;
        EntitySyncing.ILogger Logger = null;

        //EntitySyncingClient.ILogger Logger = null;
        DBreeze.DBreezeEngine DBEngineClient = null;
        DBreeze.DBreezeEngine DBEngineClient2 = null;
        DBreeze.DBreezeEngine DBEngine = null;


        void InitDBEngines()
        {
            if (DBEngineClient != null)
                return;


            DBEngineClient = new DBreezeEngine(textBox1.Text); // @"H:\c\tmp\synchronizer\client");
            DBEngineClient2 = new DBreezeEngine(textBox3.Text); // @"H:\c\tmp\synchronizer\client");
            DBEngine = new DBreezeEngine(textBox4.Text);
            
            DBreeze.Utils.CustomSerializator.ByteArraySerializator = EntitySyncingClientTester.ProtobufSerializer.SerializeProtobuf;
            DBreeze.Utils.CustomSerializator.ByteArrayDeSerializator = EntitySyncingClientTester.ProtobufSerializer.DeserializeProtobuf;
        }


        void InitSyncEngine()
        {
            if (SyncEngineClient != null)
                return;

            LoggerClient = new LoggerWrapper();

            InitDBEngines();

            SyncEngineClient = new EntitySyncingClient.Engine(LoggerClient, DBEngineClient, SendToServer, null, null);
            SyncEngineClient2 = new EntitySyncingClient.Engine(LoggerClient, DBEngineClient2, SendToServer, null, null);

            SyncEngine = new EntitySyncing.Engine(Logger, DBEngine);

            //Adding entites to be synced by this client
            //This is usually a one time operation. 
            //Entites will start to sync after calling
            //await SyncEngineClient.SynchronizeEntities();
            SyncEngineClient.AddEntity4Sync<Entity_Task>(new SyncEntity_Task_Client() { 
                urlSync = "/modules.http.GM_PersonalDevice/IDT_Actions", 
                entityTable = "Task1",
                //entityContentTable  - set up when necessary, DBreeze table for the Entity content differs from table with indexes
            });

            SyncEngineClient2.AddEntity4Sync<Entity_Task>(new SyncEntity_Task_Client()
            {
                urlSync = "/modules.http.GM_PersonalDevice/IDT_Actions",
                entityTable = "Task1"
                //entityContentTable  - set up when necessary, DBreeze table for the Entity content differs from table with indexes
            });

        }

        /// <summary>
        /// Emulates sending entity to server and returning back an aswer from the server
        /// </summary>
        /// <param name="page"></param>
        /// <param name="type"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public async Task<EntitySyncingClient.HttpCapsule> SendToServer(string page, string type, object body)
        {
            var capsIn= new EntitySyncing.HttpCapsule
            {
                 Type = type,
                  Body = (byte[])body
            };


            var capsOut = SyncEngine.SyncEntityV1<Entity_Task_Server>(capsIn, new SyncEntity_Task_Server() { 
                entityTable = "TaskSyncUser1" 
            }
            , new byte[] { 1, 1, 1, 1 }, 
            EntitySyncing.eSynchroDirectionType.Both, true); //new byte[] { 1, 1, 1, 1 } is user

            return new EntitySyncingClient.HttpCapsule { 
                Body = capsOut.Body,
                Type = capsOut.Type
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SyncClient1();
        }

        async Task SyncClient1()
        {
            await SyncEngineClient.SynchronizeEntities();
        }

        async Task SyncClient2()
        {
            await SyncEngineClient2.SynchronizeEntities();
        }


        /// <summary>
        /// Not used util
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="table"></param>
        /// <param name="counterAddress"></param>
        /// <returns></returns>
        long GetTableCounterLong(DBreeze.Transactions.Transaction tran, string table, byte[] counterAddress)
        {
            long counter = 1;
            var row = tran.Select<byte[], long>(table, counterAddress);
            if(!row.Exists)
            {
                tran.Insert<byte[], long>(table, counterAddress, 1);
                return counter;
            }

            counter = row.Value;

            counter++;
            tran.Insert(table, counterAddress, counter);
            return counter;
        }


        #region "test client 1 inserts"
        /// <summary>
        /// Insert client fixed ID (emulating possible mistake)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {           
            DateTime now = DateTime.UtcNow;
            string table = "Task1";

            Entity_Task entity = new Entity_Task()
            {
                Description = "Client 1 " + now.Ticks,              
                Id = 2,
                SyncTimestamp = now.Ticks
            };
            using (var tran = DBEngineClient.GetTransaction())
            {
                //tran.SynchronizeTables()  - must be called when necessary also adding sync indexes table

                byte[] pBlob = null;
                //First inserting blob (entity content)
                pBlob = tran.InsertDataBlockWithFixedAddress<Entity_Task>(table, pBlob, entity); //Entity is stored in the same table

                //Then calling sync indexes also with the pointer to the entity content (row.Value in this case) 
                EntitySyncingClient.SyncStrategyV1<Entity_Task>.InsertIndex4Sync(tran, table, entity, pBlob, null);                

                tran.Commit();
            }
        }

        /// <summary>
        /// Inserting normal ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            string table = "Task1";

            Entity_Task entity = new Entity_Task()
            {
                Description = "Client 1 " + now.Ticks,
                Id = now.Ticks,                
                SyncTimestamp = now.Ticks
            };
            using (var tran = DBEngineClient.GetTransaction())
            {
                //tran.SynchronizeTables()  - must be called when necessary also adding sync indexes table

                byte[] pBlob = null;
                //First inserting blob (entity content)
                pBlob = tran.InsertDataBlockWithFixedAddress<Entity_Task>(table, pBlob, entity); //Entity is stored in the same table

                //Then calling sync indexes also with the pointer to the entity content (row.Value in this case) 
                EntitySyncingClient.SyncStrategyV1<Entity_Task>.InsertIndex4Sync(tran, table, entity, pBlob, null);

                tran.Commit();
            }
        }

        /// <summary>
        /// List client elements
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {            
            Console.WriteLine("----client 1 list------");
            using (var tran = DBEngineClient.GetTransaction())
            {
                string table1 = "Task1";
                foreach (var row in tran.SelectForward<byte[], byte[]>(table1))
                {
                    //if (row.Key[0] != 200)
                    //    continue;

                    if (row.Key[0] == 200)
                    {
                        var ent = tran.SelectDataBlockWithFixedAddress<Entity_Task>(table1, row.Value);
                        Console.WriteLine(row.Key.ToBytesString() + $"  ID: {row.Key.Substring(1, 8).To_Int64_BigEndian()}; + { new DateTime(ent.SyncTimestamp).ToString("dd.MM.yyyy HH:mm:ss")}; {ent.Description} ");
                    }
                    else
                        Console.WriteLine(row.Key.ToBytesString() + "   ex");
                }
            }

        }

        private void UpdateClientID(long id)
        {
            string table = "Task1";
            DateTime now = DateTime.UtcNow;

            using (var tran = DBEngineClient.GetTransaction())
            {

                var row = tran.Select<byte[], byte[]>(table, 200.ToIndex(id));
                if (row.Exists)
                {

                    var oldEnt = tran.SelectDataBlockWithFixedAddress<Entity_Task>(table, row.Value);
                    var newEnt = oldEnt.CloneProtobuf();

                    newEnt.SyncTimestamp = now.Ticks;
                    newEnt.Description = "by Client 1 " + newEnt.SyncTimestamp;

                    //First inserting blob (entity content)
                    tran.InsertDataBlockWithFixedAddress<Entity_Task>(table, row.Value, newEnt); //Entity is stored in the same table

                    //Then calling sync indexes also with the pointer to the entity content (row.Value in this case) 
                    EntitySyncingClient.SyncStrategyV1<Entity_Task>.InsertIndex4Sync(tran, table, newEnt, row.Value, oldEnt);                   

                    tran.Commit();
                }
            }

        }


        private void button6_Click(object sender, EventArgs e)
        {
            UpdateClientID(Convert.ToInt64(textBox2.Text));
        }
        #endregion


        #region "test server"
        /// <summary>
        /// Insert server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {

            DateTime now = DateTime.UtcNow;
            string table = "TaskSyncUser1";

            //Adding task 
            using (var tran = DBEngine.GetTransaction())
            {
                //tran.SynchronizeTables()  - must be called when necessary also adding sync indexes table

                Entity_Task_Server entity = new Entity_Task_Server()
                {
                    Description = "w1 " + now.Ticks,
                    //Id = 1,
                    Id = now.Ticks,
                    SyncTimestamp = now.Ticks
                };


                byte[] pBlob = null;
                pBlob = tran.InsertDataBlockWithFixedAddress<Entity_Task_Server>(table, pBlob, entity); //Entity is stored in the same table

                //must be called to insert synchro indexes
                SyncEngine.InsertIndex4Sync(tran, table, entity, pBlob, null);

                tran.Commit();
            }
        }


        /// <summary>
        /// List server elements
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {          
            Console.WriteLine("----server list------");
            using (var tran = DBEngine.GetTransaction())
            {
                string table1 = "TaskSyncUser1";
                foreach (var row in tran.SelectForward<byte[], byte[]>(table1))
                {

                    if (row.Key[0] == 200)
                    {
                        var ent = tran.SelectDataBlockWithFixedAddress<Entity_Task>(table1, row.Value);
                        Console.WriteLine(row.Key.ToBytesString() + $"  ID: {row.Key.Substring(1, 8).To_Int64_BigEndian()}; + { new DateTime(ent.SyncTimestamp).ToString("dd.MM.yyyy HH:mm:ss")}; {ent.Description} ");
                    }
                    else
                    {
                        Console.WriteLine(row.Key.ToBytesString() + "   ex");
                    }
                }
            }
        }


        private void UpdateServerID(long id)
        {  
            string table = "TaskSyncUser1";
            DateTime now = DateTime.UtcNow;

            using (var tran = DBEngine.GetTransaction())
            {
                //tran.SynchronizeTables()  - must be called when necessary also adding sync indexes table

                var row = tran.Select<byte[], byte[]>(table, 200.ToIndex(id));
                if (row.Exists)
                {

                    var oldEnt = tran.SelectDataBlockWithFixedAddress<Entity_Task_Server>(table, row.Value);
                    var newEnt = oldEnt.CloneProtobuf();

                    newEnt.SyncTimestamp = now.Ticks;
                    newEnt.Description = "by Server " + newEnt.SyncTimestamp;

                    tran.InsertDataBlockWithFixedAddress<Entity_Task_Server>(table, row.Value, newEnt); //Entity is stored in the same table

                    //must be called to insert synchro indexes
                    SyncEngine.InsertIndex4Sync(tran, table, newEnt, row.Value, oldEnt);

                    tran.Commit();
                }
            }

        }


        private void button7_Click(object sender, EventArgs e)
        {
            UpdateServerID(Convert.ToInt64(textBox2.Text));
        }


        #endregion


        private void button8_Click(object sender, EventArgs e)
        {
            SyncClient2();
        }

        #region "Client 3"

        private void button11_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            string table = "Task1";

            Entity_Task entity = new Entity_Task()
            {
                Description = "Client 2 " + now.Ticks,
                Id = now.Ticks,
                SyncTimestamp = now.Ticks
            };
            using (var tran = DBEngineClient2.GetTransaction())
            {
                //tran.SynchronizeTables()  - must be called when necessary also adding sync indexes table

                byte[] pBlob = null;
                //First inserting blob (entity content)
                pBlob = tran.InsertDataBlockWithFixedAddress<Entity_Task>(table, pBlob, entity); //Entity is stored in the same table

                //Then calling sync indexes also with the pointer to the entity content (row.Value in this case) 
                EntitySyncingClient.SyncStrategyV1<Entity_Task>.InsertIndex4Sync(tran, table, entity, pBlob, null);

                tran.Commit();
            }
        }


        private void UpdateClient2ID(long id)
        {
            string table = "Task1";
            DateTime now = DateTime.UtcNow;

            using (var tran = DBEngineClient2.GetTransaction())
            {

                var row = tran.Select<byte[], byte[]>(table, 200.ToIndex(id));
                if (row.Exists)
                {

                    var oldEnt = tran.SelectDataBlockWithFixedAddress<Entity_Task>(table, row.Value);
                    var newEnt = oldEnt.CloneProtobuf();

                    newEnt.SyncTimestamp = now.Ticks;
                    newEnt.Description = "by Client 2 " + newEnt.SyncTimestamp;

                    //First inserting blob (entity content)
                    tran.InsertDataBlockWithFixedAddress<Entity_Task>(table, row.Value, newEnt); //Entity is stored in the same table

                    //Then calling sync indexes also with the pointer to the entity content (row.Value in this case) 
                    EntitySyncingClient.SyncStrategyV1<Entity_Task>.InsertIndex4Sync(tran, table, newEnt, row.Value, oldEnt);

                    tran.Commit();
                }
            }

        }


        private void button13_Click(object sender, EventArgs e)
        {
            UpdateClient2ID(Convert.ToInt64(textBox2.Text));
        }

        private void button9_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            string table = "Task1";

            Entity_Task entity = new Entity_Task()
            {
                Description = "Client 2 " + now.Ticks,
                Id = 2,
                SyncTimestamp = now.Ticks
            };
            using (var tran = DBEngineClient2.GetTransaction())
            {
                //tran.SynchronizeTables()  - must be called when necessary also adding sync indexes table

                byte[] pBlob = null;
                //First inserting blob (entity content)
                pBlob = tran.InsertDataBlockWithFixedAddress<Entity_Task>(table, pBlob, entity); //Entity is stored in the same table

                //Then calling sync indexes also with the pointer to the entity content (row.Value in this case) 
                EntitySyncingClient.SyncStrategyV1<Entity_Task>.InsertIndex4Sync(tran, table, entity, pBlob, null);

                tran.Commit();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Console.WriteLine("----client 2 list------");
            using (var tran = DBEngineClient2.GetTransaction())
            {
                string table1 = "Task1";
                foreach (var row in tran.SelectForward<byte[], byte[]>(table1))
                {
                    //if (row.Key[0] != 200)
                    //    continue;

                    if (row.Key[0] == 200)
                    {
                        var ent = tran.SelectDataBlockWithFixedAddress<Entity_Task>(table1, row.Value);
                        Console.WriteLine(row.Key.ToBytesString() + $"  ID: {row.Key.Substring(1, 8).To_Int64_BigEndian()}; + { new DateTime(ent.SyncTimestamp).ToString("dd.MM.yyyy HH:mm:ss")}; {ent.Description} ");
                    }
                    else
                        Console.WriteLine(row.Key.ToBytesString() + "   ex");
                }
            }
        }
        #endregion



        private void button14_Click(object sender, EventArgs e)
        {
            var resbof = BiserObjectify.Generator.Run(typeof(EntitySyncing.SyncOperation), true,
     @"H:\c\tmp\synchronizer\", forBiserBinary: true, forBiserJson: false, null);

            
        }

       




    }
}
