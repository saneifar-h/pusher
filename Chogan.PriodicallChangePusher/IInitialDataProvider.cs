namespace PeriodicalChangePusher.Core
{
    public interface IInitialDataProvider
    {
        object Provide(string topic, string key);
    }
}