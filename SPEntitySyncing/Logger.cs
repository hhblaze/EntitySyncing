using System;
using System.Collections.Generic;
using System.Text;

namespace EntitySyncing
{
    public interface ILogger
    {
        void LogException(string className, string methodName, Exception ex, string description);
    }


    internal static class Logger
    {
        public static ILogger log=null;


        internal static void LogException(string className, string methodName, Exception ex, string description)
        {
            if (log == null)
            {
                throw new Exception($"Exception at class: {className}, method: {methodName}, Description: {description}, exception: {ex?.ToString()}" );                
            }

            log.LogException(className, methodName, ex, description);
        }
    }
}
