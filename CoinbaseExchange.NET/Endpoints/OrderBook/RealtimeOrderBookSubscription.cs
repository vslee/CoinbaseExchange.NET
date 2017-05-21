using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
	public class RealtimeOrderBookSubscription : ExchangeClientBase
	{
		public static readonly Uri WSS_SANDBOX_ENDPOINT_URL = new Uri("wss://ws-feed-public.sandbox.gdax.com");
		public static readonly Uri WSS_ENDPOINT_URL = new Uri("wss://ws-feed.gdax.com");
		private readonly string ProductString;
		public Action<RealtimeReceived> RealtimeReceived;
		public Action<RealtimeOpen> RealtimeOpen;
		public Action<RealtimeDone> RealtimeDone;
		public Action<RealtimeMatch> RealtimeMatch;
		public Action<RealtimeChange> RealtimeChange;
		public Action<RealtimeError> RealtimeError;

		public RealtimeOrderBookSubscription(string ProductString, CBAuthenticationContainer auth = null) : base(auth)
		{ // + eventually can take an array of productStrings and subscribe simultaneously 
			this.ProductString = ProductString;
		}

		/// <summary>
		/// Authenticated feed messages will not increment the sequence number. It is currently not possible to detect if an authenticated feed message was dropped.
		/// </summary>
		/// <param name="onMessageReceived"></param>
		public async void Subscribe()
        {
            if (String.IsNullOrWhiteSpace(ProductString))
                throw new ArgumentNullException("product");

            var webSocketClient = new ClientWebSocket();
            var cancellationToken = new CancellationToken();
			string requestString;
			var uri = ExchangeClientBase.IsSandbox ? WSS_SANDBOX_ENDPOINT_URL : WSS_ENDPOINT_URL;
			if (_authContainer == null)
			{ // unauthenticated feed 
				requestString = String.Format(@"{{""type"": ""subscribe"",""product_id"": ""{0}""}}", ProductString);
			}
			else
			{ // authenticated feed
				var signBlock = _authContainer.ComputeSignature(relativeUrl: "/users/self", method: "GET", body: "");
				requestString = String.Format(
					@"{{""type"": ""subscribe"",""product_id"": ""{0}"",""signature"": ""{1}"",""key"": ""{2}"",""passphrase"": ""{3}"",""timestamp"": ""{4}""}}",
					ProductString, signBlock.Signature, signBlock.ApiKey, signBlock.Passphrase, signBlock.TimeStamp);
				uri = new Uri(uri, "/users/self");
			}
			var requestBytes = UTF8Encoding.UTF8.GetBytes(requestString);
            await webSocketClient.ConnectAsync(uri, cancellationToken);

            if (webSocketClient.State == WebSocketState.Open)
            {
                var subscribeRequest = new ArraySegment<byte>(requestBytes);
                var sendCancellationToken = new CancellationToken();
                await webSocketClient.SendAsync(subscribeRequest, WebSocketMessageType.Text, true, sendCancellationToken);

                while (webSocketClient.State == WebSocketState.Open)
                {
                    var receiveCancellationToken = new CancellationToken();
                    var receiveBuffer = new ArraySegment<byte>(new byte[1024 * 1024 * 5]); // 5MB buffer
                    var webSocketReceiveResult = await webSocketClient.ReceiveAsync(receiveBuffer, receiveCancellationToken);
                    if (webSocketReceiveResult.Count == 0) continue;

                    var jsonResponse = Encoding.UTF8.GetString(receiveBuffer.Array, 0, webSocketReceiveResult.Count);
                    var jToken = JToken.Parse(jsonResponse);

                    var typeToken = jToken["type"];
                    if (typeToken == null) continue;

                    var type = typeToken.Value<string>();

                    switch (type)
                    {
                        case "received":
							RealtimeReceived?.Invoke(new RealtimeReceived(jToken));
							break;
                        case "open":
							RealtimeOpen?.Invoke(new RealtimeOpen(jToken));
                            break;
                        case "done":
							RealtimeDone?.Invoke(new RealtimeDone(jToken));
                            break;
                        case "match":
							RealtimeMatch?.Invoke(new RealtimeMatch(jToken));
                            break;
                        case "change":
							RealtimeChange?.Invoke(new RealtimeChange(jToken));
                            break;
						case "heartbeat":
							// + should implement this
							break;
						case "error":
							RealtimeError?.Invoke(new RealtimeError(jToken));
							break;
						default:
                            break;
                    }
                }
            }
        }
	}
}
