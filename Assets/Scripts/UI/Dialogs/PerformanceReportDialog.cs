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
        [SerializeField] private Button _prevPageButton;
        [SerializeField] private Button _nextPageButton;
        [SerializeField] private NumericInputField _pageInputField;
        [SerializeField] private TextMeshProUGUI _benchmarkNameLabel;
        [SerializeField] private TextMeshProUGUI _minPageIndexLabel;
        [SerializeField] private TextMeshProUGUI _maxPageIndexLabel;

        private int _selectedIndex;

        protected float[] XTickSpacings = { 1, 2, 5, 10, 15, 25, 50, 100, 150, 200, 250 };
        protected float[] YTickSpacings = { 0.001f, 0.002f, 0.005f, 0.01f, 0.025f };
        protected float[] FPSLineLevels = { 0.1f, 0.25f, 0.5f, 1, 2, 5, 10, 15, 20, 30, 60, 120, 240, 360 };

        public Action<PerformanceReportDialog, BenchmarkResult> OnExportSummaryClick { get; set; }
        public Action<PerformanceReportDialog, BenchmarkResult> OnExportReportClick { get; set; }
        public Action<PerformanceReportDialog, BenchmarkResult> OnExportTimingsClick { get; set; }

        public List<BenchmarkResult> Results { get; private set; } = new List<BenchmarkResult>();
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetSelectedIndex(value);
        }
        public BenchmarkResult SelectedResult => SelectedIndex < Results.Count ? Results[SelectedIndex] : null;
        public FrameStatistics SelectedFrameStatistics { get; private set; } = new FrameStatistics();

        private void ExportTimingsButtonClicked() => OnExportTimingsClick?.Invoke(this, SelectedResult);

        private void ExportSummaryButtonClicked() => OnExportSummaryClick?.Invoke(this, SelectedResult);

        private void ExportReportButtonClicked() => OnExportReportClick?.Invoke(this, SelectedResult);

        private void SetSelectedIndex(int index)
        {
            if (_selectedIndex == index) return;
            _selectedIndex = index;

            SelectedFrameStatistics.SetFrameTimings(SelectedResult.Timings);
            UpdateBasicStatsLabel();
            _benchmarkNameLabel.text = SelectedResult.BenchmarkConfig.Name;

            _timingsPlot.MinYLimit = 0;
            _timingsPlot.SetData(SelectedResult.Timings);
            _timingsPlot.DisplayPlot(XTickSpacings, YTickSpacings);
            _pageInputField.IntValue = SelectedIndex + 1;
        }

        public void UpdateReport(List<BenchmarkResult> results)
        {
            _selectedIndex = -1;
            Results = results;
            _pageInputField.MaxValue = Results.Count;
            SelectedIndex = Results.Count - 1;
            _minPageIndexLabel.text = "1";
            _maxPageIndexLabel.text = $"{Results.Count}";
        }

        private void UpdateBasicStatsLabel()
        {
            FrameStatistics FS = SelectedFrameStatistics;
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
            _prevPageButton.Click += PrevPageButtonClicked;
            _nextPageButton.Click += NextPageButtonClicked;
            _pageInputField.IntValueChanged += InputFieldPageChanged;
        }

        private void InputFieldPageChanged(int value) => SelectedIndex = value - 1;

        private void NextPageButtonClicked() => SelectedIndex = Math.Min(Results.Count - 1, SelectedIndex + 1);

        private void PrevPageButtonClicked() => SelectedIndex = Math.Max(0, SelectedIndex - 1);

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _exportSummaryButton.Click -= ExportSummaryButtonClicked;
            _exportReportButton.Click -= ExportReportButtonClicked;
            _exportTimingsButton.Click -= ExportTimingsButtonClicked;
        }
    }
}