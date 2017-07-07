using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class GetPersonalOrderRequest : ExchangeRequestBase
	{
		Guid orderID;

		public GetPersonalOrderRequest(Guid orderID) : base("GET")
		{
			this.orderID = orderID;
			var urlFormat = String.Format("/orders/{0}", orderID);
			this.RequestUrl = urlFormat;
		}
	}
}
