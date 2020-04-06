namespace PeriodicalChangePusher.Redis
{
    public interface IRedisConnectionProvider
    {
        string Provide();
    }
}