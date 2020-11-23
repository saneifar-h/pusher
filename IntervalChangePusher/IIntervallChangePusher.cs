namespace IntervalChangePusherLib
{
    public interface IIntervalChangePusher
    {
        void Put(string topic, string key, object value);
        void Subscribe(IPushSubscriber subscriber, string topic);
        void UnSubscribe(IPushSubscriber subscriber, string topic);
        void StopPush(string topic);
        void StartPush(string topic);
        object Fetch(string topic, string key);
    }
}