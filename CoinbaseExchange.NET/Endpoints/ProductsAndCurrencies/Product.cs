using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.Products
{
	public class Product
	{
		// https://api.gdax.com/products
		// [{"id":"BTC-GBP","base_currency":"BTC","quote_currency":"GBP","base_min_size":"0.01","base_max_size":"10000","quote_increment":"0.01","display_name":"BTC/GBP"},{"id":"BTC-USD","base_currency":"BTC","quote_currency":"USD","base_min_size":"0.01","base_max_size":"10000","quote_increment":"0.01","display_name":"BTC/USD"},{"id":"ETH-USD","base_currency":"ETH","quote_currency":"USD","base_min_size":"0.01","base_max_size":"1000000","quote_increment":"0.01","display_name":"ETH/USD"},{"id":"LTC-USD","base_currency":"LTC","quote_currency":"USD","base_min_size":"0.01","base_max_size":"1000000","quote_increment":"0.01","display_name":"LTC/USD"},{"id":"ETH-EUR","base_currency":"ETH","quote_currency":"EUR","base_min_size":"0.01","base_max_size":"1000000","quote_increment":"0.01","display_name":"ETH/EUR"},{"id":"LTC-EUR","base_currency":"LTC","quote_currency":"EUR","base_min_size":"0.01","base_max_size":"1000000","quote_increment":"0.01","display_name":"LTC/EUR"},{"id":"BTC-EUR","base_currency":"BTC","quote_currency":"EUR","base_min_size":"0.01","base_max_size":"10000","quote_increment":"0.01","display_name":"BTC/EUR"},{"id":"ETH-BTC","base_currency":"ETH","quote_currency":"BTC","base_min_size":"0.01","base_max_size":"1000000","quote_increment":"0.00001","display_name":"ETH/BTC"},{"id":"LTC-BTC","base_currency":"LTC","quote_currency":"BTC","base_min_size":"0.01","base_max_size":"1000000","quote_increment":"0.00001","display_name":"LTC/BTC"}]
		public string Name { get; set; }
		public string BaseCurrencyString { get; set; }
		public string QuoteCurrencyString { get; set; }
		/// <summary>
		/// min and max order size
		/// </summary>
		public decimal BaseMinSize { get; set; }
		/// <summary>
		/// min and max order size
		/// </summary>
		public decimal BaseMaxSize { get; set; }
		/// <summary>
		///  min order price as well as the price increment (but not size increment)
		/// </summary>
		public decimal QuoteIncrement { get; set; }

		public Product(JToken jToken) : this()
		{
			this.Name = jToken["id"].Value<string>();
			this.BaseCurrencyString = jToken["base_currency"].Value<string>();
			this.QuoteCurrencyString = jToken["quote_currency"].Value<string>();
			this.BaseMinSize = jToken["base_min_size"].Value<Decimal>();
			this.BaseMaxSize = jToken["base_max_size"].Value<Decimal>();
			this.QuoteIncrement = jToken["quote_increment"].Value<Decimal>();
		}

		public Product(Product anotherProductObj) : this()
		{
			this.Name = anotherProductObj.Name;
			this.BaseCurrencyString = anotherProductObj.BaseCurrencyString;
			this.QuoteCurrencyString = anotherProductObj.QuoteCurrencyString;
			this.BaseMinSize = anotherProductObj.BaseMinSize;
			this.BaseMaxSize = anotherProductObj.BaseMaxSize;
			this.QuoteIncrement = anotherProductObj.QuoteIncrement;
		}

		public Product() {	}
	}
}
