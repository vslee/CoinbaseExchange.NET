using CoinbaseExchange.NET.Endpoints.PersonalOrders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using VSLee.Utils;

namespace CoinbaseExchange.NET.Core
{
    public abstract class ExchangeClientBase
    {
		public readonly Uri API_SANDBOX_ENDPOINT_URL = new Uri("https://api-public.sandbox.gdax.com");
		public readonly Uri API_ENDPOINT_URL = new Uri("https://api.gdax.com");
        private const string ContentType = "application/json";
		public static bool IsSandbox { get; set; }
		// We throttle private endpoints by user ID: 5 requests per second, up to 10 requests per second in bursts.
		// We throttle public endpoints by IP: 3 requests per second, up to 6 requests per second in bursts.
		/// <summary>
		/// For polling queries. Assuming private (authenticated) access. If unauthenticated, will need to adjust parameters. Realtime has its own RateGate.
		/// </summary>
		private static readonly RateGate rateGatePolling = new RateGate(occurrences: 5, timeUnit: new TimeSpan(0, 0, seconds: 1));

		protected readonly CBAuthenticationContainer _authContainer;

        public ExchangeClientBase(CBAuthenticationContainer authContainer)
        {
            _authContainer = authContainer;
        }

        protected async Task<ExchangeResponse> GetResponse(ExchangeRequestBase request)
        {
			var relativeUrlForURL = request.RequestUrl;
			var baseURI = IsSandbox ? API_SANDBOX_ENDPOINT_URL : API_ENDPOINT_URL;
			var absoluteUri = new Uri(baseURI, relativeUrlForURL);
			var uriBuilder = new UriBuilder(absoluteUri);
			uriBuilder.Port = -1;

			// add query parameters
			var requestCasted = request as ExchangePageableRequestBase;
			var nvc = HttpUtility.ParseQueryString(string.Empty);
			if (request is ExchangePageableRequestBase)
			{
				if (requestCasted.afterCursor != null)
					nvc["after"] = requestCasted.afterCursor;
				if (requestCasted.RecordCount != null)
					nvc["limit"] = requestCasted.RecordCount.Value.ToString();
				//if (requestCasted.ZeroBasedCursor < 0)
				//{
				//	nvc["before"] = (Math.Abs(requestCasted.ZeroBasedCursor-1)).ToString(); // change to 1 based
				//	if (requestCasted.RecordCount != null)
				//		nvc["limit"] = requestCasted.RecordCount.Value.ToString();
				//}
				//else if (requestCasted.ZeroBasedCursor > 0)
				//{
				//	nvc["after"] = (requestCasted.ZeroBasedCursor+1).ToString(); // change to 1 based
				//	if (requestCasted.RecordCount != null)
				//		nvc["limit"] = requestCasted.RecordCount.Value.ToString();
				//}
				// else it's zero so no need to put any query parameter
			}
			if (request is GetPersonalOrdersRequest)
			{
				var requestCasted2 = request as GetPersonalOrdersRequest;
				if (requestCasted2.Status != null)
					foreach (var status in requestCasted2.Status)
						nvc.Add("status", status);
				if(requestCasted2.productName != null)
					nvc.Add("product_id", requestCasted2.productName);
			}
			if (request is CancelAllPersonalOrdersRequest)
			{
				var requestCasted3 = request as CancelAllPersonalOrdersRequest;
				if (requestCasted3.product_id != null)
					nvc["product_id"] = requestCasted3.product_id;
			}
			if (nvc.Keys.Count > 0)
				uriBuilder.Query = nvc.ToString();

            var body = request.RequestBody;
            var method = request.Method;
            var url = uriBuilder.ToString();
			var relativeUrlForSignature = baseURI.MakeRelativeUri(uriBuilder.Uri).ToString();
			await rateGatePolling.WaitToProceedAsync(); // rate limit prior to TimeStamp being generated

			try
			{
				using (var httpClient = new HttpClient())
				{
					if (_authContainer != null)
					{ // authenticated get, required for querying account specific data, but optional for public data
					  // Caution: Use the relative URL, *NOT* the absolute one.
						var signature = _authContainer.ComputeSignature("/" + relativeUrlForSignature, method, body);
						httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", signature.ApiKey);
						httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", signature.Signature);
						httpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", signature.TimeStamp);
						httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", signature.Passphrase);
					}

					httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType));
					httpClient.DefaultRequestHeaders.Add("User-Agent", "vslee fork of sefbkn.github.io");

					HttpResponseMessage response;
					switch (method)
					{
						case "GET":
							response = await httpClient.GetAsync(url);
							break;
						case "POST":
							var requestBody = new StringContent(body, Encoding.UTF8, "application/json");
							response = await httpClient.PostAsync(url, requestBody);
							break;
						case "DELETE":
							response = await httpClient.DeleteAsync(url);
							break;
						case "PUT":
							throw new NotImplementedException("PUT");
						default:
							throw new NotImplementedException("The supplied HTTP method is not supported: " + method ?? "(null)");
					}

					var contentBody = await response.Content.ReadAsStringAsync();
					var headers = response.Headers.AsEnumerable();
					var statusCode = response.StatusCode;
					var isSuccess = response.IsSuccessStatusCode;

					return new ExchangeResponse(statusCode, isSuccess, headers, contentBody);
				}
			}
			catch (HttpRequestException e)
			{ // HttpRequestException: An error occurred while sending the request.
				return new ExchangeResponse(isSuccess: false, ErrorMessage: e.Message);
			}
			catch (WebException e)
			{ // WebException: Unable to connect to the remote server
				return new ExchangeResponse(isSuccess: false, ErrorMessage: e.Message);
			}
			catch (System.Net.Sockets.SocketException e)
			{// SocketException: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
				return new ExchangeResponse(isSuccess: false, ErrorMessage: e.Message);
			}
		}

    }
}
