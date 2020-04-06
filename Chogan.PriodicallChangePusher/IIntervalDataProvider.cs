namespace PeriodicalChangePusher.Core
{
    public interface IIntervalDataProvider
    {
        int GetInterval(string topic);
    }
}