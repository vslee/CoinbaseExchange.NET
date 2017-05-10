using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.Products
{
	public class GetProductsResponse : ExchangeResponseBase
	{
		public IEnumerable<Products> Products { get; private set; }

		public GetProductsResponse(ExchangeResponse response) : base(response)
        {
			var json = response.ContentBody;
			var jArray = JArray.Parse(json);
			Products = jArray.Select(elem => new Products(elem)).ToList();
		}
	}
}
