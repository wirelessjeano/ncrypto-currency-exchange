﻿using Lostics.NCryptoExchange.CoinsE;
using Lostics.NCryptoExchange.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Lostics.NCryptoExchangeTest
{
    [TestClass]
    public class CoinsETest
    {
        [TestMethod]
        public void TestParseAccountInfo()
        {
            JObject jsonObj = LoadTestData("get_wallets.json");
            AccountInfo accountInfo = CoinsEParsers.ParseAccountInfo(jsonObj);

            Assert.AreEqual(5, accountInfo.Wallets.Count);

            foreach (Wallet wallet in accountInfo.Wallets)
            {
                if (wallet.CurrencyCode.Equals("BTC"))
                {
                    Assert.AreEqual((decimal)3.05354081, wallet.Balance);
                }
            }
        }

        [TestMethod]
        public void TestParseMarkets()
        {
            JObject jsonObj = LoadTestData("list_markets.json");
            JArray marketsJson = jsonObj.Value<JArray>("markets");
            List<CoinsEMarket> markets = marketsJson.Select(
                market => CoinsEMarket.Parse(market as JObject)
            ).ToList();

            Assert.AreEqual(1, markets.Count);

            Assert.AreEqual(markets[0].BaseCurrencyCode, "WDC");
            Assert.AreEqual(markets[0].QuoteCurrencyCode, "BTC");
        }

        [TestMethod]
        public void TestParseMarketOrders()
        {
            JObject jsonObj = LoadTestData("depth.json");
            Book marketOrders = CoinsEParsers.ParseMarketOrders(jsonObj.Value<JObject>("marketdepth"));

            Assert.AreEqual(3, marketOrders.Buy.Count);
            Assert.AreEqual(1, marketOrders.Sell.Count);

            CoinsEMarketOrder lowestSellOrder = (CoinsEMarketOrder)marketOrders.Sell[0];

            Assert.AreEqual((decimal)2.92858000, lowestSellOrder.Price);
            Assert.AreEqual((decimal)8.98400000, lowestSellOrder.Quantity);
            Assert.AreEqual((decimal)8.98400000, lowestSellOrder.CummulativeQuantity);

            CoinsEMarketOrder highestBuyOrder = (CoinsEMarketOrder)marketOrders.Buy[2];

            Assert.AreEqual((decimal)0.12230000, highestBuyOrder.Price);
            Assert.AreEqual((decimal)11.00000000, highestBuyOrder.Quantity);
            Assert.AreEqual((decimal)16.40500000, highestBuyOrder.CummulativeQuantity);
        }

        [TestMethod]
        public void TestParseMarketTrades()
        {
            JObject jsonObj = LoadTestData("getmarkettrades.json");
            CoinsEMarketId marketId = new CoinsEMarketId("1");
            JArray marketTradesJson = jsonObj.Value<JArray>("return");
            TimeZoneInfo defaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            List<CoinsEMarketTrade> marketTrades = marketTradesJson.Select(
                marketTrade => CoinsEMarketTrade.Parse(marketTrade as JObject, marketId, defaultTimeZone)
            ).ToList();

            MarketTrade<CoinsEMarketId> mostRecentTrade = marketTrades[0];

            Assert.AreEqual("10958207", mostRecentTrade.TradeId.ToString());
            Assert.AreEqual((decimal)16433.01498728, mostRecentTrade.Quantity);
            Assert.AreEqual(OrderType.Sell, mostRecentTrade.OrderType);
        }

        [TestMethod]
        public void TestParseMyTrades()
        {
            JObject jsonObj = LoadTestData("getmytrades.json");
            CoinsEMarketId marketId = new CoinsEMarketId("132");
            TimeZoneInfo defaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            List<MyTrade<CoinsEMarketId, CoinsEOrderId>> trades = jsonObj.Value<JArray>("return").Select(
                marketTrade => CoinsEParsers.ParseMyTrade(marketTrade as JObject, marketId, defaultTimeZone)
            ).ToList();

            Assert.AreEqual(2, trades.Count);
            Assert.AreEqual("9373209", trades[0].TradeId.ToString());
            Assert.AreEqual("9164163", trades[1].TradeId.ToString());
        }

        private JObject LoadTestData(string filename)
        {
            string testDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string testDataDir = Path.Combine(Path.Combine(testDir, "Sample_Data"), "Coins-E");
            FileInfo fileName = new FileInfo(Path.Combine(testDataDir, filename));
            JObject jsonObj;

            using (StreamReader reader = new StreamReader(new FileStream(fileName.FullName, FileMode.Open)))
            {
                jsonObj = JObject.Parse(reader.ReadToEndAsync().Result);
            }

            return jsonObj;
        }
    }
}