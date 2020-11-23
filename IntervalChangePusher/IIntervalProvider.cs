namespace IntervalChangePusherLib
{
    public interface IIntervalProvider
    {
        int GetInterval(string topic);
    }
}