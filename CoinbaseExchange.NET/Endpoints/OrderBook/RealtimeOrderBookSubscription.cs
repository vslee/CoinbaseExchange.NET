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
		private readonly GDAX_Channel gdax_Channel;
		CancellationTokenSource cancellationTokenSource;
		// The websocket feed is publicly available, but connection to it are rate-limited to 1 per 4 seconds per IP.
		/// <summary>
		/// Only for subscribing to Websockets. Polling has its own RateGate
		/// </summary>
		private static readonly RateGate rateGateRealtime = new RateGate(occurrences: 1, timeUnit: new TimeSpan(0, 0, seconds: 4));
        public event EventHandler<RealtimeReceived> RealtimeReceived;
        public event EventHandler<RealtimeOpen> RealtimeOpen;
        public event EventHandler<RealtimeDone> RealtimeDone;
        public event EventHandler<RealtimeMatch> RealtimeMatch;
        public event EventHandler<RealtimeChange> RealtimeChange;
        /// <summary>
        /// Error in underlying websocket stream, need to refresh orderbook
        /// </summary>
        public event EventHandler<RealtimeError> RealtimeStreamError;
        /// <summary>
        /// Token error or cancellation error, no need to restart stream
        /// </summary>
        public event EventHandler<RealtimeMessage> RealtimeDataError;
		/// <summary>
		/// Event args is the product name
		/// </summary>
		public event EventHandler<string> ConnectionOpened;
		/// <summary>
		/// Event args is the product name
		/// </summary>
		public event EventHandler<string> ConnectionClosed;

        public RealtimeOrderBookSubscription(string[] Products, CBAuthenticationContainer auth = null,
			GDAX_Channel gdax_Channel = GDAX_Channel.full) : base(auth)
        { // + eventually can take an array of productStrings and subscribe simultaneously 
			this.Products = Products;
			if (Products == null || Products.Length == 0)
				throw new ArgumentNullException("Products");
			this.gdax_Channel = gdax_Channel;
        }

		public async Task SubscribeAsync(bool reConnectOnDisconnect)
		{
			await this.SubscribeAsync(reConnectOnDisconnect: reConnectOnDisconnect, processSequence: async (s) => true);
		}

		public async Task SubscribeAsync(bool reConnectOnDisconnect, Func<Int64, Task<bool>> processSequence)
		{
			foreach (var product in Products)
			{
				if (String.IsNullOrWhiteSpace(product))
					throw new ArgumentNullException("Products");
			}

			var uri = ExchangeClientBase.IsSandbox ? WSS_SANDBOX_ENDPOINT_URL : WSS_ENDPOINT_URL;
			var productsString = Products.Aggregate((a, b) => a + "\", \"" + b);
			// enough for unauthenticated feed
			string requestStringSubset = String.Format(
				@"""type"": ""subscribe"",""product_ids"": [""{0}""],""channels"": [""heartbeat"",""full""]", productsString);
			string requestString;
			if (_authContainer == null)
			{ //  
				requestString = String.Format(@"{{{0}}}", requestStringSubset);
			}
			else
			{ // authenticated feed
				var signBlock = _authContainer.ComputeSignature(relativeUrl: "/users/self/verify", method: "GET", body: "");
				requestString = String.Format(
					@"{{{0},""signature"": ""{1}"",""key"": ""{2}"",""passphrase"": ""{3}"",""timestamp"": ""{4}""}}",
					requestStringSubset, signBlock.Signature, signBlock.ApiKey, signBlock.Passphrase, signBlock.TimeStamp);
				uri = new Uri(uri, "/users/self/verify");
			}

			cancellationTokenSource = new CancellationTokenSource();

			while (!cancellationTokenSource.IsCancellationRequested)
			{
				string disconnectReason = "";
				try
				{
					var webSocketClient = new ClientWebSocket();
					await webSocketClient.ConnectAsync(uri, cancellationTokenSource.Token);
					if (webSocketClient.State == System.Net.WebSockets.WebSocketState.Open)
					{
						var requestBytes = UTF8Encoding.UTF8.GetBytes(requestString);
						var subscribeRequest = new ArraySegment<byte>(requestBytes);
						await rateGateRealtime.WaitToProceedAsync(); // don't subscribe at too high of a rate
						await webSocketClient.SendAsync(subscribeRequest, WebSocketMessageType.Text, true, cancellationTokenSource.Token);
						// key is product name, value is whether connection was just opened
						SortedList<string, bool> justconnectedSL = new SortedList<string, bool>();
						foreach (var product in Products)
						{
							justconnectedSL.Add(product, true);
						}

						while (webSocketClient.State == System.Net.WebSockets.WebSocketState.Open && !cancellationTokenSource.IsCancellationRequested)
						{
							foreach (var product in justconnectedSL.Keys.ToArray())
							{
								if (justconnectedSL[product])
								{
									ConnectionOpened?.Invoke(this, product);
									justconnectedSL[product] = false;
								}
							}
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
								} while (!webSocketReceiveResult.EndOfMessage);

								if (!timedOut)
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
													break;
												case "received":
													var rr = new RealtimeReceived(jToken);
													if (rr.Message != null)
														RealtimeDataError?.Invoke(this, rr);
													if (await processSequence(rr.Sequence))
														RealtimeReceived?.Invoke(this, rr);
													break;
												case "open":
													var ro = new RealtimeOpen(jToken);
													if (ro.Message != null)
														RealtimeDataError?.Invoke(this, ro);
													if (await processSequence(ro.Sequence))
														RealtimeOpen?.Invoke(this, ro);
													break;
												case "done":
													var rd = new RealtimeDone(jToken);
													if (rd.Message != null)
														RealtimeDataError?.Invoke(this, rd);
													if (await processSequence(rd.Sequence))
														RealtimeDone?.Invoke(this, rd);
													break;
												case "match":
													var rm = new RealtimeMatch(jToken);
													if (rm.Message != null)
														RealtimeDataError?.Invoke(this, rm);
													if (await processSequence(rm.Sequence))
														RealtimeMatch?.Invoke(this, rm);
													break;
												case "change":
													var rc = new RealtimeChange(jToken);
													if (rc.Message != null)
														RealtimeDataError?.Invoke(this, rc);
													if (await processSequence(rc.Sequence))
														RealtimeChange?.Invoke(this, rc);
													break;
												case "heartbeat":
													// + should implement this
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
				foreach (var product in Products)
				{
					RealtimeStreamError?.Invoke(this, new RealtimeError("disconnected" + disconnectReason));
					ConnectionClosed?.Invoke(this, product);
				}
				if (!reConnectOnDisconnect)
					UnSubscribe();
			}
		}

		public void UnSubscribe()
        {
 			cancellationTokenSource?.Cancel();
       }

        public void Dispose()
        {
            UnSubscribe();
        }
    }
}
