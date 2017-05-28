using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class CancelAllPersonalOrdersRequest : ExchangeRequestBase
	{
		public string product_id;

		public CancelAllPersonalOrdersRequest(string product_id = null) : base("DELETE")
		{
			this.product_id = product_id;
			this.RequestUrl = String.Format("/orders");
		}
	}
}
