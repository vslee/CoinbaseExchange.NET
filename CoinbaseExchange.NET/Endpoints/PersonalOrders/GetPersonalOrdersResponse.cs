using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class GetPersonalOrdersResponse : ExchangePageableResponseBase
	{
		public IEnumerable<PersonalOrder> PersonalOrders { get; private set; }

		public GetPersonalOrdersResponse(ExchangeResponse response) : base(response)
        {
			var json = response.ContentBody;
			var jArray = JArray.Parse(json);
			PersonalOrders = jArray.Select(elem => new PersonalOrder(elem)).ToList();
		}
	}
}
