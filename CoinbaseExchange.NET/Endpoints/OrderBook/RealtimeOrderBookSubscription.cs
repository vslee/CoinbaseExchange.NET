using CoinbaseExchange.NET.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSLee.Utils;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
	public enum GDAX_Channel
	{
		// heartbeat, included by default
		// ticker, not yet implemented
		// level2, not yet implemented
		user,
		matches,
		full,
	}

   public class RealtimeOrderBookSubscription : ExchangeClientBase, IDisposable
    {
        public static readonly Uri WSS_SANDBOX_ENDPOINT_URL = new Uri("wss://ws-feed-public.sandbox.gdax.com");
        public static readonly Uri WSS_ENDPOINT_URL = new Uri("wss://ws-feed.gdax.com");
        private readonly string[] Products;
		/// <summary>
		/// Key: product, Value: channel
		/// </summary>
		private SortedList<string, GDAX_Channel> subscribedProducts = new SortedList<string, GDAX_Channel>();
		private readonly GDAX_Channel gdax_Channel;
		CancellationTokenSource cancellationTokenSource;
		// The websocket feed is publicly available, but connection to it are rate-limited to 1 per 4 seconds per IP.
		/// <summary>
		/// Only for subscribing to Websockets. Polling has its own RateGate
		/// </summary>
		private static readonly RateGate rateGateRealtime = new RateGate(occurrences: 1, timeUnit: new TimeSpan(0, 0, seconds: 4));
		private ClientWebSocket webSocketClient;
		public event EventHandler<RealtimeReceived> RealtimeReceived;
        public event EventHandler<RealtimeOpen> RealtimeOpen;
        public event EventHandler<RealtimeDone> RealtimeDone;
        public event EventHandler<RealtimeMatch> RealtimeMatch;
		public event EventHandler<RealtimeMatch> RealtimeLastMatch;
		public event EventHandler<RealtimeChange> RealtimeChange;
		public event EventHandler<Heartbeat> Heartbeat;
		/// <summary>
		/// Error in underlying websocket stream, need to refresh orderbook
		/// </summary>
		public event EventHandler<RealtimeError> RealtimeStreamError;
        /// <summary>
        /// Token error or cancellation error, no need to restart stream
        /// </summary>
        public event EventHandler<RealtimeMessage> RealtimeDataError;
		/// <summary>
		/// P1: product, P2: channel
		/// </summary>
		public event Action<string, GDAX_Channel> ConnectionOpened;
		/// <summary>
		/// P1: product, P2: channel
		/// </summary>
		public event Action<string, GDAX_Channel> ConnectionClosed;

		public GDAX_Channel? GetCurrentlySubscribedChannel(string product)
		{
			if (subscribedProducts.ContainsKey(product))
				return subscribedProducts[product];
			else return null;
		}

        public RealtimeOrderBookSubscription(string[] Products, CBAuthenticationContainer auth = null,
			GDAX_Channel gdax_Channel = GDAX_Channel.full) : base(auth)
        { // + eventually can take an array of productStrings and subscribe simultaneously 
			this.Products = Products;
			if (Products == null || Products.Length == 0)
				throw new ArgumentNullException("Products");
			this.gdax_Channel = gdax_Channel;
			ConnectionOpened += (product, channel) => subscribedProducts.Add(product, channel);
			ConnectionClosed += (product, channel) => subscribedProducts.Remove(product);
		}

		public async Task<bool> SubscribeAsync(string[] Products, GDAX_Channel gdax_Channel = GDAX_Channel.full)
		{
			var productsToUnsubscribe = new SortedList<GDAX_Channel, List<string>>(); // value is a list of products to unsubscribe from
			var productsToSubscribeFiltered = new List<string>();
			foreach (var product in Products)
			{
				if (subscribedProducts.ContainsKey(product) &&
					subscribedProducts[product] != GDAX_Channel.full && gdax_Channel == GDAX_Channel.full)
				{ // if currently subscribed to a non full channel, then unsubscribe
					var currentlySubscribedChannel = subscribedProducts[product];
					if (!productsToUnsubscribe.ContainsKey(currentlySubscribedChannel))
						productsToUnsubscribe.Add(currentlySubscribedChannel, new List<string>());
					productsToUnsubscribe[currentlySubscribedChannel].Add(product);
					productsToSubscribeFiltered.Add(product); // these need to be subscribed to
				}

				if (!subscribedProducts.ContainsKey(product))
				{ // these also need to be subscribed to since they're not subscribed
					productsToSubscribeFiltered.Add(product);
				}
			}
			foreach (var kvp in productsToUnsubscribe)
			{ // unsubscribe
				await sendSubscriptionMsgAsync(Products: kvp.Value, gdax_Channel: kvp.Key, unSubscribe: true);
			}
			return await sendSubscriptionMsgAsync(Products: Products, gdax_Channel: gdax_Channel, unSubscribe: false);
		}

		public async Task<bool> DowngradeSubscriptionAsync(string[] Products, GDAX_Channel gdax_Channel)
		{
			var productsToUnsubscribe = new SortedList<GDAX_Channel, List<string>>(); // value is a list of products to unsubscribe from
			var productsToSubscribeFiltered = new List<string>();
			foreach (var product in Products)
			{
				if (subscribedProducts.ContainsKey(product) &&
					subscribedProducts[product] == GDAX_Channel.full && gdax_Channel != GDAX_Channel.full)
				{ // if currently subscribed to a full channel, then unsubscribe
					var currentlySubscribedChannel = subscribedProducts[product];
					if (!productsToUnsubscribe.ContainsKey(currentlySubscribedChannel))
						productsToUnsubscribe.Add(currentlySubscribedChannel, new List<string>());
					productsToUnsubscribe[currentlySubscribedChannel].Add(product);
					productsToSubscribeFiltered.Add(product); // these need to be subscribed to
				}

				if (!subscribedProducts.ContainsKey(product))
				{ // these also need to be subscribed to since they're not subscribed
					productsToSubscribeFiltered.Add(product);
				}
			}
			foreach (var kvp in productsToUnsubscribe)
			{ // unsubscribe
				await sendSubscriptionMsgAsync(Products: kvp.Value, gdax_Channel: kvp.Key, unSubscribe: true);
			}
			return await sendSubscriptionMsgAsync(Products: productsToSubscribeFiltered, gdax_Channel: gdax_Channel, unSubscribe: false);
		}

		public async Task<bool> UnsubscribeAsync(string[] Products)
		{
			var productsToUnsubscribe = new SortedList<GDAX_Channel, List<string>>(); // value is a list of products to unsubscribe from
			foreach (var product in Products)
			{
				if (subscribedProducts.ContainsKey(product))
				{ // if currently subscribed to any channel, then unsubscribe
					var currentlySubscribedChannel = subscribedProducts[product];
					if (!productsToUnsubscribe.ContainsKey(currentlySubscribedChannel))
						productsToUnsubscribe.Add(currentlySubscribedChannel, new List<string>());
					productsToUnsubscribe[currentlySubscribedChannel].Add(product);
				}
			}
			foreach (var kvp in productsToUnsubscribe)
			{ // unsubscribe
				await sendSubscriptionMsgAsync(Products: kvp.Value, gdax_Channel: kvp.Key, unSubscribe: true);
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Products"></param>
		/// <param name="gdax_Channel"></param>
		/// <returns>if submission of subscription request succeeded (not whether the subsciption actually succeeded)</returns>
		private async Task<bool> sendSubscriptionMsgAsync(IEnumerable<string> Products, GDAX_Channel gdax_Channel = GDAX_Channel.full, bool unSubscribe = false)
		{
			if (webSocketClient.State == System.Net.WebSockets.WebSocketState.Open)
			{
				foreach (var product in Products)
				{
					if (String.IsNullOrWhiteSpace(product))
						throw new ArgumentNullException("Products");
				}
				var productsString = Products.Aggregate((a, b) => a + "\", \"" + b);
				var subAction = unSubscribe ? "unsubscribe" : "subscribe";
				// enough for unauthenticated feed
				string requestStringSubset = String.Format(
					@"""type"": ""{0}"",""product_ids"": [""{1}""],""channels"": [""heartbeat"",""{2}""]",
					subAction, productsString, gdax_Channel);
				string requestString;
				if (_authContainer == null)
				{ // unauthenticated feed
					requestString = String.Format(@"{{{0}}}", requestStringSubset);
				}
				else
				{ // authenticated feed
					var signBlock = _authContainer.ComputeSignature(relativeUrl: "/users/self/verify", method: "GET", body: "");
					requestString = String.Format(
						@"{{{0},""signature"": ""{1}"",""key"": ""{2}"",""passphrase"": ""{3}"",""timestamp"": ""{4}""}}",
						requestStringSubset, signBlock.Signature, signBlock.ApiKey, signBlock.Passphrase, signBlock.TimeStamp);
				}
				var requestBytes = UTF8Encoding.UTF8.GetBytes(requestString);
				var subscribeRequest = new ArraySegment<byte>(requestBytes);
				await webSocketClient.SendAsync(subscribeRequest, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
				return true;
			}
			else
			{
				RealtimeStreamError?.Invoke(this, new RealtimeError("Unable to send subscription msg - websocket not open"));
				return false;
			}
		}

		public async Task SubscribeAsync(bool reConnectOnDisconnect)
		{
			var uri = ExchangeClientBase.IsSandbox ? WSS_SANDBOX_ENDPOINT_URL : WSS_ENDPOINT_URL;
			if (_authContainer != null) // authenticated feed
				uri = new Uri(uri, "/users/self/verify");
			cancellationTokenSource = new CancellationTokenSource();

			while (!cancellationTokenSource.IsCancellationRequested)
			{
				string disconnectReason = "";
				try
				{
					webSocketClient = new ClientWebSocket();
					await webSocketClient.ConnectAsync(uri, cancellationTokenSource.Token);
					if (webSocketClient.State == System.Net.WebSockets.WebSocketState.Open && !cancellationTokenSource.IsCancellationRequested)
					{
						await rateGateRealtime.WaitToProceedAsync(); // don't subscribe at too high of a rate
						await sendSubscriptionMsgAsync(Products: Products, gdax_Channel: gdax_Channel);
						// key is product name, value is whether connection was just opened
						if (webSocketClient.State == System.Net.WebSockets.WebSocketState.Open && !cancellationTokenSource.IsCancellationRequested)
						{ // checking again bc maybe the server disconnected after the subscribe msg was sent
						  // + move to processing subscriptions section below later
							foreach (var product in Products)
							{
								ConnectionOpened?.Invoke(product, gdax_Channel);
							}
						}
						while (webSocketClient.State == System.Net.WebSockets.WebSocketState.Open && !cancellationTokenSource.IsCancellationRequested)
						{
							using (var timeoutCTS = new CancellationTokenSource(6500)) // heartbeat every 1000 ms, so give it 5 hearbeat chances
							using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCTS.Token, cancellationTokenSource.Token))
							using (var stream = new MemoryStream(1024))
							{
								var receiveBuffer = new ArraySegment<byte>(new byte[1024 * 8]);
								bool timedOut = false;
								WebSocketReceiveResult webSocketReceiveResult;
								do
								{
									try
									{
										webSocketReceiveResult = await webSocketClient.ReceiveAsync(receiveBuffer, linkedTokenSource.Token);
									}
									catch (OperationCanceledException)
									{
										timedOut = true;
										disconnectReason = " - stream timed out";
										break;
									}
									await stream.WriteAsync(receiveBuffer.Array, receiveBuffer.Offset, webSocketReceiveResult.Count, cancellationTokenSource.Token);
								} while (!webSocketReceiveResult.EndOfMessage && !cancellationTokenSource.IsCancellationRequested);

								if (!timedOut && !cancellationTokenSource.IsCancellationRequested)
								{
									var message = stream.ToArray().Where(b => b != 0).ToArray();
									var messageString = Encoding.ASCII.GetString(message, 0, message.Length);
									if (!String.IsNullOrEmpty(messageString))
									{
										try
										{
											var jToken = JToken.Parse(messageString);

											var typeToken = jToken["type"];
											if (typeToken == null)
											{
												RealtimeDataError?.Invoke(this, new RealtimeError("null typeToken: + " + Encoding.ASCII.GetString(message, 0, message.Length)));
												return; // go to next msg
											}

											var type = typeToken.Value<string>();
											switch (type)
											{
												case "subscriptions":
													// + process initial subscription confirmation
													// + also for unsubscribe confirmation
													break;
												case "received":
													var rr = new RealtimeReceived(jToken);
													if (rr.Message != null)
														RealtimeDataError?.Invoke(this, rr);
													RealtimeReceived?.Invoke(this, rr);
													break;
												case "open":
													var ro = new RealtimeOpen(jToken);
													if (ro.Message != null)
														RealtimeDataError?.Invoke(this, ro);
													RealtimeOpen?.Invoke(this, ro);
													break;
												case "done":
													var rd = new RealtimeDone(jToken);
													if (rd.Message != null)
														RealtimeDataError?.Invoke(this, rd);
													RealtimeDone?.Invoke(this, rd);
													break;
												case "match":
													var rm = new RealtimeMatch(jToken);
													if (rm.Message != null)
														RealtimeDataError?.Invoke(this, rm);
													RealtimeMatch?.Invoke(this, rm);
													break;
												case "last_match":
													var rlm = new RealtimeMatch(jToken);
													if (rlm.Message != null)
														RealtimeDataError?.Invoke(this, rlm);
													RealtimeLastMatch?.Invoke(this, rlm);
													break;
												case "change":
													var rc = new RealtimeChange(jToken);
													if (rc.Message != null)
														RealtimeDataError?.Invoke(this, rc);
													RealtimeChange?.Invoke(this, rc);
													break;
												case "heartbeat":
													// + should implement this (checking LastTraderId)
													var hb = new Heartbeat(jToken);
													Heartbeat?.Invoke(this, hb);
													break;
												case "error":
													RealtimeDataError?.Invoke(this, new RealtimeError(jToken));
													break;
												default:
													RealtimeDataError?.Invoke(this, new RealtimeError("Unexpected type: " + jToken));
													break;
											}

										}
										catch (JsonReaderException e)
										{
											RealtimeDataError?.Invoke(this, new RealtimeError(
												"JsonReaderException: " + e.Message + ":" + messageString));
										}
									}
									else RealtimeDataError?.Invoke(this, new RealtimeError("empty message received. Connection state: " 
										+ webSocketClient.State + ", linkedToken: " + linkedTokenSource.Token.IsCancellationRequested));
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					if (e.Message == "The remote party closed the WebSocket connection without completing the close handshake.") // System.Net.WebSockets.WebSocketException
						disconnectReason = " - remote closed the WebSocket w/o completing the close handshake";
					else if (e.Message == "Unable to connect to the remote server") // System.Net.WebSockets.WebSocketException
					{
						disconnectReason = " - unable to connect to server"; // shorten it a bit
						await Task.Delay(10000); // if unable to connect, then wait 10 seconds before trying to connect again
					}
					else
						RealtimeStreamError?.Invoke(this, new RealtimeError("other exception caught: " + e.GetType() + " : " + e.Message));
				}
				if (!reConnectOnDisconnect)
					UnSubscribe();
				foreach (var product in Products)
				{
					RealtimeStreamError?.Invoke(this, new RealtimeError("disconnected" + disconnectReason));
					ConnectionClosed?.Invoke(product, gdax_Channel);
				}
				if (!reConnectOnDisconnect)
					break;
			}
		}

		public void UnSubscribe()
		{
			cancellationTokenSource?.Cancel();
		}

		public void Dispose()
        {
            UnSubscribe();
			webSocketClient.Dispose();
        }
    }
}
