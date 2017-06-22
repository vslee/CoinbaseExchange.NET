using CoinbaseExchange.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Endpoints.PersonalOrders
{
	public class PersonalOrdersClient : ExchangeClientBase
	{
		public PersonalOrdersClient(CBAuthenticationContainer authenticationContainer) 
			: base(authenticationContainer)
        {

		}

		//Int16 pageNumber = 0;
		GetPersonalOrdersResponse prevResponse;

		public async Task<GetPersonalOrdersResponse> GetPersonalOrdersAsync(string[] Status = null, string cursor = null) // Int16 cursor = 0)
		{
			//this.pageNumber = cursor;
			var request = new GetPersonalOrdersRequest(Status: Status, cursor: cursor);
			var response = await this.GetResponse(request);
			prevResponse = new GetPersonalOrdersResponse(response);
			return prevResponse;
		}

		/// <summary>
		/// The page before is a newer page and not one that happened before in chronological time.
		/// </summary>
		/// <param name="Status"></param>
		/// <returns></returns>
		public async Task<GetPersonalOrdersResponse> GetPersonalOrdersPageBeforeAsync(string[] Status = null)
		{
			return await GetPersonalOrdersAsync(Status, prevResponse.BeforePaginationToken); // (Int16)(pageNumber-1));
		}

		/// <summary>
		/// The page after is an older page and not one that happened after this one in chronological time.
		/// </summary>
		/// <param name="Status"></param>
		/// <returns></returns>
		public async Task<GetPersonalOrdersResponse> GetPersonalOrdersPageAfterAsync(string[] Status = null)
		{
			return await GetPersonalOrdersAsync(Status, prevResponse.AfterPaginationToken); // (, (Int16)(pageNumber+1));
		}

		public async Task<SubmitPersonalOrderResponse> SubmitPersonalOrderAsync(PersonalOrderParams orderParams)
		{
			var request = new SubmitPersonalOrderRequest(orderParams);
			var response = await this.GetResponse(request);
			return new SubmitPersonalOrderResponse(response);
		}

		public async Task<CancelPersonalOrderResponse> CancelPersonalOrderAsync(Guid orderID)
		{
			var request = new CancelPersonalOrderRequest(orderID);
			var response = await this.GetResponse(request);
			return new CancelPersonalOrderResponse(response);
		}

		public async Task<CancelAllPersonalOrdersResponse> CancelAllPersonalOrdersAsync(string product_id = null)
		{
			var request = new CancelAllPersonalOrdersRequest(product_id);
			var response = await this.GetResponse(request);
			return new CancelAllPersonalOrdersResponse(response);
		}
	}
}
