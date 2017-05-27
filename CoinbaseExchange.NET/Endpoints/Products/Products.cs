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
		public string Name { get; set; }
		public string BaseCurrency { get; set; }
		public string QuoteCurrency { get; set; }
		public decimal BaseMinSize { get; set; }
		public decimal BaseMaxSize { get; set; }
		public decimal QuoteIncrement { get; set; }

		public Products(JToken jToken)
		{
			this.Name = jToken["id"].Value<string>();
			this.BaseCurrency = jToken["base_currency"].Value<string>();
			this.QuoteCurrency = jToken["quote_currency"].Value<string>();
			this.BaseMinSize = jToken["base_min_size"].Value<Decimal>();
			this.BaseMaxSize = jToken["base_max_size"].Value<Decimal>();
			this.QuoteIncrement = jToken["quote_increment"].Value<Decimal>();
		}

		public Products(Products anotherProductsObj)
		{
			this.Name = anotherProductsObj.Name;
			this.BaseCurrency = anotherProductsObj.BaseCurrency;
			this.QuoteCurrency = anotherProductsObj.QuoteCurrency;
			this.BaseMinSize = anotherProductsObj.BaseMinSize;
			this.BaseMaxSize = anotherProductsObj.BaseMaxSize;
			this.QuoteIncrement = anotherProductsObj.QuoteIncrement;
		}

		public Products()
		{

		}
	}
}
