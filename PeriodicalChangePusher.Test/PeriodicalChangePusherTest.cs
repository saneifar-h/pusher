using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using PeriodicalChangePusher.Core;

namespace PeriodicalChangePusher.Test
{
    [TestClass]
    public class PeriodicalChangePusherTest
    {
        private const string BidAskTopic = "bidAsk";
        private const string TradeTopic = "trade";
        private const string DetailTopic = "detail";

        [TestMethod]
        public void Test_Default_Of_KeyValuePair()
        {
            var dic = new ConcurrentDictionary<string, KeyValuePair<long, string>>();
            dic.TryAdd("dd", new KeyValuePair<long, string>(10, "dsjkhjk"));
            dic.TryGetValue("test", out var foundItem);
            Assert.AreEqual(0, foundItem.Key);
            Assert.AreEqual(null, foundItem.Value);
        }

        [TestMethod]
        public void TestSimpleMultiTopicListen()
        {
            var intervalDataProvider = Substitute.For<IIntervalDataProvider>();
            var initialDataProvider = Substitute.For<IInitialDataProvider>();
            intervalDataProvider.GetInterval(BidAskTopic).Returns(1000);
            intervalDataProvider.GetInterval(TradeTopic).Returns(2000);
            intervalDataProvider.GetInterval(DetailTopic).Returns(1500);
            var lstResult = new List<string>();

            var listener = Substitute.For<IPushSubscriber>();
            listener.When(x => x.OnPush(Arg.Any<string>(), Arg.Any<IReadOnlyList<KeyValuePair<string, object>>>())).Do(x =>
           {
               var topic = (string)x.Args()[0];
               var value = (IEnumerable<KeyValuePair<string, object>>)x.Args()[1];
               foreach (var item in value)
               {
                   lstResult.Add($"rec topic:{topic} key:{item.Key} value:{item.Value} time:{DateTime.Now.TimeOfDay}");
               }
           });

            var periodicalChangePusher = new Core.PeriodicalChangePusher(intervalDataProvider, initialDataProvider);
            periodicalChangePusher.Register(listener, BidAskTopic);
            periodicalChangePusher.Register(listener, TradeTopic);
            periodicalChangePusher.Register(listener, DetailTopic);

            periodicalChangePusher.Save(BidAskTopic, "1", "test10");
            periodicalChangePusher.Save(TradeTopic, "2", "test20");
            periodicalChangePusher.Save(DetailTopic, "3", "test30");
            lstResult.Add($"start time:{DateTime.Now.TimeOfDay}");
            Task.Delay(1200).Wait();
            periodicalChangePusher.Save(BidAskTopic, "1", "test11");
            periodicalChangePusher.Save(TradeTopic, "2", "test21");
            Task.Delay(500).Wait();
            periodicalChangePusher.Save(BidAskTopic, "1", "test12");
            periodicalChangePusher.Save(TradeTopic, "2", "test22");
            periodicalChangePusher.Save(DetailTopic, "3", "test31");
            Task.Delay(2000).Wait();
            periodicalChangePusher.Save(DetailTopic, "3", "test32");


            Task.Delay(5000).Wait();
            Assert.AreEqual(7, lstResult.Count);
            Assert.IsTrue(lstResult.Any(i => i.StartsWith("rec topic:bidAsk key:1 value:test10 ")));
            Assert.IsTrue(lstResult.Any(i => i.StartsWith("rec topic:detail key:3 value:test30 ")));
            Assert.IsTrue(lstResult.Any(i => i.StartsWith("rec topic:detail key:3 value:test32 ")));
            Assert.IsTrue(lstResult.Count(i => i.StartsWith("rec topic:detail key:3")) == 3);
            Assert.IsTrue(lstResult.Count(i => i.StartsWith("rec topic:bidAsk key:1")) == 2);
            Assert.IsTrue(lstResult.Count(i => i.StartsWith("rec topic:trade key:2 ")) == 1);

        }


