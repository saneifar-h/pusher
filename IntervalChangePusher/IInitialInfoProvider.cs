namespace IntervalChangePusherLib
{
    public interface IInitialInfoProvider
    {
        object Provide(string topic, string key);
    }
}