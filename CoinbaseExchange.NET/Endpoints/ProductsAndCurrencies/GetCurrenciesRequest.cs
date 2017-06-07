using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.ProductsAndCurrencies
{
	public class GetCurrenciesRequest : ExchangeRequestBase
	{
		public GetCurrenciesRequest()
			: base("GET")
		{
			var urlFormat = String.Format("/currencies");
			this.RequestUrl = urlFormat;
		}
	}
}
