using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class CancelAllPersonalOrdersResponse : ExchangeResponseBase
	{
		public List<Guid> DeletedOrderIDs;

		public CancelAllPersonalOrdersResponse(ExchangeResponse response) : base(response)
        {
			// + use response.IsSuccessStatusCode;
			var json = response.ContentBody;
			var token = JToken.Parse(json);
			if (token is JArray)
			{
				DeletedOrderIDs = token.Select(elem => (Guid)elem).ToList();
			}
			else if (token is JObject)
			{
				this.Message = "CancelAllPersonalOrdersResponse: " + token["message"].Value<string>();
			}
		}
	}
}
