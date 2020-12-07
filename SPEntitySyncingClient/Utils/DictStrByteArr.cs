using System;
using System.Collections.Generic;
using System.Text;
using DBreeze.Utils;

namespace EntitySyncingClient
{
    /// <summary>
    /// Dictionary of string,byte[]
    /// </summary>
    public partial class DictStrByteArr
    {
        /// <summary>
        /// Instantiated
        /// </summary>
        public Dictionary<string, byte[]> d { get; set; } = new Dictionary<string, byte[]>();
    }

    public partial class DictStrByteArr : Biser.IEncoder
    {


        public Biser.Encoder BiserEncoder(Biser.Encoder existingEncoder = null)
        {
            Biser.Encoder encoder = new Biser.Encoder(existingEncoder);


            encoder.Add(d, (r1) => {
                encoder.Add(r1.Key);
                encoder.Add(r1.Value);
            });

            return encoder;
        }


        public static DictStrByteArr BiserDecode(byte[] enc = null, Biser.Decoder extDecoder = null)
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

            DictStrByteArr m = new DictStrByteArr();



            m.d = decoder.CheckNull() ? null : new System.Collections.Generic.Dictionary<System.String, System.Byte[]>();
            if (m.d != null)
            {
                decoder.GetCollection(() => {
                    var pvar1 = decoder.GetString();
                    return pvar1;
                },
            () => {
                var pvar2 = decoder.GetByteArray();
                return pvar2;
            }, m.d, true);
            }


            return m;
        }


    }
}
