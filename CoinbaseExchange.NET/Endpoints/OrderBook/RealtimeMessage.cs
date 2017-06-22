using CoinbaseExchange.NET.Endpoints.PersonalOrders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLee.Utils.ExchangeBase;

namespace CoinbaseExchange.NET.Endpoints.OrderBook
{
    public class RealtimeMessage
    {
        public string Type { get; set; }
        public long Sequence { get; set; }
        public decimal? Price { get; set; }

        protected RealtimeMessage(JToken jToken)
        {
            this.Type = jToken["type"].Value<string>();
            this.Sequence = jToken["sequence"].Value<long>();
			var priceToken = jToken["price"];
			if (priceToken != null) // no "price" token in market orders
				this.Price = priceToken.Value<decimal>();
        }
    }

    public class RealtimeReceived : RealtimeMessage
    {
        public Guid OrderId { get; set; }
		/// <summary>
		/// The client_oid is different than the server-assigned order id. 
		/// If you are consuming the public feed and see a received message with your client_oid, 
		/// you should record the server-assigned order_id as it will be used for future order status updates. 
		/// The client_oid will NOT be used after the received message is sent.
		/// </summary>
		public Guid? ClientOrderId { get; set; }
		public decimal? Size { get; set; }
		// Market orders (indicated by the order_type field) may have an optional funds field which indicates how much quote currency will be used to buy or sell. For example, a funds field of 100.00 for the BTC-USD product would indicate a purchase of up to 100.00 USD worth of bitcoin.
		public Side Side { get; set; }

        public RealtimeReceived(JToken jToken) : base(jToken)
        {
            this.OrderId = (Guid)jToken["order_id"];
			var coidtoken = jToken["client_oid"];
			if (coidtoken != null)
				this.ClientOrderId = (Guid)coidtoken;
			var sizeToken = jToken["size"];
			if (sizeToken != null)
				this.Size = jToken["size"].Value<decimal>();
			var sideString = jToken["side"].Value<string>();
			var sideParseSuccess = Enum.TryParse<Side>(sideString, ignoreCase: true, result: out var sideEnum);
			if (sideParseSuccess)
				this.Side = sideEnum;
		}
	}

    public class RealtimeOpen : RealtimeMessage
    {
        public Guid OrderId { get; set; }
        public decimal RemainingSize { get; set; }
        public Side Side { get; set; }

        public RealtimeOpen(JToken jToken)
            : base(jToken)
        {
            this.OrderId = (Guid)jToken["order_id"];
            this.RemainingSize = jToken["remaining_size"].Value<decimal>();
			var sideString = jToken["side"].Value<string>();
			var sideParseSuccess = Enum.TryParse<Side>(sideString, ignoreCase: true, result: out var sideEnum);
			if (sideParseSuccess)
				this.Side = sideEnum;
		}
	}

    public class RealtimeDone : RealtimeMessage
    {
        public Guid OrderId { get; set; }
		/// <summary>
		/// market orders will not have a remaining_size or price field as they are never on the open order book at a given price.
		/// </summary>
		public decimal? RemainingSize { get; set; }
        public Side Side { get; set; }
        public string Reason { get; set; }

        public RealtimeDone(JToken jToken)
            : base(jToken)
        {
            this.OrderId = (Guid)jToken["order_id"];
			var RemainingSize = jToken["remaining_size"];
			if (RemainingSize != null)
				this.RemainingSize = jToken["remaining_size"].Value<decimal>();
			var sideString = jToken["side"].Value<string>();
			var sideParseSuccess = Enum.TryParse<Side>(sideString, ignoreCase: true, result: out var sideEnum);
			if (sideParseSuccess)
				this.Side = sideEnum;
			this.Reason = jToken["reason"].Value<string>();
        }

    }

    public class RealtimeMatch : RealtimeMessage
    {
        public long TradeId { get; set; }
        public Guid MakerOrderId { get; set; }
        public Guid TakerOrderId { get; set; }
        public DateTime Time { get; set; }
        public Side Side { get; set; }
		public decimal Size { get; set; }

		public RealtimeMatch(JToken jToken) : base(jToken)
        {
            this.TradeId = jToken["trade_id"].Value<long>();
			this.MakerOrderId = (Guid)jToken["maker_order_id"];
			this.TakerOrderId = (Guid)jToken["taker_order_id"];
            this.Time = jToken["time"].Value<DateTime>();
			var sideString = jToken["side"].Value<string>();
			var sideParseSuccess = Enum.TryParse<Side>(sideString, ignoreCase: true, result: out var sideEnum);
			if (sideParseSuccess)
				this.Side = sideEnum;
			this.Size = jToken["size"].Value<decimal>();
        }
    }

    public class RealtimeChange : RealtimeMessage
    {
        public Guid OrderId { get; set; }
        public DateTime Time { get; set; }
        public decimal NewSize { get; set; }
        public decimal OldSize { get; set; }
        public Side Side { get; set; }

        public RealtimeChange(JToken jToken)
            : base(jToken)
        {
            this.OrderId = (Guid)jToken["order_id"];
            this.Time = jToken["time"].Value<DateTime>();
            this.NewSize = jToken["new_size"].Value<decimal>();
            this.OldSize = jToken["old_size"].Value<decimal>();
			var sideString = jToken["side"].Value<string>();
			var sideParseSuccess = Enum.TryParse<Side>(sideString, ignoreCase: true, result: out var sideEnum);
			if (sideParseSuccess)
				this.Side = sideEnum;
		}
	}

    public class RealtimeError
    {
		/// <summary>
		/// Will be "error"
		/// </summary>
		public string Type { get; set; }
		public string Message { get; set; }
		public RealtimeError(JToken jToken)
        {
			this.Type = jToken["type"].Value<string>();
			this.Message = jToken["message"].Value<string>();
		}

		public RealtimeError(string ErrorMsg)
		{
			this.Message = ErrorMsg;
		}
	}
}
