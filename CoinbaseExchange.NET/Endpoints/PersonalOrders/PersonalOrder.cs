using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLee.Utils.ExchangeBase;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public enum OrderStatus
	{
		// internal program statuses
		Created, // prior to submission
		Submitted, // prior to GDAX acknowledgement
		Rejected, // submitted, but not accepted by GDAX
		SyntheticFill, // internally filled

		// GDAX statuses
		Pending, // received by GDAX, prior to being "Received"?
		Active, // same as "received" realtime state on GDAX
		Open, // now on the order book 
		Done, // either cancelled or filled
		InvalidStatus, // unable to parse GDAX status
	 // settled (settled is a actually a separate boolean field in GDAX)
	}

	public static partial class EnumExtensionMethods
	{
		public static bool IsInPlay(this OrderStatus orderStatus)
		{
			return InPlay.Contains(orderStatus);
		}
		public static OrderStatus[] InPlay => new OrderStatus[]
		{
			OrderStatus.Created, OrderStatus.Submitted, OrderStatus.Pending,
			OrderStatus.Active, OrderStatus.Open
		};
		public static bool IsSubmittedButNotDone(this OrderStatus orderStatus)
		{
			return SubmittedButNotDone.Contains(orderStatus);
		}
		public static OrderStatus[] SubmittedButNotDone => new OrderStatus[]
		{
			OrderStatus.Submitted, OrderStatus.Pending,
			OrderStatus.Active, OrderStatus.Open
		};
		public static bool IsDone(this OrderStatus orderStatus)
		{
			return Done.Contains(orderStatus);
		}
		public static OrderStatus[] Done => new OrderStatus[]
		{
			OrderStatus.Done, OrderStatus.SyntheticFill,
			OrderStatus.Rejected, OrderStatus.InvalidStatus
		};
	}

	public class PersonalOrder
	{
		public Guid? ServerOrderId { get; set; }
		public decimal? Price { get; set; }
		public decimal? Size { get; set; }
		public string ProductName { get; set; }
		public Side Side { get; set; }
		/// <summary>
		/// market or limit
		/// </summary>
		public OrderType Type { get; set; }
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
		public OrderStatus Status { get; set; }
		public bool Settled { get; set; }
		[NotMapped]
		public string ErrorParsing { get; set; }

		public PersonalOrder() { }

		public PersonalOrder(PersonalOrder anotherPersonalOrder) : this()
		{
			this.ServerOrderId = anotherPersonalOrder.ServerOrderId;
			this.Price = anotherPersonalOrder.Price;
			this.Size = anotherPersonalOrder.Size;
			this.ProductName = anotherPersonalOrder.ProductName;
			this.Side = anotherPersonalOrder.Side;
			this.Type = anotherPersonalOrder.Type;
			this.TimeInForce = anotherPersonalOrder.TimeInForce;
			this.PostOnly = anotherPersonalOrder.PostOnly;
			this.CreatedAt = anotherPersonalOrder.CreatedAt;
			this.FilledFees = anotherPersonalOrder.FilledFees;
			this.FilledSize = anotherPersonalOrder.FilledSize;
			this.ExecutedValue = anotherPersonalOrder.ExecutedValue;
			this.Status = anotherPersonalOrder.Status;
			this.Settled = anotherPersonalOrder.Settled;
			this.ErrorParsing = anotherPersonalOrder.ErrorParsing;
		}

		/// <summary>
		/// ability to create a preliminary order in memory prior to submission
		/// </summary>
		/// <param name=""></param>
		/// <param name=""></param>
		public PersonalOrder(PersonalOrderParams orderParams, OrderStatus status) : this()
		{
			if (orderParams.ClientOrderId == null)
				throw new ArgumentNullException("orderParams.ClientOrderId");
			//this.ServerOrderId = orderParams.ClientOrderId.Value;
			this.Price = orderParams.Price;
			this.Size = orderParams.Size;
			this.ProductName = orderParams.ProductName;
			this.Side = orderParams.Side;
			this.Type = orderParams.Type;
			this.TimeInForce = orderParams.TimeInForce;
			this.PostOnly = orderParams.PostOnly != null ? orderParams.PostOnly.Value : false;
			this.CreatedAt = DateTime.UtcNow;
		}

		public PersonalOrder(JToken jToken) : this()
		{
			this.ServerOrderId = (Guid)jToken["id"];
			var priceToken = jToken["price"];
			if (priceToken != null)
				this.Price = priceToken.Value<Decimal>();
			var sizeToken = jToken["size"];
			if (sizeToken != null)
				this.Size = sizeToken.Value<Decimal>();
			this.ProductName = jToken["product_id"].Value<string>();
			var sideString = jToken["side"].Value<string>();
			var sideParseSuccess = Enum.TryParse<Side>(sideString, ignoreCase: true, result: out var sideEnum);
			if (sideParseSuccess)
				this.Side = sideEnum;
			else
			{
				this.ErrorParsing = "Error parsing: " + sideString;
			}
			var typeString = jToken["type"].Value<string>();
			var typeParseSuccess = Enum.TryParse<OrderType>(typeString, ignoreCase: true, result: out var typeEnum);
			if (typeParseSuccess)
				this.Type = typeEnum;
			else
			{
				this.ErrorParsing = "Error parsing: " + typeString;
			}
			var TIFToken = jToken["time_in_force"];
			if (TIFToken != null)
				this.TimeInForce = TIFToken.Value<string>();
			this.PostOnly = jToken["post_only"].Value<bool>();
			this.CreatedAt = jToken["created_at"].Value<DateTime>();
			this.FilledFees = jToken["fill_fees"].Value<Decimal>();
			this.FilledSize = jToken["filled_size"].Value<Decimal>();
			this.ExecutedValue = jToken["executed_value"].Value<Decimal>();
			var statusString = jToken["status"].Value<string>();
			var statusParseSuccess = Enum.TryParse<OrderStatus>(statusString, ignoreCase: true, result: out var statusEnum);
			if (statusParseSuccess)
				this.Status = statusEnum;
			else
			{
				this.Status = OrderStatus.InvalidStatus;
				this.ErrorParsing = "Error parsing: " + statusString;
			}
			this.Settled = jToken["settled"].Value<bool>();
		}
	}
}
