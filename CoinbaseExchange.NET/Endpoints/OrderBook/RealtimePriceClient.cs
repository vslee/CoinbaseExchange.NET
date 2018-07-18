using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
	public class RealtimePriceClient : IDisposable
	{
		private readonly string ProductString;
		bool robsCreatedLocally = false;
		public RealtimeOrderBookSubscription RealtimeOrderBookSubscription { get; private set; }

		public event EventHandler<decimal> PriceChanged;
		public decimal Price { get; set; } = -1;

		public RealtimePriceClient(string ProductString, CBAuthenticationContainer auth = null)
			: this(ProductString: ProductString, auth: auth,
				  realtimeOrderBookSubscription: new RealtimeOrderBookSubscription(new string[] { ProductString }, auth, GDAX_Channel.matches))
		{
			robsCreatedLocally = true;
			// subscribe if robsCreatedLocally
			this.RealtimeOrderBookSubscription.SubscribeAsync(// don't await bc it won't complete until subscription ends
									reConnectOnDisconnect: true);
		}

		public RealtimePriceClient(string ProductString, RealtimeOrderBookSubscription realtimeOrderBookSubscription,
										CBAuthenticationContainer auth = null)
		{
			this.ProductString = ProductString;

			this.RealtimeOrderBookSubscription = realtimeOrderBookSubscription;
			this.RealtimeOrderBookSubscription.RealtimeMatch += OnMatch;
			// should already be subscribed since robs is passed in
		}

			private void OnMatch(object sender, RealtimeMatch e)
		{
			this.Price = e.Price.Value;
			PriceChanged?.Invoke(this, e.Price.Value);
		}

		public void Dispose()
		{
			this.RealtimeOrderBookSubscription.RealtimeMatch -= OnMatch;
			if (robsCreatedLocally)
			{
				RealtimeOrderBookSubscription.Dispose();
			}
		}
	}
}
