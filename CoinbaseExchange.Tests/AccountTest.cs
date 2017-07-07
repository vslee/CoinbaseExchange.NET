using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoinbaseExchange.NET.Endpoints.Account;
using CoinbaseExchange.NET;
using CoinbaseExchange.NET.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoinbaseExchange.Tests
{
    [TestClass]
    public class AccountTest
    {
        [TestMethod]
        public void TestListAccounts()
        {
            var accounts = GetAccounts();
            // Do something with the response.
        }

		//[TestMethod]
		//public void TestGetAccountHistory()
		//{
		//	var accounts = GetAccountsOld().Accounts;
		//	foreach (var account in accounts)
		//	{
		//		var authContainer = GetAuthenticationContainer();
		//		//ExchangeClientBase.IsSandbox = true;
		//		var accountClient = new AccountClient(authContainer);
		//		var response = accountClient.GetAccountHistory(account.Id).Result;

		//		Assert.IsTrue(response.AccountHistoryRecords != null);
		//	}
		//}

		//[TestMethod]
		//public void TestGetAccountHolds()
		//{
		//	var accounts = GetAccountsOld().Accounts;
		//	// Do something with the response.

		//	foreach (var account in accounts)
		//	{
		//		var authContainer = GetAuthenticationContainer();
		//		//ExchangeClientBase.IsSandbox = true;
		//		var accountClient = new AccountClient(authContainer);
		//		var response = accountClient.GetAccountHolds(account.Id).Result;

		//		Assert.IsTrue(response.AccountHolds != null);
		//	}
		//}

		//private ListAccountsResponse GetAccountsOld()
		//{
		//	var authContainer = GetAuthenticationContainer();
		//	//ExchangeClientBase.IsSandbox = true;
		//	var accountClient = new AccountClient(authContainer);
		//	var response = accountClient.ListAccounts().Result;
		//	return response;
		//}

		[TestMethod]
        public async Task TestGetAccountHistoryAsync()
        {
            var accounts = await GetAccounts();
            foreach (var account in accounts.Accounts)
            {
                var authContainer = GetAuthenticationContainer();
                var accountClient = new AccountClient(authContainer);
				//ExchangeClientBase.IsSandbox = true;
				var response = await accountClient.GetAccountHistory(account.Id);

                Assert.IsTrue(response.AccountHistoryRecords != null);
            }
        }

        [TestMethod]
		public async Task TestGetAccountHoldsAsync()
        {
            var accounts = await GetAccounts();
            // Do something with the response.

            foreach (var account in accounts.Accounts)
            {
                var authContainer = GetAuthenticationContainer();
                var accountClient = new AccountClient(authContainer);
				//ExchangeClientBase.IsSandbox = true;
				var response = await accountClient.GetAccountHolds(account.Id);

                Assert.IsTrue(response.AccountHolds != null);
            }
        }

        private Task<ListAccountsResponse> GetAccounts()
        {
            var authContainer = GetAuthenticationContainer();
			//ExchangeClientBase.IsSandbox = true;
			var accountClient = new AccountClient(authContainer);
            return accountClient.ListAccounts();
        }

        private CBAuthenticationContainer GetAuthenticationContainer()
        {
            var authenticationContainer = new CBAuthenticationContainer(
                "", // API Key
                "", // Passphrase
                ""  // Secret
            );

            return authenticationContainer;
        }
    }
}