        [TestMethod]
        public void TestSingleTopicForChangePush()
        {
            var intervalDataProvider = Substitute.For<IIntervalDataProvider>();
            var initialDataProvider = Substitute.For<IInitialDataProvider>();
            intervalDataProvider.GetInterval(BidAskTopic).Returns(1000);
            var lstResult = new List<string>();

            var listener = Substitute.For<IPushSubscriber>();
            listener.When(x => x.OnPush(Arg.Any<string>(), Arg.Any<IReadOnlyList<KeyValuePair<string, object>>>())).Do(x =>
            {
                var topic = (string)x.Args()[0];
                var values = (IEnumerable<KeyValuePair<string, object>>)x.Args()[1];
                foreach (var item in values)
                {
                    lstResult.Add($"rec topic:{topic} key:{item.Key} value:{item.Value} time:{DateTime.Now.TimeOfDay}");
                }
            });

            var periodicalChangePusher = new Core.PeriodicalChangePusher(intervalDataProvider, initialDataProvider);
            periodicalChangePusher.Register(listener, BidAskTopic);
            lstResult.Add($"start time {DateTime.Now.TimeOfDay}");
            periodicalChangePusher.Save(BidAskTopic, "1", "test10");
            periodicalChangePusher.Save(BidAskTopic, "1", "test11");
            periodicalChangePusher.Save(BidAskTopic, "1", "test12");
            periodicalChangePusher.Save(BidAskTopic, "1", "test13");
            periodicalChangePusher.Save(BidAskTopic, "1", "test14");

            Task.Delay(800).Wait();
            periodicalChangePusher.Save(BidAskTopic, "1", "test15");

            Task.Delay(500).Wait();
            periodicalChangePusher.Save(BidAskTopic, "1", "test16");
            periodicalChangePusher.Save(BidAskTopic, "1", "test17");
            periodicalChangePusher.Save(BidAskTopic, "1", "test18");
            Task.Delay(2000).Wait();
            periodicalChangePusher.Save(BidAskTopic, "1", "test19");
            periodicalChangePusher.Save(BidAskTopic, "1", "test20");
            periodicalChangePusher.Save(BidAskTopic, "1", "test21");
            periodicalChangePusher.Save(BidAskTopic, "1", "test22");

            Task.Delay(5000).Wait();

            Assert.IsTrue(lstResult[1].StartsWith("rec topic:bidAsk key:1 value:test15 "));
            Assert.IsTrue(lstResult[2].StartsWith("rec topic:bidAsk key:1 value:test18 "));
            Assert.IsTrue(lstResult[3].StartsWith("rec topic:bidAsk key:1 value:test22 "));
            Assert.AreEqual(4, lstResult.Count);

        }

        [TestMethod]
        public void TestInitialData()
        {
            var intervalDataProvider = Substitute.For<IIntervalDataProvider>();
            var initialDataProvider = Substitute.For<IInitialDataProvider>();
            initialDataProvider.Provide(BidAskTopic, "1").Returns("Value1");
            initialDataProvider.Provide(BidAskTopic, "2").Returns("Value2");
            intervalDataProvider.GetInterval(BidAskTopic).Returns(1000);
            var periodicalChangePusher = new Core.PeriodicalChangePusher(intervalDataProvider, initialDataProvider);
            Assert.AreEqual("Value1", periodicalChangePusher.Load(BidAskTopic, "1"));
            Assert.AreEqual("Value2", periodicalChangePusher.Load(BidAskTopic, "2"));
            Assert.AreEqual("Value2", periodicalChangePusher.Load(BidAskTopic, "2"));
            Assert.AreEqual("Value1", periodicalChangePusher.Load(BidAskTopic, "1"));
        }

        [TestMethod]
        public void TestLoadData()
        {
            var intervalDataProvider = Substitute.For<IIntervalDataProvider>();
            var initialDataProvider = Substitute.For<IInitialDataProvider>();
            var initialBidAsk = "initial BidAsk";
            initialDataProvider.Provide(BidAskTopic, Arg.Any<string>()).Returns(initialBidAsk);
            var initialDetail = "initial Detail";
            initialDataProvider.Provide(DetailTopic, Arg.Any<string>()).Returns(initialDetail);
            var initialTrade = "initial Trade";
            initialDataProvider.Provide(TradeTopic, Arg.Any<string>()).Returns(initialTrade);
            intervalDataProvider.GetInterval(BidAskTopic).Returns(1000);
            intervalDataProvider.GetInterval(TradeTopic).Returns(2000);
            intervalDataProvider.GetInterval(DetailTopic).Returns(1500);
            var lstResult = new List<string>();

            var listener = Substitute.For<IPushSubscriber>();
            listener.When(x => x.OnPush(Arg.Any<string>(), Arg.Any<IReadOnlyList<KeyValuePair<string, object>>>())).Do(x =>
            {
                var topic = (string)x.Args()[0];
                var value = (IEnumerable<KeyValuePair<string, object>>)x.Args()[1];
                foreach (var item in value)
                {
                    lstResult.Add($"rec topic:{topic} key:{item.Key} value:{item.Value} time:{DateTime.Now.TimeOfDay}");
                }
            });

            var periodicalChangePusher = new Core.PeriodicalChangePusher(intervalDataProvider, initialDataProvider);
            periodicalChangePusher.Register(listener, BidAskTopic);
            periodicalChangePusher.Register(listener, TradeTopic);
            periodicalChangePusher.Register(listener, DetailTopic);
            Assert.AreEqual(initialBidAsk, periodicalChangePusher.Load(BidAskTopic, "1"));
            Assert.AreEqual(initialDetail, periodicalChangePusher.Load(DetailTopic, "3"));
            Assert.AreEqual(initialTrade, periodicalChangePusher.Load(TradeTopic, "2"));

            periodicalChangePusher.Save(BidAskTopic, "1", "test10");
            Assert.AreEqual("test10", periodicalChangePusher.Load(BidAskTopic, "1"));
            periodicalChangePusher.Save(TradeTopic, "2", "test20");
            Assert.AreEqual("test20", periodicalChangePusher.Load(TradeTopic, "2"));
            periodicalChangePusher.Save(DetailTopic, "3", "test30");
            Assert.AreEqual("test30", periodicalChangePusher.Load(DetailTopic, "3"));
            lstResult.Add($"start time:{DateTime.Now.TimeOfDay}");
            Task.Delay(1200).Wait();
            periodicalChangePusher.Save(BidAskTopic, "1", "test11");
            periodicalChangePusher.Save(TradeTopic, "2", "test21");
            Assert.AreEqual("test21", periodicalChangePusher.Load(TradeTopic, "2"));
            Task.Delay(500).Wait();
            periodicalChangePusher.Save(BidAskTopic, "1", "test12");
            Assert.AreEqual("test12", periodicalChangePusher.Load(BidAskTopic, "1"));
            periodicalChangePusher.Save(TradeTopic, "2", "test22");
            periodicalChangePusher.Save(DetailTopic, "3", "test31");
            Task.Delay(2000).Wait();
            periodicalChangePusher.Save(DetailTopic, "3", "test32");
            Assert.AreEqual("test32", periodicalChangePusher.Load(DetailTopic, "3"));


            Task.Delay(5000).Wait();
            Assert.AreEqual(7, lstResult.Count);
            Assert.IsTrue(lstResult.Any(i => i.StartsWith("rec topic:bidAsk key:1 value:test10 ")));
            Assert.IsTrue(lstResult.Any(i => i.StartsWith("rec topic:detail key:3 value:test30 ")));
            Assert.IsTrue(lstResult.Any(i => i.StartsWith("rec topic:detail key:3 value:test32 ")));
            Assert.IsTrue(lstResult.Count(i => i.StartsWith("rec topic:detail key:3")) == 3);
            Assert.IsTrue(lstResult.Count(i => i.StartsWith("rec topic:bidAsk key:1")) == 2);
            Assert.IsTrue(lstResult.Count(i => i.StartsWith("rec topic:trade key:2 ")) == 1);

        }

