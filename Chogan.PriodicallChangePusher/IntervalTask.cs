using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PeriodicalChangePusher.Core
{
    public class IntervalTask
    {
        private readonly int interval;
        private readonly ConcurrentDictionary<string, KeyValuePair<long, object>> keyValues;
        private readonly Dictionary<string, long> processedValues = new Dictionary<string, long>();
        private readonly string topic;
        private List<IPushSubscriber> receivers;

        public IntervalTask(string topic, int interval,
            ConcurrentDictionary<string, KeyValuePair<long, object>> keyValues)
        {
            this.interval = interval;
            this.keyValues = keyValues;
            this.topic = topic;
        }

        public void SetListener(List<IPushSubscriber> setReceivers)
        {
            receivers = setReceivers;
        }

        public void Start()
        {
            Task.Factory.StartNew(TaskAction, TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent);
        }

        private void TaskAction()
        {
            while (true)
            {
                Task.Delay(interval).Wait();
                var lstValues = new List<KeyValuePair<string, object>>();
                foreach (var item in keyValues.ToArray())
                {
                    if (processedValues.ContainsKey(item.Key) && processedValues[item.Key] >= item.Value.Key)
                        continue;
                    lstValues.Add(new KeyValuePair<string, object>(item.Key, item.Value.Value));
                    processedValues[item.Key] = item.Value.Key;
                }

                foreach (var pushReceiver in receivers) pushReceiver.OnPush(topic, lstValues);
            }
        }
    }
}