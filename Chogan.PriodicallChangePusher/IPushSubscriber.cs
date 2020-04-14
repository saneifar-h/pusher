using System.Collections.Generic;

namespace PeriodicalChangePusher.Core
{
    public interface IPushSubscriber
    {
        void OnPush(string topic, IReadOnlyList<KeyValuePair<string, object>> changeValues);
        void Initialize();
    }
}