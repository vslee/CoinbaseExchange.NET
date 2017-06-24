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
		public IList<PersonalOrder> PersonalOrders { get; private set; }

		public GetPersonalOrdersResponse(ExchangeResponse response) : base(response)
        {
			var json = response.ContentBody;
			var token = JToken.Parse(json);
			if (token is JArray)
			{
				PersonalOrders = token.Select(elem => new PersonalOrder(elem)).ToList();
			}
			else if (token is JObject)
			{
				this.Message = "GetPersonalOrdersResponse: " + token["message"].Value<string>();
			}
		}
	}
}
