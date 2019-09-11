using System;
using System.Net;

namespace TagBites.IO.Http
{
    internal class TimeoutWebClient : WebClient
    {
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public TimeoutWebClient()
        {
            Timeout = 10000;
        }
        public TimeoutWebClient(int timeout)
        {
            Timeout = timeout;
        }


        protected override WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);
            result.Timeout = Timeout;
            return result;
        }
    }
}
