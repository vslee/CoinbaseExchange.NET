using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSLee.Utils;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
    public class RealtimeOrderBookClient : IDisposable, INotifyPropertyChanged
	{
		private readonly string ProductString;

        private object _sellLock = new object();
        private object _buyLock = new object();
		private readonly Dictionary<Guid, BidAskOrder> _ordersByID = new Dictionary<Guid, BidAskOrder>();
		public RealtimeOrderBookSubscription RealtimeOrderBookSubscription { get; private set; }
		ProductOrderBookClient productOrderBookClient;

		public event PropertyChangedEventHandler PropertyChanged;

		public ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>> Sells { get; set; }
        public ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>> Buys { get; set; }

        public decimal Spread => BestSell - BestBuy;

		public decimal BestBuy
		{
			get
			{
				lock (_buyLock)
				{
					if (Buys.Count == 0)
						return -1;
					else
						// + can be optimized
						return Buys.Keys.First();
				}
			}
		}

		public decimal BestSell
		{
			get
			{
				lock (_sellLock)
				{
					if (Sells.Count == 0)
						return -1;
					else
						// + can be optimized
						return Sells.Keys.Last();
				}
			}
		}

		public RealtimeOrderBookClient(string ProductString, CBAuthenticationContainer auth = null)
        {
			this.ProductString = ProductString;
			this.productOrderBookClient = new ProductOrderBookClient(auth);

			Sells = new ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>(new DescendingComparer<decimal>());
            Buys = new ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>(new DescendingComparer<decimal>());

			this.RealtimeOrderBookSubscription = new RealtimeOrderBookSubscription(ProductString, auth);
			this.RealtimeOrderBookSubscription.RealtimeOpen  += OnOpen;
			this.RealtimeOrderBookSubscription.RealtimeDone += OnDone;
			this.RealtimeOrderBookSubscription.RealtimeMatch += OnMatch;
			this.RealtimeOrderBookSubscription.RealtimeChange += OnChange;
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
			foreach (var order in response.Sells)
			{ // no need to lock here bc lock is in AddOrder()
				AddOrder(order, "sell");
			}
			foreach (var order in response.Buys)
			{
				AddOrder(order, "buy");
			}

			var subTask = this.RealtimeOrderBookSubscription.SubscribeAsync(); // don't await bc it won't complete until subscription ends
        }

		private Tuple<object, ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>> GetOrderList(string side)
		{
			if (side == "buy")
				return new Tuple<object, ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>>(
					_buyLock, Buys);
			else return new Tuple<object, ObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>>(
					_sellLock, Sells);
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
			NotifyPropertyChanged("BestBuy");
			NotifyPropertyChanged("BestSell");
			NotifyPropertyChanged("Spread");
		}

		private void RemoveOrder(Guid orderID, string side)
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
			NotifyPropertyChanged("BestBuy");
			NotifyPropertyChanged("BestSell");
			NotifyPropertyChanged("Spread");
		}

		private void OnOpen(object sender, RealtimeOpen open)
		{
			var order = new BidAskOrder();
			order.Id = open.OrderId;
			order.Price = open.Price.Value;
			order.Size = open.RemainingSize;
			AddOrder(order: order, side: open.Side);
		}

		private void OnDone(object sender, RealtimeDone done)
		{
			RemoveOrder(orderID: done.OrderId, side: done.Side);
		}

		private void OnMatch(object sender, RealtimeMatch match)
		{
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

		private void OnChange(object sender, RealtimeChange change)
		{
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

		protected virtual void NotifyPropertyChanged(String propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
			RealtimeOrderBookSubscription.UnSubscribe();
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
