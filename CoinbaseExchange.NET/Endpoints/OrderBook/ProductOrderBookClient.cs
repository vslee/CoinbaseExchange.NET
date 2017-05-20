using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
	public class ProductOrderBookClient : ExchangeClientBase
	{
		public ProductOrderBookClient(CBAuthenticationContainer authenticationContainer)
			: base(authenticationContainer)
		{

		}

        public async Task<GetProductOrderBookResponse> GetProductOrderBook(string productId, int level = 1)
        {
            var request = new GetProductOrderBookRequest(productId, level);
            var response = await this.GetResponse(request);
            var orderBookResponse = new GetProductOrderBookResponse(response);
            return orderBookResponse;
        }
	}
}
