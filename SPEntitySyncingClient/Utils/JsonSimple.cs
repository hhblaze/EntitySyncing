using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncingClient
{
    internal static class JsonSimple
    {

        public static string SerializeJsonSimple(this Dictionary<string, string> dict)
        {
            return SerializeDictionaryInternal(dict);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        static string SerializeDictionaryInternal(Dictionary<string, string> dict)
        {
            /*
             {}\' are special symbols must be followed by \ to be as a text 
             {}' must not be followed by \ to be as an object element
             */
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                string t = "";
                foreach (var el in dict)
                {
                    sb.Append("'");
                    sb.Append(el.Key);
                    sb.Append("'");
                    sb.Append(":");
                    sb.Append("'");
                    t = el.Value.Replace("\\", "\\\\").Replace("'", "\'"); //In case when value is not an object (doesn't start from curly bracket)
                    sb.Append(t);
                    sb.Append("'");
                    sb.Append(";");
                }
                sb.Append("}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /*  EXAMPLE SUPPLY
         
         pdfUpload{'DocumentSpace':'pdfTextSearch';'Type':'user';'ExternalID':'DOCID1'}
         * 
         or 
         * 
         pdfUpload{'DocumentSpace'='pdfTextSearch';'Type'='user';'ExternalID'='DOCID1'}
         *
         * or
         * {'DocumentSpace''pdfTextSearch''Type''user''ExternalID'='DO\'\{\}CID1'}
         * 
         or
         * 
         { 
            'internalObject3'=
                '{
                    'key1'='val1';
                    'key2':'{'k2':'v2'}';
                    'key3':'{'k3':'v3'}'
                }';
            'io2' = 'kuku';
            'io3' = 'kuku1';
            'io4' : 'kuku2'
        }
         
         */


        /// <summary>
        /// EXAMPLE SUPPLY search right in the class
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Dictionary<string, string> DeserializeJsonSimple(this string json)
        {
            return DeserializeDictionaryInternal(json);
        }

        /// <summary>
        /// Parses signle value of type Object. Object starts from { and ends with }
        /// </summary>
        class ObjectParser
        {
            //TODO get rid of object recursion, come to standart in case of deserialization to normal object

            enum eState
            {
                BEGIN,
                KEY,
                VALUE,
                KEY_END
            }

            enum eValueState
            {
                Unknown,
                String,
                Object,
                ObjectEnded
            }

            string _json = "";

            public ObjectParser(string json)
            {
                _json = json;
            }

            /// <summary>
            /// Self Key/Value pair
            /// </summary>
            public Dictionary<string, string> kvp = new Dictionary<string, string>();
            /// <summary>
            /// Accumulator of self representation inside of json string (can be limited value due to different startFrom)
            /// Will be not necessary in case of converting to real object
            /// </summary>
            public StringBuilder MSB = new StringBuilder();

            public int Start(int startFrom)
            {
                try
                {

                    eState state = eState.BEGIN;
                    StringBuilder sbKey = new StringBuilder();
                    StringBuilder sbValue = new StringBuilder();
                    bool useNextSymbolAsChar = false;
                    //If value is not a string but internal object {'':'';'':''...}
                    eValueState valueState = eValueState.Unknown;
                    char c = ' ';
                    ObjectParser op = null;

                    //System.Diagnostics.Debug.WriteLine("Start from " + startFrom);


                    for (int i = startFrom; i < _json.Length; i++)
                    {
                        c = _json[i];

                        switch (state)
                        {
                            case eState.BEGIN:
                                if (c == '\'')
                                {
                                    MSB.Append(c);
                                    state = eState.KEY;
                                }
                                else if (c == '{')   //Satrt of an object
                                {
                                    MSB.Append(c);
                                    continue;
                                }
                                else if (c == '}') //End of object
                                {
                                    MSB.Append(c);
                                    return i;
                                }
                                continue;
                            case eState.KEY:
                                if (useNextSymbolAsChar)
                                {
                                    sbKey.Append(c);
                                    useNextSymbolAsChar = false;
                                    MSB.Append(c);
                                    continue;
                                }
                                if (c == '\'')
                                {
                                    state = eState.KEY_END;
                                    MSB.Append(c);
                                }
                                else if (c == '\\')
                                {
                                    useNextSymbolAsChar = true;
                                    MSB.Append(c);
                                }
                                else
                                {
                                    sbKey.Append(c);
                                    MSB.Append(c);
                                }
                                continue;
                            case eState.KEY_END:
                                if (c == '\'')
                                {
                                    state = eState.VALUE;
                                    MSB.Append(c);
                                }
                                continue;
                            case eState.VALUE:
                                if (useNextSymbolAsChar)
                                {
                                    sbValue.Append(c);
                                    useNextSymbolAsChar = false;
                                    MSB.Append(c);
                                    continue;
                                }

                                if (valueState == eValueState.Unknown && c == '{')
                                {
                                    //Value contains object
                                    sbValue.Clear();
                                    valueState = eValueState.Object;
                                    //Calling new object parser from this place
                                    op = new ObjectParser(this._json);
                                    i = op.Start(i);
                                    MSB.Append(op.MSB.ToString());
                                    //System.Diagnostics.Debug.WriteLine("Collected " + op.MSB.ToString());

                                    continue;
                                }
                                else if (valueState == eValueState.Object)
                                {
                                    if (c == '\'')
                                    {
                                        //ObjectValue ends here
                                        valueState = eValueState.Unknown;
                                        state = eState.BEGIN;
                                        kvp.Add(sbKey.ToString(), op.MSB.ToString());
                                        //System.Diagnostics.Debug.WriteLine("obj K:{0};V:{1}", sbKey.ToString(), op.MSB.ToString());
                                        sbKey.Clear();
                                        sbValue.Clear();
                                        MSB.Append(c);
                                    }
                                }
                                else
                                {
                                    if (c == '\'')
                                    {
                                        //StringValue ends here
                                        valueState = eValueState.Unknown;
                                        state = eState.BEGIN;
                                        kvp.Add(sbKey.ToString(), sbValue.ToString());
                                        //System.Diagnostics.Debug.WriteLine("K:{0};V:{1}", sbKey.ToString(), sbValue.ToString());
                                        sbKey.Clear();
                                        sbValue.Clear();
                                        MSB.Append(c);
                                    }
                                    else if (c == '\\')
                                    {
                                        useNextSymbolAsChar = true;

                                        if (valueState == eValueState.Unknown)
                                            valueState = eValueState.String;

                                        MSB.Append(c);
                                    }
                                    else
                                    {
                                        sbValue.Append(c);
                                        MSB.Append(c);
                                    }
                                }

                                continue;
                        }
                    }


                    return this._json.Length;

                }
                catch (Exception ex)
                {
                    throw ex;
                }


            }//Eo Start

        }//eoc



        public static Dictionary<string, string> DeserializeDictionaryInternal(string json)
        {
            ObjectParser op = new ObjectParser(json);
            op.Start(0);
            //System.Diagnostics.Debug.WriteLine("Bingo: " + op.MSB.ToString());
            return op.kvp;

        }



    }
}
