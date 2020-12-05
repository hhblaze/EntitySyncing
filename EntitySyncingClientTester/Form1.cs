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


        public class LoggerWrapper : EntitySyncingClient.ILogger
        {
            public void LogException(string className, string methodName, Exception ex, string description)
            {
              
            }
        }

        EntitySyncingClient.Engine SyncEngineClient = null;
        EntitySyncing.Engine SyncEngine = null;

        EntitySyncingClient.ILogger LoggerClient = null;
        EntitySyncing.ILogger Logger = null;

        //EntitySyncingClient.ILogger Logger = null;
        DBreeze.DBreezeEngine DBEngineClient = null;
        DBreeze.DBreezeEngine DBEngine = null;


        void InitDBEngines()
        {
            if (DBEngineClient != null)
                return;


            DBEngineClient = new DBreezeEngine(textBox1.Text); // @"H:\c\tmp\synchronizer\client");
            if(cbServerInit.Checked)
                DBEngine = new DBreezeEngine(@"H:\c\tmp\synchronizer\server");
            
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

            if (cbServerInit.Checked)
                SyncEngine = new EntitySyncing.Engine(Logger, DBEngine);

            //!!!!!!! Add entity table also inside of SyncEntity_Task
            //Adding entites to be synced
            SyncEngineClient.AddSyncEntityV1<Entity_Task_Client>("Task1", new SyncEntity_Task_Client() { UrlSync = "/modules.http.GM_PersonalDevice/IDT_Actions" });

        }

        /// <summary>
        /// Emulates giving entity to server
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
            , new byte[] { 1, 1, 1, 1 }, EntitySyncing.eSynchroDirectionType.Both, true);

            return new EntitySyncingClient.HttpCapsule { 
                Body = capsOut.Body,
                Type = capsOut.Type
            };
        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.InitSyncEngine();

            Run();
        }

        async Task Run()
        {
         


            await SyncEngineClient.SynchronizeEntities();
        }




        private void button2_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            string table = "TaskSyncUser1";

            InitDBEngines();

            //Adding task 
            using (var tran = DBEngine.GetTransaction())
            {


                //foreach(var row in tran.SelectForward<byte[],byte[]>(table))
                //{
                //    Console.WriteLine(row.Key.ToBytesString());
                //}



                //var cnt = GetTableCounterLong(tran, table, new byte[] { 1 });

                Entity_Task_Server entity = new Entity_Task_Server()
                {
                    Description = "w1 " + now.Ticks,
                    //Id = cnt,
                    Id = now.Ticks,
                    SyncTimestamp = now.Ticks
                };

                
                byte[] pBlob = null;
                pBlob = tran.InsertDataBlockWithFixedAddress<Entity_Task_Server>(table, pBlob, entity); //Entity is stored in the same table
                

                tran.Insert<byte[], byte[]>(table, 200.ToIndex(entity.Id), pBlob);
                tran.Insert<byte[], byte[]>(table, 201.ToIndex(now.Ticks, entity.Id), pBlob);

                tran.Commit();
            }
        }


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


        private void button3_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            string table = "Task1";

            InitDBEngines();

            Entity_Task_Client entity = new Entity_Task_Client()
            {
                Description = "cl1   " + now.Ticks,
                Id = now.Ticks,
                SyncTimestamp = now.Ticks
            };
            using (var tran = DBEngineClient.GetTransaction())
            {

                byte[] pBlob = null;
                pBlob = tran.InsertDataBlockWithFixedAddress<Entity_Task_Client>(table, pBlob, entity); //Entity is stored in the same table


                tran.Insert<byte[], byte[]>(table, 200.ToIndex(entity.Id), pBlob);
                tran.Insert<byte[], byte[]>(table, 201.ToIndex(now.Ticks, entity.Id), pBlob);

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
            InitDBEngines();
            Console.WriteLine("----client list------");
            using (var tran = DBEngineClient.GetTransaction())
            {
                string table1 = "Task1";
                foreach (var row in tran.SelectForward<byte[], byte[]>(table1))
                {
                    if (row.Key[0] != 200)
                        continue;

                    var ent = tran.SelectDataBlockWithFixedAddress<Entity_Task_Client>(table1, row.Value);

                    Console.WriteLine(row.Key.ToBytesString() + $"  ID: {row.Key.Substring(1,8).To_Int64_BigEndian()}; + { new DateTime(ent.SyncTimestamp).ToString("dd.MM.yyyy HH:mm:ss")}; {ent.Description} " );
                }
            }

        }

        /// <summary>
        /// List server elements
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            InitDBEngines();
            Console.WriteLine("----server list------");
            using (var tran = DBEngine.GetTransaction())
            {
                string table1 = "TaskSyncUser1";
                foreach (var row in tran.SelectForward<byte[], byte[]>(table1))
                {
                    if (row.Key[0] != 200)
                        continue;

                    var ent = tran.SelectDataBlockWithFixedAddress<Entity_Task_Server>(table1, row.Value);

                    Console.WriteLine(row.Key.ToBytesString() + $"  ID: {row.Key.Substring(1, 8).To_Int64_BigEndian()}; { new DateTime(ent.SyncTimestamp).ToString("dd.MM.yyyy HH:mm:ss")}; {ent.Description} ");
                }
            }
        }


        private void UpdateClientID(long id)
        {
            InitDBEngines();            
            string table = "Task1";
            DateTime now = DateTime.UtcNow;

            using (var tran = DBEngineClient.GetTransaction())
            {
                
                var row = tran.Select<byte[], byte[]>(table, 200.ToIndex(id));
                if(row.Exists)
                {

                    var ent = tran.SelectDataBlockWithFixedAddress<Entity_Task_Client>(table, row.Value);
                    ent.SyncTimestamp = now.Ticks;
                    ent.Description = "cln desc from" + ent.SyncTimestamp;

                    tran.InsertDataBlockWithFixedAddress<Entity_Task_Client>(table, row.Value, ent); //Entity is stored in the same table

                    //tran.Insert<byte[], byte[]>(table, 200.ToIndex(ent.Id), pBlob); 
                    tran.Insert<byte[], byte[]>(table, 201.ToIndex(ent.SyncTimestamp, ent.Id), row.Value);

                    tran.Commit();
                }
            }

        }

        private void UpdateServerID(long id)
        {
            InitDBEngines();
            string table = "TaskSyncUser1";
            DateTime now = DateTime.UtcNow;

            using (var tran = DBEngine.GetTransaction())
            {

                var row = tran.Select<byte[], byte[]>(table, 200.ToIndex(id));
                if (row.Exists)
                {

                    var ent = tran.SelectDataBlockWithFixedAddress<Entity_Task_Client>(table, row.Value);
                    ent.SyncTimestamp = now.Ticks;
                    ent.Description = "srv desc from" + ent.SyncTimestamp;

                    tran.InsertDataBlockWithFixedAddress<Entity_Task_Client>(table, row.Value, ent); //Entity is stored in the same table

                    //tran.Insert<byte[], byte[]>(table, 200.ToIndex(ent.Id), pBlob); 
                    tran.Insert<byte[], byte[]>(table, 201.ToIndex(ent.SyncTimestamp, ent.Id), row.Value);

                    tran.Commit();
                }
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            UpdateClientID(Convert.ToInt64(textBox2.Text));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            UpdateServerID(Convert.ToInt64(textBox2.Text));
        }
    }
}