        [TestMethod]
        public void LoadTest()
        {
            var intervalDataProvider = Substitute.For<IIntervalDataProvider>();
            var initialDataProvider = Substitute.For<IInitialDataProvider>();
            var initialBidAsk = "initial BidAsk";
            initialDataProvider.Provide(BidAskTopic, Arg.Any<string>()).Returns(initialBidAsk);
            intervalDataProvider.GetInterval(BidAskTopic).Returns(1000);

            var lstValue = new Queue<KeyValuePair<string, string>>();

            for (int j = 1; j < 201; j++)
            {
                for (int i = 1; i < 10001; i++)
                {
                    lstValue.Enqueue(new KeyValuePair<string, string>($"InsIdIR0005D{i.ToString().PadLeft(5,'0')}", $"{CreateBidAsk(j)}"));
                }
            }
            var lstResult = new List<string>();
            var lstLoads = new List<object>();
            var listener = Substitute.For<IPushSubscriber>();
            listener.When(x => x.OnPush(Arg.Any<string>(), Arg.Any<IReadOnlyList<KeyValuePair<string, object>>>())).Do(x =>
            {
                var topic = (string)x.Args()[0];
                var value = (IEnumerable<KeyValuePair<string, object>>)x.Args()[1];
                foreach (var item in value)
                {
                    lstResult.Add($"rec topic:{topic} key:{item.Key} value:{item.Value} time:{DateTime.Now.TimeOfDay}");
                }
            });
            var stopwatch = new Stopwatch();
            var periodicalChangePusher = new Core.PeriodicalChangePusher(intervalDataProvider, initialDataProvider);
            periodicalChangePusher.Register(listener, BidAskTopic);
            var rnd = new Random();
            var counter = 0;
            stopwatch.Start();
            while (lstValue.Count > 0)
            {
                var keyValuePair = lstValue.Dequeue();
                periodicalChangePusher.Save(BidAskTopic, keyValuePair.Key, keyValuePair.Value);
                counter++;
                if (counter % 79 == 0)
                {
                    lstLoads.Add(periodicalChangePusher.Load(BidAskTopic, $"InsIdIR0005D{rnd.Next(10000)}"));
                }
            }
            stopwatch.Stop();
            Task.Delay(1500).Wait();
            var ress1 = lstResult.Skip(1).Take(10000).OrderBy(i => i).ToList();
            var ress2= lstResult.Skip(10001).Take(10000).OrderBy(i => i).ToList();
        }

        private string CreateBidAsk(int index)
        {
            var str=$"{{\"AskCount\":{index},\"AskPrice\":{index},\"AskVolume\":{index},\"BidAskDateTime\":null,\"BidCount\":{index},\"BidPrice\":{index},\"BidVolume\":{index}}}";
            var lstdata= new List<string>();
            for (int j = 0; j < 5; j++)
            {
                lstdata.Add(str);
            }
            return $"[{string.Join(",",lstdata)}]";
        }
    }
}
