using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class PersonalOrder
	{
		public string Id { get; set; }
		public decimal? Price { get; set; }
		public decimal? Size { get; set; }
		public string ProductId { get; set; }
		public string Side { get; set; }
		public string Type { get; set; }
		public string TimeInForce { get; set; }
		public bool PostOnly { get; set; }
		public DateTime CreatedAt { get; set; }
		public decimal FilledFees { get; set; }
		public decimal FilledSize { get; set; }
		public decimal ExecutedValue { get; set; }
		/// <summary>
		/// The HTTP Request will respond when an order is either rejected (insufficient funds, invalid parameters, etc) or received (accepted by the matching engine). A 200 response indicates that the order was received and is active. Active orders may execute immediately (depending on price and market conditions) either partially or fully. A partial execution will put the remaining size of the order in the open state. An order that is filled completely, will go into the done state.
		//		Users listening to streaming market data are encouraged to use the client_oid field to identify their received messages in the feed.The REST response with a server order_id may come after the received message in the public data feed.
		/// Orders which are no longer resting on the order book, will be marked with the done status. There is a small window between an order being done and settled. An order is settled when all of the fills have settled and the remaining holds (if any) have been removed.
		/// </summary>
		public string Status { get; set; }
		public bool Settled { get; set; }

		public PersonalOrder()
		{
		}

		public PersonalOrder(JToken jToken)
		{
			this.Id = jToken["id"].Value<string>();
			var priceToken = jToken["price"];
			if (priceToken != null)
				this.Price = priceToken.Value<Decimal>();
			var sizeToken = jToken["size"];
			if (sizeToken != null)
				this.Size = sizeToken.Value<Decimal>();
			this.ProductId = jToken["product_id"].Value<string>();
			this.Side = jToken["side"].Value<string>();
			this.Type = jToken["type"].Value<string>();
			var TIFToken = jToken["time_in_force"];
			if (TIFToken != null)
				this.TimeInForce = TIFToken.Value<string>();
			this.PostOnly = jToken["post_only"].Value<bool>();
			this.CreatedAt = jToken["created_at"].Value<DateTime>();
			this.FilledFees = jToken["fill_fees"].Value<Decimal>();
			this.FilledSize = jToken["filled_size"].Value<Decimal>();
			this.ExecutedValue = jToken["executed_value"].Value<Decimal>();
			this.Status = jToken["status"].Value<string>();
			this.Settled = jToken["settled"].Value<bool>();
		}
	}
}
