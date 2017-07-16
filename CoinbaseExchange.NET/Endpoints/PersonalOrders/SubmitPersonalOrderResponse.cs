using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class SubmitPersonalOrderResponse : ExchangePageableResponseBase
	{
		public PersonalOrder SubmittedOrder { get; private set; }

		public SubmitPersonalOrderResponse(ExchangeResponse response, SubmitPersonalOrderRequest request) : base(response)
        {
			if (response.ErrorMessage != null)
			{
				Message = response.ErrorMessage;
			}
			else
			{
				var json = response.ContentBody;
				try
				{
					var jToken = JToken.Parse(json);
					//{"message":"Insufficient funds"}
					var msgToken = jToken["message"];
					if (msgToken != null)
						Message = "SubmitPersonalOrderResponse: " + msgToken.Value<string>() + ", request: " + request.RequestUrl;
					else
						SubmittedOrder = new PersonalOrder(jToken);
				}
				catch (Newtonsoft.Json.JsonReaderException e)
				{
					Message = "Error w/ SubmitPersonalOrderResponse() " + e.Message + ": " + json;
				}
			}
		}
	}
}
