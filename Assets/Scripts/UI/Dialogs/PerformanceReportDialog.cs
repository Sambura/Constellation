using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

namespace ConstellationUI
{
    public class PerformanceReportDialog : MonoDialog
    {
        [Header("Objects")]
        [SerializeField] private TextMeshProUGUI _basicStatsLabel;
        [SerializeField] private LinePlot _timingsPlot;
        [SerializeField] private Button _exportSummaryButton;
        [SerializeField] private Button _exportReportButton;
        [SerializeField] private Button _exportTimingsButton;

        protected float[] XTickSpacings = { 1, 2, 5, 10, 15, 25, 50, 100, 150, 200, 250 };
        protected float[] YTickSpacings = { 0.001f, 0.002f, 0.005f, 0.01f, 0.025f };
        protected float[] FPSLineLevels = { 0.1f, 0.25f, 0.5f, 1, 2, 5, 10, 15, 20, 30, 60, 120, 240, 360 };

        public Action<PerformanceReportDialog> OnExportSummaryClick { get; set; }
        public Action<PerformanceReportDialog> OnExportReportClick { get; set; }
        public Action<PerformanceReportDialog> OnExportTimingsClick { get; set; }

        public FrameStatistics FrameStatistics { get; private set; } = new FrameStatistics();
        public List<float> FrameTimings { get; private set; }

        private void ExportTimingsButtonClicked() => OnExportTimingsClick?.Invoke(this);

        private void ExportSummaryButtonClicked() => OnExportSummaryClick?.Invoke(this);

        private void ExportReportButtonClicked() => OnExportReportClick?.Invoke(this);

        public void UpdateReport(List<float> frameTimings)
        {
            FrameTimings = frameTimings;
            FrameStatistics.SetFrameTimings(FrameTimings);
            UpdateBasicStatsLabel();

            _timingsPlot.MinYLimit = 0;
            _timingsPlot.SetData(FrameTimings);
            _timingsPlot.DisplayPlot(XTickSpacings, YTickSpacings);
        }

        private void UpdateBasicStatsLabel()
        {
            FrameStatistics FS = FrameStatistics;
            _basicStatsLabel.text =	$"{FS.TotalFrames} frames\n" +
                                    $"{FS.TotalDuration:0.00} s\n" +
                                    $"{FS.AverageFrameTime * 1000:0.000} ms\n" +
                                    $"{FS.FrameDurationStd}\n" +
                                    $"{FS.AverageFPS:0.00} FPS\n" +
                                    $"{FS.OneLowTime * 1000:0.000} ms, {1 / FS.OneLowTime: 0.00} FPS\n" +
                                    $"{FS.PointOneLowTime * 1000:0.000} ms, {1 / FS.PointOneLowTime: 0.00} FPS\n" +
                                    $"{FS.LongestFrameTime * 1000:0.000} ms, {1 / FS.LongestFrameTime: 0.00} FPS";
        }

        protected virtual void Start()
        {
            _exportSummaryButton.Click += ExportSummaryButtonClicked;
            _exportReportButton.Click += ExportReportButtonClicked;
            _exportTimingsButton.Click += ExportTimingsButtonClicked;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _exportSummaryButton.Click -= ExportSummaryButtonClicked;
            _exportReportButton.Click -= ExportReportButtonClicked;
            _exportTimingsButton.Click -= ExportTimingsButtonClicked;
        }
    }
}