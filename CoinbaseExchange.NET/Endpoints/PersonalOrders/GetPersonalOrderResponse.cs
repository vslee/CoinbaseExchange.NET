using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class GetPersonalOrderResponse : ExchangePageableResponseBase
	{
		public PersonalOrder FoundOrder { get; private set; }
		public string ContentBody { get; private set; }

		public GetPersonalOrderResponse(ExchangeResponse response) : base(response)
		{
			this.ContentBody = response.ContentBody;
			var jToken = JToken.Parse(ContentBody);

			//{"message":"NotFound"}
			var msgToken = jToken["message"];
			if (msgToken != null)
				Message = "GetPersonalOrderResponse: " + msgToken.Value<string>();
			else
				FoundOrder = new PersonalOrder(jToken);
		}
	}
}
