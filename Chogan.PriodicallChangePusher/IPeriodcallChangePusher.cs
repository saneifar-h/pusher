﻿namespace PeriodicalChangePusher.Core
{
    public interface IPeriodicalChangePusher
    {
        void Save(string topic, string key, object value);
        void Register(IPushSubscriber subscriber, string topic);
        void UnRegister(IPushSubscriber subscriber, string topic);
    }
}