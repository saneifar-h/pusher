using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IntervalChangePusherLib
{
    public class IntervalTask
    {
        private static readonly object syncObject = new object();
        private readonly ConcurrentDictionary<string, KeyValuePair<long, object>> keyValues;
        public readonly int NumberOfIntervalUnit;
        private readonly Dictionary<string, long> processedValues = new Dictionary<string, long>();
        private readonly string topic;
        private CancellationTokenSource cancellationTokenSource;
        private readonly AutoResetEvent pushToSubscribeResetEvent = new AutoResetEvent(true);
        private List<IPushSubscriber> receivers;
        private Task task;
        private DateTime? lastPushTime;

        public IntervalTask(string topic, int numberOfIntervalUnit,
            ConcurrentDictionary<string, KeyValuePair<long, object>> keyValues)
        {
            NumberOfIntervalUnit = numberOfIntervalUnit;
            this.keyValues = keyValues;
            this.topic = topic;
        }

        public DateTime LastPushTime
        {
            get => lastPushTime ?? DateTime.Now;
            private set => lastPushTime = value;
        }

        public void SetListener(List<IPushSubscriber> setReceivers)
        {
            receivers = setReceivers;
        }

        public void Push()
        {
            pushToSubscribeResetEvent.Set();
        }

        public void Start()
        {
            if (task != null && !task.IsCanceled && !task.IsCompleted && !task.IsFaulted) return;
            lock (syncObject)
            {
                if (task != null && !task.IsCanceled && !task.IsCompleted && !task.IsFaulted)
                    return;
                cancellationTokenSource = new CancellationTokenSource();
                task = Task.Factory.StartNew(TaskAction, cancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
        }

        private void TaskAction()
        {
            while (true)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;
                var lstValues = new List<KeyValuePair<string, object>>();
                foreach (var item in keyValues.ToArray())
                {
                    if (processedValues.ContainsKey(item.Key) && processedValues[item.Key] >= item.Value.Key)
                        continue;
                    lstValues.Add(new KeyValuePair<string, object>(item.Key, item.Value.Value));
                    processedValues[item.Key] = item.Value.Key;
                }

                foreach (var pushReceiver in receivers) pushReceiver.OnPush(topic, lstValues);
                LastPushTime = DateTime.Now;
                pushToSubscribeResetEvent.WaitOne();

                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }
        }
    }
}