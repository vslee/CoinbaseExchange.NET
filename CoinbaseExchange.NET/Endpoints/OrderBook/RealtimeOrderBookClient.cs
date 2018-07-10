using CoinbaseExchange.NET.Core;
using CoinbaseExchange.NET.Endpoints.PersonalOrders;
using Swordfish.NET.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSLee.Utils;
using VSLee.Utils.ExchangeBase;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
    public class RealtimeOrderBookClient : IDisposable, INotifyPropertyChanged, IOrderBook<decimal>
	{
		private readonly string ProductString;

        private readonly object _sellLock = new object();
        private readonly object _buyLock = new object();
		private readonly Dictionary<Guid, BidAskOrder> _ordersByID = new Dictionary<Guid, BidAskOrder>();
		Int64 currentSequence = -1;
		/// <summary>
		/// whether RealtimeOrderBookSubscription was created here (true) or passed in the constructor (false)
		/// </summary>
		bool robsCreatedLocally = false;
		public RealtimeOrderBookSubscription RealtimeOrderBookSubscription { get; private set; }
		readonly ProductOrderBookClient productOrderBookClient;

		public event PropertyChangedEventHandler PropertyChanged;

		public ConcurrentObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>> Sells { get; set; }
        public ConcurrentObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>> Buys { get; set; }

        public decimal Spread => BestSell - BestBuy;

		public decimal Midpoint => (BestSell + BestBuy) / 2;

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
			: this(ProductString: ProductString, auth: auth,
				  realtimeOrderBookSubscription: new RealtimeOrderBookSubscription(new string[] { ProductString }, auth))
		{
			robsCreatedLocally = true;
		}

		public RealtimeOrderBookClient(string ProductString, RealtimeOrderBookSubscription realtimeOrderBookSubscription,
										CBAuthenticationContainer auth = null)
        {
			this.ProductString = ProductString;
			this.productOrderBookClient = new ProductOrderBookClient(auth);

			Sells = new ConcurrentObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>(isMultithreaded: true, comparer: new DescendingComparer<decimal>());
            Buys = new ConcurrentObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>(isMultithreaded: true, comparer: new DescendingComparer<decimal>());

			this.RealtimeOrderBookSubscription = realtimeOrderBookSubscription;
			this.RealtimeOrderBookSubscription.RealtimeOpen  += OnOpen;
			this.RealtimeOrderBookSubscription.RealtimeDone += OnDone;
			this.RealtimeOrderBookSubscription.RealtimeMatch += OnMatch;
			this.RealtimeOrderBookSubscription.RealtimeChange += OnChange;
			
			// not really used for anything except for their sequence number
			this.RealtimeOrderBookSubscription.RealtimeReceived += OnReceived;
			this.RealtimeOrderBookSubscription.RealtimeLastMatch += OnLastMatch;
			this.RealtimeOrderBookSubscription.Heartbeat += OnHeartbeat;

			this.RealtimeOrderBookSubscription.ConnectionClosed += OnConnectionClosed;
        }

		private async void OnConnectionClosed(string product, GDAX_Channel channel)
		{
			//resetInProgress = false;
			await ResetStateWithFullOrderBookAsync();
		}

		class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
		{
			public int Compare(T x, T y)
			{
				return y.CompareTo(x);
			}
		}

		private bool resetInProgress = false;
		public async Task ResetStateWithFullOrderBookAsync()
		{
			if (resetInProgress) return;
			resetInProgress = true;
			currentSequence = -1;
			if (robsCreatedLocally)
				this.RealtimeOrderBookSubscription.SubscribeAsync(// don't await bc it won't complete until subscription ends
										reConnectOnDisconnect: true); // else should already be subscribed
			//using (var blocker = new SemaphoreSlim(0))
			//{ // https://stackoverflow.com/questions/23442543/using-async-await-with-dispatcher-begininvoke
				await System.Windows.Application.Current.Dispatcher.Invoke<Task>(
						async () =>
						{ // change to UI thread to use UI dbContext
							var response = await productOrderBookClient.GetProductOrderBook(ProductString, 3);
							Sells.Clear();
							foreach (var order in response.Sells)
							{ // no need to lock here bc lock is in AddOrder()
								AddOrder(order, Side.Sell);
							}
							Buys.Clear();
							foreach (var order in response.Buys)
							{
								AddOrder(order, Side.Buy);
							}
							this.currentSequence = response.Sequence;
							//blocker.Release();
						});
			//	await blocker.WaitAsync();
			//}
			resetInProgress = false;
		}

		private async Task<bool> checkSequenceNumber(long sequence)
		{
			if (this.currentSequence == -1)
			{
				await ResetStateWithFullOrderBookAsync();
			}

			if (sequence <= this.currentSequence)
				// ignore older messages (e.g. before order book initialization from getProductOrderBook)
				return false; // skip this msg
			else if (sequence > this.currentSequence + 1)
			{
				// out of order (missed one)
				await ResetStateWithFullOrderBookAsync();
				return false; // skip this msg
			}
			else // sequence == this.currentSequence + 1
			{
				this.currentSequence = sequence;
				return true; // process this msg
			}
		}

		private Tuple<object, ConcurrentObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>> GetOrderList(Side side)
		{
			if (side == Side.Buy)
				return new Tuple<object, ConcurrentObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>>(
					_buyLock, Buys);
			else return new Tuple<object, ConcurrentObservableSortedDictionary<decimal, ObservableLinkedList<BidAskOrder>>>(
					_sellLock, Sells);
		}

		private void AddOrder(BidAskOrder order, Side side)
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

		private void RemoveOrder(Guid orderID, Side side)
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

		private async void OnOpen(object sender, RealtimeOpen open)
		{
			if (await checkSequenceNumber(open.Sequence))
				System.Windows.Application.Current.Dispatcher.Invoke(
				  System.Windows.Threading.DispatcherPriority.Background,
				  new Action(() =>
				  { // change to UI thread
					  var order = new BidAskOrder();
					  order.Id = open.OrderId;
					  order.Price = open.Price.Value;
					  order.Size = open.RemainingSize;
					  AddOrder(order: order, side: open.Side);
				  }));
		}

		private async void OnDone(object sender, RealtimeDone done)
		{
			if (await checkSequenceNumber(done.Sequence))
				System.Windows.Application.Current.Dispatcher.Invoke(
				  System.Windows.Threading.DispatcherPriority.Background,
				  new Action(() =>
				  { // change to UI thread
					  RemoveOrder(orderID: done.OrderId, side: done.Side);
				  }));
		}

		private async void OnMatch(object sender, RealtimeMatch match)
		{
			if (await checkSequenceNumber(match.Sequence))
				System.Windows.Application.Current.Dispatcher.Invoke(
				  System.Windows.Threading.DispatcherPriority.Background,
				  new Action(() =>
				  { // change to UI thread
					  var list = GetOrderList(side: match.Side);
					  list.Item2.TryGetValue(match.Price.Value, out var linkedList);
					  if (linkedList == null)
						  return; // + handle when order is not in book (say program missed some orders being added)
					  var order = linkedList.Where(o => o.Id == match.MakerOrderId).SingleOrDefault();
					  if (order != null)
					  {
						  order.Size -= match.Size;
						  if (order.Size == 0)
							  RemoveOrder(match.MakerOrderId, side: match.Side);
						  if (order.Size < 0)
							  RemoveOrder(match.MakerOrderId, side: match.Side);
					  }
				  }));
		}

		private async void OnChange(object sender, RealtimeChange change)
		{
			if (await checkSequenceNumber(change.Sequence) && change.NewSize != null)
				System.Windows.Application.Current.Dispatcher.Invoke(
				  System.Windows.Threading.DispatcherPriority.Background,
				  new Action(() =>
				  { // change to UI thread
					  var list = GetOrderList(side: change.Side);
					  lock (list.Item1)
					  {
						  _ordersByID.TryGetValue(change.OrderId, out var order);
						  if (order == null)
							  return; // + handle when order is not in book (say program missed some orders being added)
						  var linkedlist = list.Item2[change.Price.Value];
						  order.Size = change.NewSize.Value;
						  // newSize should never be zero so no need to remove
					  }
				  }));
		}

		private async void OnReceived(object sender, RealtimeReceived received)
		{
			await checkSequenceNumber(received.Sequence);
		}

		private async void OnLastMatch(object sender, RealtimeMatch lastMatch)
		{
			await checkSequenceNumber(lastMatch.Sequence);
		}

		private async void OnHeartbeat(object sender, Heartbeat heartbeat)
		{
			await checkSequenceNumber(heartbeat.Sequence);
		}

		protected virtual void NotifyPropertyChanged(String propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
			this.RealtimeOrderBookSubscription.RealtimeOpen -= OnOpen;
			this.RealtimeOrderBookSubscription.RealtimeDone -= OnDone;
			this.RealtimeOrderBookSubscription.RealtimeMatch -= OnMatch;
			this.RealtimeOrderBookSubscription.RealtimeChange -= OnChange;
			this.RealtimeOrderBookSubscription.RealtimeReceived -= OnReceived;
			this.RealtimeOrderBookSubscription.RealtimeLastMatch -= OnLastMatch;
			this.RealtimeOrderBookSubscription.Heartbeat -= OnHeartbeat;
			this.RealtimeOrderBookSubscription.ConnectionClosed -= OnConnectionClosed;

			if (robsCreatedLocally)
			{
				RealtimeOrderBookSubscription.Dispose();
			}
		}
	}
}
