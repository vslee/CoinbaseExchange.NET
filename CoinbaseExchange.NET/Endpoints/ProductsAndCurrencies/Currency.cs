using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.ProductsAndCurrencies
{
	public class GDAX_Currency
	{
		// [{"id":"BTC","name":"Bitcoin","min_size":"0.00000001"},{"id":"EUR","name":"Euro","min_size":"0.01000000"},{"id":"LTC","name":"Litecoin","min_size":"0.00000001"},{"id":"GBP","name":"British Pound","min_size":"0.01000000"},{"id":"USD","name":"United States Dollar","min_size":"0.01000000"},{"id":"ETH","name":"Ether","min_size":"0.00000001"}]
		public string Abbreviation { get; set; }
		public string Name { get; set; }
		/// <summary>
		/// Size increment (not Min order size - that is specified in Product)
		/// </summary>
		public decimal MinSize { get; set; }

		public GDAX_Currency(JToken jToken) : this()
		{
			this.Abbreviation = jToken["id"].Value<string>();
			this.Name = jToken["name"].Value<string>();
			this.MinSize = jToken["min_size"].Value<Decimal>();
		}

		public GDAX_Currency(GDAX_Currency anotherCurrencyObj) : this()
		{
			this.Abbreviation = anotherCurrencyObj.Abbreviation;
			this.Name = anotherCurrencyObj.Name;
			this.MinSize = anotherCurrencyObj.MinSize;
		}

		public GDAX_Currency() { }
	}
}
