using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSLee.Utils;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
    public class RealtimeOrderBookClient
    {
		private readonly string ProductString;

        private object _askLock = new object();
        private object _bidLock = new object();
		private readonly Dictionary<string, BidAskOrder> _ordersByID = new Dictionary<string, BidAskOrder>();
		RealtimeOrderBookSubscription realtimeOrderBookSubscription;
		ProductOrderBookClient productOrderBookClient;

        public ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>> Sells { get; set; }
        public ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>> Buys { get; set; }

        public event EventHandler Updated;

        public decimal Spread
        {
            get
            {
				lock (_askLock)
				{
					lock (_bidLock)
					{
						if (!Buys.Any() || !Sells.Any())
							return 0;

						// + can be optimized
						var maxBuy = Buys.Keys.First();
						var minSell = Sells.Keys.Last();

						return minSell - maxBuy;
					}
				}
            }
        }

        public RealtimeOrderBookClient(string ProductString, CBAuthenticationContainer auth = null)
        {
			this.ProductString = ProductString;
			this.productOrderBookClient = new ProductOrderBookClient(auth);

			Sells = new ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>(new DescendingComparer<decimal>());
            Buys = new ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>(new DescendingComparer<decimal>());

			this.realtimeOrderBookSubscription = new RealtimeOrderBookSubscription(ProductString, auth);
			this.realtimeOrderBookSubscription.RealtimeOpen  += OnOpen;
			this.realtimeOrderBookSubscription.RealtimeDone += OnDone;
			this.realtimeOrderBookSubscription.RealtimeMatch += OnMatch;
			this.realtimeOrderBookSubscription.RealtimeChange += OnChange;
			ResetStateWithFullOrderBook();
        }

		class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
		{
			public int Compare(T x, T y)
			{
				return y.CompareTo(x);
			}
		}

		private async void ResetStateWithFullOrderBook()
        {
            var response = await productOrderBookClient.GetProductOrderBook(ProductString, 3);

            lock (_askLock)
            {
				foreach (var order in response.Sells)
				{
					AddOrder(order, "sell");
				}
			}
			lock (_bidLock)
            {
				foreach (var order in response.Buys)
				{
					AddOrder(order, "buy");
				}
            }

            OnUpdated();

			this.realtimeOrderBookSubscription.Subscribe();
        }

		private Tuple<object, ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>> GetOrderList(string side)
		{
			if (side == "buy")
				return new Tuple<object, ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>>(
					_bidLock, Buys);
			else return new Tuple<object, ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>>(
					_askLock, Sells);
			;
		}

		private void AddOrder(BidAskOrder order, string side)
		{
			var list = GetOrderList(side);
			lock (list.Item1)
			{
				list.Item2.TryGetValue(order.Price, out var linkedlist);
				if (linkedlist == null)
				{
					linkedlist = new ObservableLinkedList<BidAskOrder>();
					list.Item2.Add(order.Price, linkedlist);
				}

				linkedlist.AddLast(order);
				_ordersByID[order.Id] = order;
			}
		}

		private void RemoveOrder(string orderID, string side)
		{
			var list = GetOrderList(side);
			lock (list.Item1)
			{
				_ordersByID.TryGetValue(orderID, out var order);
				if (order == null)
					return; // handle when order is not in book (say program missed some orders being added)
				var linkedlist = list.Item2[order.Price];
				linkedlist.Remove(order);
				if (linkedlist.Count == 0)
					list.Item2.Remove(order.Price);
				_ordersByID.Remove(order.Id);
			}
		}

		private void OnUpdated()
        {
            if (Updated != null)
                Updated(this, new EventArgs());
        }

		private void OnOpen(RealtimeOpen open)
		{
			var order = new BidAskOrder();
			order.Id = open.OrderId;
			order.Price = open.Price.Value;
			order.Size = open.RemainingSize;
			AddOrder(order: order, side: open.Side);
		}

		private void OnDone(RealtimeDone done)
		{
			RemoveOrder(orderID: done.OrderId, side: done.Side);
		}

		private void OnMatch(RealtimeMatch match)
		{ // + handle when order is not in book (say program missed some orders being added)
			var side = match.Side;
			var list = GetOrderList(side: side);
			decimal newPrice;
			lock (list.Item1)
			{
				list.Item2.TryGetValue(match.Price.Value, out var linkedList);
				if (linkedList == null)
					return; // handle when order is not in book (say program missed some orders being added)
				var order = linkedList.First.Value; // first order in queue gets matched
				order.Size -= match.Size;
				newPrice = order.Size;
			}
			if (newPrice == 0)
				RemoveOrder(match.MakerOrderId, side: side); // keep outside of lock to avoid deadlock
		}

		private void OnChange(RealtimeChange change)
		{ // + handle when order is not in book (say program missed some orders being added)
			var list = GetOrderList(side: change.Side);
			lock (list.Item1)
			{
				_ordersByID.TryGetValue(change.OrderId, out var order);
				if (order == null)
					return; // handle when order is not in book (say program missed some orders being added)
				var linkedlist = list.Item2[change.Price.Value];
				order.Size = change.NewSize;
				// newSize should never be zero so no need to remove
			}
		}

		//private void OnReceived(RealtimeReceived receivedMessage)
  //      {
  //          var order = new BidAskOrder();

		//	if (receivedMessage.Price != null) // no "price" token in market orders
		//	{
		//		order.Id = receivedMessage.OrderId;
		//		order.Price = receivedMessage.Price.Value;
		//		order.Size = receivedMessage.Size;

		//		if (receivedMessage.Side == "buy")
		//		{
		//			lock (_bidLock)
		//			{
		//				Buys.Add(order);
		//				//Buys = _buys.ToList();
		//			}
		//		}
		//		else if (receivedMessage.Side == "sell")
		//		{
		//			lock (_askLock)
		//			{
		//				Sells.Add(order);
		//				//Sells = _sells.ToList();
		//			}
		//		}
		//		OnUpdated();
		//	}
		//}

  //      private void OnDone(RealtimeDone message)
  //      {
		//	if (message.Side == "buy")
		//	{
		//		lock (_bidLock)
		//		{
		//			Buys.RemoveAll(b => b.Id == message.OrderId);
		//			//Buys = _buys.ToList();
		//		}
		//	}
		//	else if (message.Side == "sell")
		//	{
		//		lock (_askLock)
		//		{
		//			Sells.RemoveAll(a => a.Id == message.OrderId);
		//			//Sells = _sells.ToList();
		//		}
		//	}
		//	OnUpdated(); // probably should be outside of lock
		//}
    }
}
