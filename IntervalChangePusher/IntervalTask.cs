using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IntervalChangePusherLib
{
    public class IntervalTask
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly int interval;
        private readonly ConcurrentDictionary<string, KeyValuePair<long, object>> keyValues;
        private readonly Dictionary<string, long> processedValues = new Dictionary<string, long>();
        private readonly string topic;
        private static readonly object SyncObject = new object();
        private List<IPushSubscriber> receivers;
        private Task task;

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
            if (task != null && !task.IsCanceled && !task.IsCompleted && !task.IsFaulted) return;
            lock (SyncObject)
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
                try
                {
                    Task.Delay(interval, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException ex)
                {
                    break;
                }
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
            }
        }
    }
}