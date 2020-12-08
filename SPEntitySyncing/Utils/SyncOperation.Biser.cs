using System;
using System.Collections.Generic;
using System.Text;
using DBreeze.Utils;

namespace EntitySyncing
{
    public partial class SyncOperation : Biser.IEncoder
    {
        public Biser.Encoder BiserEncoder(Biser.Encoder existingEncoder = null)
        {
            Biser.Encoder encoder = new Biser.Encoder(existingEncoder);


            encoder.Add(SyncTimestamp);
            encoder.Add(SerializedObject);
            encoder.Add((int)Operation);
            encoder.Add(Type);
            encoder.Add(InternalId);
            encoder.Add(ExternalId);

            return encoder;
        }


        public static SyncOperation BiserDecode(byte[] enc = null, Biser.Decoder extDecoder = null)
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

            SyncOperation m = new SyncOperation();



            m.SyncTimestamp = decoder.GetLong();
            m.SerializedObject = decoder.GetByteArray();
            m.Operation = (eOperation)decoder.GetInt();
            m.Type = decoder.GetString();
            m.InternalId = decoder.GetLong();
            m.ExternalId = decoder.GetLong();


            return m;
        }


    }
}
