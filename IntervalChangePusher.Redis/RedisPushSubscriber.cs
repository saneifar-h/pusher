using System.Collections.Generic;
using System.Linq;
using IntervalChangePusherLib;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace IntervalChangePusherRedis
{
    public class RedisPushSubscriber : IPushSubscriber
    {
        private readonly IRedisConnectionProvider redisConnectionProvider;
        private ConnectionMultiplexer redisConnection;

        public RedisPushSubscriber(IRedisConnectionProvider redisConnectionProvider)
        {
            this.redisConnectionProvider = redisConnectionProvider;
        }

        public async void OnPush(string topic, IReadOnlyList<KeyValuePair<string, object>> changeValues)
        {
            var arr = new KeyValuePair<RedisKey, RedisValue>[changeValues.Count];
            for (var i = 0; i < changeValues.Count; i++)
            {
                var item = changeValues.ElementAt(i);
                arr[i] = new KeyValuePair<RedisKey, RedisValue>(new RedisKey(item.Key),
                    new RedisValue(JsonConvert.SerializeObject(item.Value)));
            }

            await redisConnection.GetDatabase().StringSetAsync(arr);
        }

        public void Initialize()
        {
            redisConnection = ConnectionMultiplexer.Connect(redisConnectionProvider.Provide());
        }
    }
}