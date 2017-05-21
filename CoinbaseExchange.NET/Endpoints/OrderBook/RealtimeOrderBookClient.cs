using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
    public class RealtimeOrderBookClient
    {
		private readonly string ProductString;

        private object _spreadLock = new object();
        private object _askLock = new object();
        private object _bidLock = new object();
		RealtimeOrderBookSubscription realtimeOrderBookSubscription;
		ProductOrderBookClient productOrderBookClient;

		private List<BidAskOrder> _sells { get; set; }
        private List<BidAskOrder> _buys { get; set; }

        public List<BidAskOrder> Sells { get; set; }
        public List<BidAskOrder> Buys { get; set; }

        public event EventHandler Updated;

        public decimal Spread
        {
            get
            {
                lock (_spreadLock)
                {
                    if (!Buys.Any() || !Sells.Any())
                        return 0;

                    var maxBuy = Buys.Select(x => x.Price).Max();
                    var minSell = Sells.Select(x => x.Price).Min();

                    return minSell - maxBuy;
                }
            }
        }

        public RealtimeOrderBookClient(string ProductString, CBAuthenticationContainer auth = null)
        {
			this.ProductString = ProductString;
			this.productOrderBookClient = new ProductOrderBookClient(auth);
			_sells = new List<BidAskOrder>();
            _buys = new List<BidAskOrder>();

            Sells = new List<BidAskOrder>();
            Buys = new List<BidAskOrder>();

			this.realtimeOrderBookSubscription = new RealtimeOrderBookSubscription(ProductString, auth);
			this.realtimeOrderBookSubscription.RealtimeReceived += OnReceived;
			this.realtimeOrderBookSubscription.RealtimeDone += OnDone;
            ResetStateWithFullOrderBook();
        }

        private async void ResetStateWithFullOrderBook()
        {
            var response = await productOrderBookClient.GetProductOrderBook(ProductString, 3);

            lock (_spreadLock)
            {
                lock (_askLock)
                {
                    lock (_bidLock)
                    {
                        _buys = response.Buys.ToList();
                        _sells = response.Sells.ToList();

                        Buys = _buys.ToList();
                        Sells = _sells.ToList();
                    }
                }
            }

            OnUpdated();

			this.realtimeOrderBookSubscription.Subscribe();
        }

		private void OnUpdated()
        {
            if (Updated != null)
                Updated(this, new EventArgs());
        }
        private void OnReceived(RealtimeReceived receivedMessage)
        {
            var order = new BidAskOrder();

			if (receivedMessage.Price != null) // no "price" token in market orders
			{
				order.Id = receivedMessage.OrderId;
				order.Price = receivedMessage.Price.Value;
				order.Size = receivedMessage.Size;

				lock (_spreadLock)
				{
					if (receivedMessage.Side == "buy")
					{
						lock (_bidLock)
						{
							_buys.Add(order);
							Buys = _buys.ToList();
						}
					}
					else if (receivedMessage.Side == "sell")
					{
						lock (_askLock)
						{
							_sells.Add(order);
							Sells = _sells.ToList();
						}
					}
				}
				OnUpdated();
			}
		}

        private void OnDone(RealtimeDone message)
        {
            lock (_spreadLock)
            {
				if (message.Side == "buy")
				{
					lock (_bidLock)
					{
						_buys.RemoveAll(b => b.Id == message.OrderId);
						Buys = _buys.ToList();
					}
				}
				else if (message.Side == "sell")
				{
					lock (_askLock)
					{
						_sells.RemoveAll(a => a.Id == message.OrderId);
						Sells = _sells.ToList();
					}
				}
			}
			OnUpdated(); // probably should be outside of lock
		}
    }
}
