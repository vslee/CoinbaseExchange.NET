# CoinbaseExchange.NET

A C# wrapper around the exchange.coinbase.com REST API

This project is a work in progress.

This is a fork of https://github.com/sefbkn/CoinbaseExchange.NET with some improvments I made



## Dependencies

* .NET Framework v4.5.1
* JSON.NET (via NuGet use: Install-Package Newtonsoft.JSON)
* VSLee.Utils (https://github.com/vslee/VSLee.Utils)
* RateGate https://github.com/Danthar/RateLimiting

## What is done already?
* Authentication
* Account endpoint (90%)
* Fills
* OrderBook / RealtimeOrderBook
* PersonalOrders (with submission and pagination)
* Products

## What needs to be completed
* Last 10% of the Accounts endpoint
  * The API states that there are 4 account history types (deposit, withdraw, match, fee) and does not detail what the structure of the responses will be for each of these. (See ./Endpoints/Account/AccountHistory.cs)
* The rest of the endpoints
* Pagination to be spread to all endpoints where it makes sense