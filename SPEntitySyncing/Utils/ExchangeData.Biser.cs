using System;
using System.Collections.Generic;
using System.Text;
using DBreeze.Utils;

namespace EntitySyncing
{
    public partial class ExchangeData : Biser.IEncoder
    {


        public Biser.Encoder BiserEncoder(Biser.Encoder existingEncoder = null)
        {
            Biser.Encoder encoder = new Biser.Encoder(existingEncoder);


            encoder.Add(LastServerSyncTimeStamp);
            encoder.Add(SyncOperations, (r1) => {
                encoder.Add(r1);
            });
            encoder.Add(RepeatSynchro);
            encoder.Add(NewServerSyncTimeStamp);

            return encoder;
        }


        public static ExchangeData BiserDecode(byte[] enc = null, Biser.Decoder extDecoder = null)
        {
            Biser.Decoder decoder = null;
            if (extDecoder == null)
            {
                if (enc == null || enc.Length == 0)
                    return null;
                decoder = new Biser.Decoder(enc);
            }
            else
            {
                if (extDecoder.CheckNull())
                    return null;
                else
                    decoder = extDecoder;
            }

            ExchangeData m = new ExchangeData();



            m.LastServerSyncTimeStamp = decoder.GetLong();
            m.SyncOperations = decoder.CheckNull() ? null : new System.Collections.Generic.List<SyncOperation>();
            if (m.SyncOperations != null)
            {
                decoder.GetCollection(() => {
                    var pvar1 = SyncOperation.BiserDecode(null, decoder);
                    return pvar1;
                }, m.SyncOperations, true);
            }
            m.RepeatSynchro = decoder.GetBool();
            m.NewServerSyncTimeStamp = decoder.GetLong();


            return m;
        }


    }
}
