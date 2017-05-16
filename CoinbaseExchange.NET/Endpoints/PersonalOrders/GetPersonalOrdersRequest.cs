using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class GetPersonalOrdersRequest : ExchangePageableRequestBase
	{
		public string[] Status;

		public GetPersonalOrdersRequest(string[] Status = null, Int16 cursor = 0)
            : base("GET", cursor: cursor)
        {
			this.Status = Status;
			var urlFormat = String.Format("/orders");
			this.RequestUrl = urlFormat;
		}
	}
}
