﻿using CoinbaseExchange.NET.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLee.Utils;
using WebSocketSharp;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
    public class RealtimeOrderBookSubscription : ExchangeClientBase, IDisposable
    {
        public static readonly Uri WSS_SANDBOX_ENDPOINT_URL = new Uri("wss://ws-feed-public.sandbox.gdax.com");
        public static readonly Uri WSS_ENDPOINT_URL = new Uri("wss://ws-feed.gdax.com");
        private readonly string ProductString;
        WebSocket ws;
		bool unSubscribing;
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
        public event EventHandler<RealtimeError> RealtimeDataError;
		public event EventHandler ConnectionClosed;

        public RealtimeOrderBookSubscription(string ProductString, CBAuthenticationContainer auth = null) : base(auth)
        { // + eventually can take an array of productStrings and subscribe simultaneously 
            this.ProductString = ProductString;
        }

		public async Task SubscribeAsync(bool reConnectOnDisconnect)
		{
			await this.SubscribeAsync(reConnectOnDisconnect: reConnectOnDisconnect, processSequence: async (s) => true);
		}

		/// <summary>
		/// Don't await this - or it won't return until the subscription ends
		/// Authenticated feed messages will not increment the sequence number. It is currently not possible to detect if an authenticated feed message was dropped.
		/// </summary>
		/// <param name="onMessageReceived"></param>
		public async Task SubscribeAsync(bool reConnectOnDisconnect, Func<Int64, Task<bool>> processSequence)
		{
			if (String.IsNullOrWhiteSpace(ProductString))
				throw new ArgumentNullException("product");

			string requestString;
			unSubscribing = false;
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

			ws = new WebSocket(uri.ToString());
			ws.OnMessage += (sender, e) =>
			{
				var jToken = JToken.Parse(e.Data);

				var typeToken = jToken["type"];
				if (typeToken == null)
				{
					RealtimeDataError?.Invoke(this, new RealtimeError("null typeToken: + " + e.Data));
					return; // go to next msg
				}

				var type = typeToken.Value<string>();
				switch (type)
				{
					case "received":
						var rr = new RealtimeReceived(jToken);
						if (processSequence(rr.Sequence).Result)
							RealtimeReceived?.Invoke(this, rr);
						break;
					case "open":
						var ro = new RealtimeOpen(jToken);
						if (processSequence(ro.Sequence).Result)
							RealtimeOpen?.Invoke(this, ro);
						break;
					case "done":
						var rd = new RealtimeDone(jToken);
						if (processSequence(rd.Sequence).Result)
							RealtimeDone?.Invoke(this, rd);
						break;
					case "match":
						var rm = new RealtimeMatch(jToken);
						if (processSequence(rm.Sequence).Result)
							RealtimeMatch?.Invoke(this, rm);
						break;
					case "change":
						var rc = new RealtimeChange(jToken);
						if (processSequence(rc.Sequence).Result)
							RealtimeChange?.Invoke(this, rc);
						break;
					case "heartbeat":
						// + should implement this
						break;
					case "error":
						RealtimeDataError?.Invoke(this, new RealtimeError(jToken));
						break;
					default:
						break;
				}
			};
			ws.OnError += async (s, e) => 
			{
				RealtimeStreamError?.Invoke(this, new RealtimeError(e.Exception + e.Message));
				ConnectionClosed?.Invoke(this, e);
				if (!unSubscribing & reConnectOnDisconnect)
					await connectAndSend(requestString);
			};
			ws.OnClose += async (s, e) =>
			{
				RealtimeStreamError?.Invoke(this, new RealtimeError("Connection closed: " + e.Reason + " Product: " + ProductString));
				ConnectionClosed?.Invoke(this, e);
				if (!unSubscribing & reConnectOnDisconnect)
					await connectAndSend(requestString);
			};

			await connectAndSend(requestString);
		}

		private async Task connectAndSend(string requestString)
		{
			await rateGateRealtime.WaitToProceedAsync(); // don't subscribe at too high of a rate
			ws.Connect();
			ws.Send(requestString);
		}

		public void UnSubscribe()
        {
			unSubscribing = true;
            if (ws != null)
            {
                ws.Close();
                ((IDisposable)ws).Dispose();
            }
        }

        public void Dispose()
        {
            UnSubscribe();
        }
    }
}
