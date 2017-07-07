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
		public string productName;

		public GetPersonalOrdersRequest(string[] Status = null, string productName = null, string cursor = null) //Int16 cursor = 0)
            : base("GET", cursor)
        {
			this.Status = Status;
			this.productName = productName;
			var urlFormat = String.Format("/orders");
			this.RequestUrl = urlFormat;
		}
	}
}
