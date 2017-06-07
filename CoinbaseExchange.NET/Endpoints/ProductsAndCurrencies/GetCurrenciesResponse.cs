using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.ProductsAndCurrencies
{
	public class GetCurrenciesResponse : ExchangeResponseBase
	{
		public IEnumerable<GDAX_Currency> Currencies { get; private set; }

		public GetCurrenciesResponse(ExchangeResponse response) : base(response)
        {
			var json = response.ContentBody;
			var jArray = JArray.Parse(json);
			Currencies = jArray.Select(elem => new GDAX_Currency(elem)).ToList();
		}
	}
}
