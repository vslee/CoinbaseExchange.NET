using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class CancelPersonalOrderRequest : ExchangeRequestBase
	{
		public CancelPersonalOrderRequest(Guid orderID) : base("DELETE")
		{
			this.RequestUrl = String.Format("/orders/{0}", orderID);
		}
	}
}
