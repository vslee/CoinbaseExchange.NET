using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.Products
{
	public class GetProductsRequest : ExchangeRequestBase
	{
		public GetProductsRequest()
			: base("GET")
		{
			var urlFormat = String.Format("/products");
			this.RequestUrl = urlFormat;
		}
	}
}
