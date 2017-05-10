using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.Products
{
	public class Products
	{
		public string Id { get; set; }
		public string BaseCurrency { get; set; }
		public string QuoteCurrency { get; set; }
		public decimal BaseMinSize { get; set; }
		public decimal BaseMaxSize { get; set; }
		public string QuoteIncrement { get; set; }

		public Products(JToken jToken)
		{
			this.Id = jToken["id"].Value<string>();
			this.BaseCurrency = jToken["base_currency"].Value<string>();
			this.QuoteCurrency = jToken["quote_currency"].Value<string>();
			this.BaseMinSize = jToken["base_min_size"].Value<Decimal>();
			this.BaseMaxSize = jToken["base_max_size"].Value<Decimal>();
			this.QuoteIncrement = jToken["quote_increment"].Value<string>();
		}
	}
}
