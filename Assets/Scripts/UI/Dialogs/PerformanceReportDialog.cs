using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using static Core.Algorithm;

namespace ConstellationUI
{
    public class PerformanceReportDialog : MonoDialog
    {
        [Header("Objects")]
        [SerializeField] private TextMeshProUGUI _basicStatsLabel;
        [SerializeField] private LinePlot _timingsPlot;

        protected float[] XTickSpacings = { 1, 2, 5, 10, 15, 25, 50, 100, 150, 200, 250 };
        protected float[] YTickSpacings = { 0.001f, 0.002f, 0.005f, 0.01f, 0.025f };
        protected float[] FPSLineLevels = { 0.1f, 0.25f, 0.5f, 1, 2, 5, 10, 15, 20, 30, 60, 120, 240, 360 };

        public int TotalFrames { get; private set; }
        public float TotalDuration { get; private set; }
        public float AverageFPS { get; private set; }
        public float AverageFrameTime { get; private set; }
        public float FrameDurationStd { get; private set; }
        public float OneLowTime { get; private set; }
        public float PointOneLowTime { get; private set; }
        public float LongestFrameTime { get; private set; }
        public List<float> FrameTimings { get; private set; }

        private void RecalculateStats()
        {
            TotalFrames = FrameTimings.Count;
            TotalDuration = FrameTimings.Sum();
            AverageFrameTime = TotalDuration / TotalFrames;
            AverageFPS = 1 / AverageFrameTime;
            FrameDurationStd = CalculateStandardDeviation(FrameTimings, AverageFrameTime);

            List<float> copy = new List<float>(FrameTimings);
            copy.Sort();
            copy.Reverse();
            OneLowTime = copy[copy.Count / 100];
            PointOneLowTime = copy[copy.Count / 1000];
            LongestFrameTime = copy[0];
        }

        public void UpdateReport(List<float> frameTimings)
        {
            FrameTimings = frameTimings;
            RecalculateStats();
            UpdateBasicStatsLabel();

            _timingsPlot.MinYLimit = 0;
            _timingsPlot.SetData(FrameTimings);
            _timingsPlot.DisplayPlot(XTickSpacings, YTickSpacings);
        }

        private void UpdateBasicStatsLabel()
        {
            _basicStatsLabel.text =	$"{TotalFrames} frames\n" +
                                    $"{TotalDuration:0.00} s\n" +
                                    $"{AverageFrameTime * 1000:0.000} ms\n" +
                                    $"{FrameDurationStd}\n" +
                                    $"{AverageFPS:0.00} FPS\n" +
                                    $"{OneLowTime * 1000:0.000} ms, {1 / OneLowTime: 0.00} FPS\n" +
                                    $"{PointOneLowTime * 1000:0.000} ms, {1 / PointOneLowTime: 0.00} FPS\n" +
                                    $"{LongestFrameTime * 1000:0.000} ms, {1 / LongestFrameTime: 0.00} FPS";
        }
    }
}