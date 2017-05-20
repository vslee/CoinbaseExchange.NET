using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinbaseExchange.NET.Utilities;

namespace CoinbaseExchange.NET.Core
{
    public abstract class ExchangeRequestBase
    {
        public string Method { get; private set; }
		/// <summary>
		/// relative URL
		/// </summary>
        public string RequestUrl { get; protected set; }
        public string RequestBody { get; protected set; }

   //     public bool IsExpired
   //     {
			//// Your timestamp must be within 30 seconds of the api service time or your request will be considered expired and rejected. We recommend using the time endpoint to query for the API server time if you believe there many be time skew between your server and the API servers.
			////get { return (GetCurrentUnixTimeStamp() - TimeStamp) >= 30; } 
   //     }

        protected ExchangeRequestBase(string method)
        {
            this.Method = method;
        }
    }
}
