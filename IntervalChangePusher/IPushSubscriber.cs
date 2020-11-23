using System.Collections.Generic;

namespace IntervalChangePusherLib
{
    public interface IPushSubscriber
    {
        void OnPush(string topic, IReadOnlyList<KeyValuePair<string, object>> changeValues);
        void Initialize();
    }
}