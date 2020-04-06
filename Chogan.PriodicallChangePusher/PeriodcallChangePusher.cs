using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PeriodicalChangePusher.Core
{
    public class PeriodicalChangePusher : IPeriodicalChangePusher
    {
        private readonly IIntervalDataProvider intervalDataProvider;

        private readonly Dictionary<string, List<IPushSubscriber>> listenerDic =
            new Dictionary<string, List<IPushSubscriber>>();

        private readonly Dictionary<string, IntervalTask> taskDic =
            new Dictionary<string, IntervalTask>();

        private readonly Dictionary<string, ConcurrentDictionary<string, KeyValuePair<long, string>>> topicDictionaries
            = new Dictionary<string, ConcurrentDictionary<string, KeyValuePair<long, string>>>();

        public PeriodicalChangePusher(IIntervalDataProvider intervalDataProvider)
        {
            this.intervalDataProvider = intervalDataProvider;
        }

        public void Save(string topic, string key, string value)
        {
            if (!topicDictionaries.ContainsKey(topic))
            {
                var topicRelatedDic = new ConcurrentDictionary<string, KeyValuePair<long, string>>();
                topicDictionaries.Add(topic, topicRelatedDic);
                var intervalTask = new IntervalTask(topic, intervalDataProvider.GetInterval(topic), topicRelatedDic);
                taskDic.Add(topic, intervalTask);
                intervalTask.SetListener(listenerDic[topic]);
                intervalTask.Start();
            }

            var sequence = DateTime.Now.Ticks;
            var keyvalue = new KeyValuePair<long, string>(sequence, value);
            topicDictionaries[topic].AddOrUpdate(key, keyvalue, (k, v) => keyvalue);
        }

        public void Register(IPushSubscriber pushReciever, string topic)
        {
            if (!listenerDic.ContainsKey(topic))
                listenerDic.Add(topic, new List<IPushSubscriber>());
            var pushRecievers = listenerDic[topic];
            pushRecievers.Add(pushReciever);
            if (taskDic.ContainsKey(topic))
                taskDic[topic].SetListener(pushRecievers);
            pushReciever.Initialize();
        }

        public void UnRegister(IPushSubscriber pushReciever, string topic)
        {
            if (!listenerDic.ContainsKey(topic))
                return;
            var pushRecievers = listenerDic[topic];
            pushRecievers.Remove(pushReciever);
            taskDic[topic].SetListener(pushRecievers);
        }
    }
}