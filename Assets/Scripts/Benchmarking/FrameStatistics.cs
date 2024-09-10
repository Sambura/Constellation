using System.Collections.Generic;
using System.Linq;
using static Core.Algorithm;

public class FrameStatistics
{
    public int TotalFrames { get; private set; }
    public float TotalDuration { get; private set; }
    public float AverageFPS { get; private set; }
    public float AverageFrameTime { get; private set; }
    public float FrameDurationStd { get; private set; }
    public float OneLowTime { get; private set; }
    public float PointOneLowTime { get; private set; }
    public float LongestFrameTime { get; private set; }

    public void SetFrameTimings(List<float> frameTimings)
    {
        TotalFrames = frameTimings.Count;
        TotalDuration = frameTimings.Sum();
        AverageFrameTime = TotalDuration / TotalFrames;
        AverageFPS = 1 / AverageFrameTime;
        FrameDurationStd = CalculateStandardDeviation(frameTimings, AverageFrameTime);

        List<float> copy = new List<float>(frameTimings);
        copy.Sort();
        copy.Reverse();
        OneLowTime = copy[copy.Count / 100];
        PointOneLowTime = copy[copy.Count / 1000];
        LongestFrameTime = copy[0];
    }
}