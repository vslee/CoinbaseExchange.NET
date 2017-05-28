using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class CancelPersonalOrderResponse : ExchangeResponseBase
	{
		public Guid DeletedOrderID;
		public bool deleteSuccess = false;

		public CancelPersonalOrderResponse(ExchangeResponse response) : base(response)
        {
			// + use response.IsSuccessStatusCode;
			var json = response.ContentBody;
			var token = JToken.Parse(json);
			if (token is JArray)
			{
				//var deletedOrders = token.ToObject<List<string>>();
				DeletedOrderID = token.Select(elem => (Guid)elem).Single();
				deleteSuccess = true;
			}
			else if (token is JObject)
			{
				this.Message = token["message"].Value<string>();
			}
		}
	}
}
