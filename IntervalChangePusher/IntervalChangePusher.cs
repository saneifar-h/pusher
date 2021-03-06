﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IntervalChangePusherLib
{
    public class IntervalChangePusher : IIntervalChangePusher
    {
        private readonly IInitialInfoProvider initialDataProvider;
        private readonly IIntervalProvider intervalDataProvider;

        private readonly Dictionary<string, List<IPushSubscriber>> listenerDic =
            new Dictionary<string, List<IPushSubscriber>>();

        private readonly Dictionary<string, IntervalTask> taskDic =
            new Dictionary<string, IntervalTask>();

        private readonly Dictionary<string, ConcurrentDictionary<string, KeyValuePair<long, object>>> topicDictionaries
            = new Dictionary<string, ConcurrentDictionary<string, KeyValuePair<long, object>>>();

        public IntervalChangePusher(IIntervalProvider intervalDataProvider,
            IInitialInfoProvider initialDataProvider)
        {
            this.intervalDataProvider = intervalDataProvider;
            this.initialDataProvider = initialDataProvider;
        }

        public void Put(string topic, string key, object value)
        {
            if (!topicDictionaries.ContainsKey(topic))
            {
                var topicRelatedDic = new ConcurrentDictionary<string, KeyValuePair<long, object>>();
                topicDictionaries.Add(topic, topicRelatedDic);
                var intervalTask = new IntervalTask(topic, intervalDataProvider.GetInterval(topic), topicRelatedDic);
                taskDic.Add(topic, intervalTask);
                if (listenerDic.ContainsKey(topic))
                    intervalTask.SetListener(listenerDic[topic]);
                intervalTask.Start();
            }

            var sequence = DateTime.Now.Ticks;
            var keyValue = new KeyValuePair<long, object>(sequence, value);
            topicDictionaries[topic].AddOrUpdate(key, keyValue, (k, v) => keyValue);
        }

        public void Subscribe(IPushSubscriber pushSubscriber, string topic)
        {
            if (!listenerDic.ContainsKey(topic))
                listenerDic.Add(topic, new List<IPushSubscriber>());
            var pushSubscribers = listenerDic[topic];
            pushSubscribers.Add(pushSubscriber);
            if (taskDic.ContainsKey(topic))
                taskDic[topic].SetListener(pushSubscribers);
            pushSubscriber.Initialize();
        }

        public void UnSubscribe(IPushSubscriber pushSubscriber, string topic)
        {
            if (!listenerDic.ContainsKey(topic))
                return;
            var pushSubscribers = listenerDic[topic];
            pushSubscribers.Remove(pushSubscriber);
            taskDic[topic].SetListener(pushSubscribers);
        }

        public void StopPush(string topic)
        {
            if (taskDic.ContainsKey(topic))
                taskDic[topic].Stop();
        }

        public void StartPush(string topic)
        {
            if (taskDic.ContainsKey(topic))
                taskDic[topic].Start();
        }

        public object Fetch(string topic, string key)
        {
            topicDictionaries.TryGetValue(topic, out var foundDic);
            if (foundDic == null)
            {
                var initialData = initialDataProvider.Provide(topic, key);
                Put(topic, key, initialData);
                return initialData;
            }

            foundDic.TryGetValue(key, out var foundKeyValuePair);
            if (foundKeyValuePair.Key == 0 && foundKeyValuePair.Value == null)
            {
                var initialData = initialDataProvider.Provide(topic, key);
                Put(topic, key, initialData);
                return initialData;
            }

            return foundKeyValuePair.Value;
        }
    }
}