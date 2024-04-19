using Teltonika.AVL.Trackers;

namespace Teltonika.AVL;

public static class TeltonikaTrackers
{
    private static TeltonikaTracker? @default;

    private static FM1100? fm1100;
    private static FM2200? fm2200;
    private static FM3200? fm3200;
    private static FM3400? fm3400;
    private static TFT100? tft100;

    public static FM1100 FM1100 => fm1100 ??= new FM1100();

    public static FM2200 FM2200 => fm2200 ??= new FM2200();

    public static FM3200 FM3200 => fm3200 ??= new FM3200();

    public static FM3400 FM3400 => fm3400 ??= new FM3400();

    public static TFT100 TFT100 => tft100 ??= new TFT100();

    public static TeltonikaTracker Default => @default ??= new TeltonikaTracker();

    public static TeltonikaTracker FromType(TrackerType trackerType)
    {
        return trackerType switch
        {
            TrackerType.FM1100 => FM1100,
            TrackerType.FM2200 => FM2200,
            TrackerType.FM3200 => FM3200,
            TrackerType.FM3400 => FM3400,
            TrackerType.TFT100 => TFT100,
            _ => Default
        };
    }
}