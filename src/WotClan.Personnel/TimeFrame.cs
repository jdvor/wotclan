namespace WotClan.Personnel;

public enum TimeFrame
{
    AllAvailable = 0,
    Days28,
    Days7,
    Day,
}

internal static class TimeFrameExtensions
{
    internal static string ToHttp(this TimeFrame tf)
    {
        return tf switch
        {
            TimeFrame.Days28 => "28",
            TimeFrame.Days7 => "7",
            TimeFrame.Day => "1",
            _ => "all",
        };
    }
}
