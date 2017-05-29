using CoinbaseExchange.NET.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class PersonalOrderParams
	{
		// client_oid[optional] Order ID selected by you to identify your order
		[JsonProperty("client_oid")]
		public Guid? ClientOrderId { get; set; }
		// type[optional] limit, market, or stop(default is limit)
		[JsonProperty("type")]
		public string Type { get; set; }
		// side buy or sell
		[JsonProperty("side")]
		public Side Side { get; set; }
		// product_id A valid product id
		[JsonProperty("product_id")]
		public string ProductId { get; set; }
		// stp[optional] Self-trade prevention flag
		// price   Price per bitcoin
		[JsonProperty("price")]
		public decimal? Price { get; set; }
		// size    Amount of BTC to buy or sell (either size or funds required)
		[JsonProperty("size")]
		public decimal? Size { get; set; }
		// funds	[optional]* Desired amount of quote currency to use
		[JsonProperty("funds")]
		public decimal? Funds { get; set; }
		// time_in_force[optional] GTC, GTT, IOC, or FOK(default is GTC)
		[JsonProperty("time_in_force")]
		public string TimeInForce { get; set; }
		// cancel_after[optional]* min, hour, day
		// overdraft_enabled	* If true funding will be provided if the order’s cost cannot be covered by the account’s balance
		// funding_amount* Amount of funding to be provided for the order
		[JsonProperty("post_only")]
		public bool? PostOnly { get; set; }
	}

	public class SubmitPersonalOrderRequest : ExchangeRequestBase
	{
		public SubmitPersonalOrderRequest(PersonalOrderParams orderParams) : base("POST")
		{
			var urlFormat = String.Format("/orders");
			this.RequestUrl = urlFormat;
			this.RequestBody = JsonConvert.SerializeObject(orderParams,
					Newtonsoft.Json.Formatting.None,
					new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore,
						Converters = new List<JsonConverter> { new StringEnumConverter { CamelCaseText = true } },
						ContractResolver = new LowercaseContractResolver()
					});
		}
	}

	public class LowercaseContractResolver : DefaultContractResolver
	{
		protected override string ResolvePropertyName(string propertyName)
		{
			return propertyName.ToLower();
		} 
	}
}
