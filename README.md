# GDAX/CoinbaseExchange.NET

A C# wrapper around the GDAX/Coinbase exchange API

This is a fork of https://github.com/sefbkn/CoinbaseExchange.NET with some improvements I made



## Dependencies

* .NET Framework v4.5.1 or greater
* JSON.NET (via NuGet use: Install-Package Newtonsoft.JSON)
* VSLee.Utils (https://github.com/vslee/VSLee.Utils)
* RateGate (included in VSLee.Utils)

## What is done already?
* Authentication
* Account endpoint (90%)
* Fills
* OrderBook / RealtimeOrderBook
* PersonalOrders (with submission, cancellation, and pagination)
* Products
* Note missing pieces below

## What needs to be completed
* Last 10% of the Accounts endpoint
  * The API states that there are 4 account history types (deposit, withdraw, match, fee) and does not detail what the structure of the responses will be for each of these. (See ./Endpoints/Account/AccountHistory.cs)
* The rest of the endpoints
* Pagination to be spread to all endpoints where it makes sense
* Bugs. This library has numerous bugs. I have tried to fix as many of them as I could, but undoubtedly many still exist. If you use this, you will likely run into one which causes you to lose a lot of money. You have been warned. 
* Please feel free to send pull requests fixing any bugs you may have found. 