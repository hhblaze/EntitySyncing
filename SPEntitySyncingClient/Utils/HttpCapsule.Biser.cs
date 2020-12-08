using DBreeze.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    public partial class HttpCapsule : Biser.IEncoder
    {


        public Biser.Encoder BiserEncoder(Biser.Encoder existingEncoder = null)
        {
            Biser.Encoder encoder = new Biser.Encoder(existingEncoder);


            encoder.Add(Action);
            encoder.Add(EntityType);
            encoder.Add(IsOk);
            encoder.Add(Body);

            return encoder;
        }


        public static HttpCapsule BiserDecode(byte[] enc = null, Biser.Decoder extDecoder = null)
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

            HttpCapsule m = new HttpCapsule();



            m.Action = decoder.GetString();
            m.EntityType = decoder.GetString();
            m.IsOk = decoder.GetBool();
            m.Body = decoder.GetByteArray();


            return m;
        }


    }
}
