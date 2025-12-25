using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    /// <summary>
    /// Tích lũy performance metrics qua nhiều lần chạy để có kết quả trung bình chính xác
    /// </summary>
    public class PerformanceAccumulator
    {
        private readonly int _warmupRuns;
        private readonly int _sampleRuns;
        private int _currentRun;
        private readonly Dictionary<string, List<StageData>> _stageMetrics;

        public PerformanceAccumulator(int warmupRuns = 3, int sampleRuns = 20)
        {
            _warmupRuns = warmupRuns;
            _sampleRuns = sampleRuns;
            _currentRun = 0;
            _stageMetrics = new Dictionary<string, List<StageData>>();
        }

        /// <summary>
        /// Thêm một lần chạy vào accumulator
        /// </summary>
        /// <returns>True nếu đã đủ samples để hiển thị summary</returns>
        public bool AddRun(DetailedPerformanceTracker tracker)
        {
            _currentRun++;

            // Bỏ qua warm-up runs
            if (_currentRun <= _warmupRuns)
            {
                System.Diagnostics.Debug.WriteLine($"[PERF ACCUMULATOR] Warm-up run {_currentRun}/{_warmupRuns} (skipped)");
                return false;
            }

            // Thu thập metrics
            var stages = tracker.GetAllStages();
            foreach (var stage in stages)
            {
                if (!_stageMetrics.ContainsKey(stage.Name))
                {
                    _stageMetrics[stage.Name] = new List<StageData>();
                }
                _stageMetrics[stage.Name].Add(new StageData
                {
                    Time = stage.ElapsedMs,
                    Cpu = stage.CpuPercent,
                    Memory = stage.MemoryMB
                });
            }

            int validRuns = _currentRun - _warmupRuns;
            System.Diagnostics.Debug.WriteLine($"[PERF ACCUMULATOR] Sample run {validRuns}/{_sampleRuns}");

            // Kiểm tra đã đủ samples chưa
            if (validRuns >= _sampleRuns)
            {
                return true; // Đã đủ, cần hiển thị summary
            }

            return false;
        }

        /// <summary>
        /// Lấy summary dạng text với statistics
        /// </summary>
        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== PERFORMANCE SUMMARY (20 SAMPLES) ==========");
            sb.AppendLine($"Warm-up runs: {_warmupRuns}");
            sb.AppendLine($"Valid samples: {_currentRun - _warmupRuns}");
            sb.AppendLine();

            sb.AppendLine("┌────────────────────────────────┬──────────────────────┬──────────────────────┬──────────────────────┐");
            sb.AppendLine("│ Stage                          │ Time (ms)            │ CPU (%)              │ Memory (MB)          │");
            sb.AppendLine("├────────────────────────────────┼──────────────────────┼──────────────────────┼──────────────────────┤");

            double maxAvgCpu = 0;
            string maxCpuStage = "";

            foreach (var kvp in _stageMetrics.OrderBy(x => x.Key))
            {
                var stageName = kvp.Key;
                var data = kvp.Value;

                // Tính statistics
                var avgTime = data.Average(d => d.Time);
                var minTime = data.Min(d => d.Time);
                var maxTime = data.Max(d => d.Time);
                var stdTime = CalculateStdDev(data.Select(d => (double)d.Time).ToList());

                var avgCpu = data.Average(d => d.Cpu);
                var minCpu = data.Min(d => d.Cpu);
                var maxCpu = data.Max(d => d.Cpu);
                var stdCpu = CalculateStdDev(data.Select(d => d.Cpu).ToList());

                var avgMem = data.Average(d => d.Memory);
                var minMem = data.Min(d => d.Memory);
                var maxMem = data.Max(d => d.Memory);

                // Tracking max CPU
                if (avgCpu > maxAvgCpu)
                {
                    maxAvgCpu = avgCpu;
                    maxCpuStage = stageName;
                }

                // Đánh dấu stage có CPU cao nhất
                var marker = "";
                if (avgCpu == maxAvgCpu && kvp.Key == maxCpuStage)
                {
                    marker = "🔥";
                }
                else
                {
                    marker = "  ";
                }

                sb.AppendLine($"│ {marker}{stageName,-28} │ {avgTime,5:F1} ±{stdTime,4:F1} ({minTime,3:F0}-{maxTime,3:F0}) │ {avgCpu,5:F1} ±{stdCpu,4:F1} ({minCpu,3:F0}-{maxCpu,3:F0}) │ {avgMem,+5:F1;-5:F1;+5:F1} ({minMem,+4:F1;-4:F1;+4:F1}-{maxMem,+4:F1;-4:F1;+4:F1}) │");
            }

            sb.AppendLine("└────────────────────────────────┴──────────────────────┴──────────────────────┴──────────────────────┘");
            sb.AppendLine();
            sb.AppendLine("Format: Avg ±StdDev (Min-Max)");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(maxCpuStage))
            {
                sb.AppendLine($"🔥 HIGHEST AVERAGE CPU USAGE: {maxCpuStage} ({maxAvgCpu:F1}%)");
                sb.AppendLine($"   → This stage is the bottleneck. Consider optimizing it first.");
            }

            sb.AppendLine();
            sb.AppendLine("======================================================");

            return sb.ToString();
        }

        /// <summary>
        /// Lấy stage có CPU usage cao nhất (theo trung bình)
        /// </summary>
        public (string Name, double AvgCpu) GetHighestCpuStage()
        {
            if (_stageMetrics.Count == 0)
                return (null, 0);

            var maxStage = _stageMetrics
                .Select(kvp => new { Name = kvp.Key, AvgCpu = kvp.Value.Average(d => d.Cpu) })
                .OrderByDescending(x => x.AvgCpu)
                .FirstOrDefault();

            return (maxStage?.Name, maxStage?.AvgCpu ?? 0);
        }

        /// <summary>
        /// Reset accumulator để bắt đầu chu kỳ mới
        /// </summary>
        public void Reset()
        {
            _currentRun = 0;
            _stageMetrics.Clear();
            System.Diagnostics.Debug.WriteLine("[PERF ACCUMULATOR] Reset - Starting new cycle");
        }

        /// <summary>
        /// Tính độ lệch chuẩn (standard deviation)
        /// </summary>
        private double CalculateStdDev(List<double> values)
        {
            if (values.Count <= 1)
                return 0;

            double avg = values.Average();
            double sumOfSquares = values.Sum(val => (val - avg) * (val - avg));
            return Math.Sqrt(sumOfSquares / values.Count);
        }

        /// <summary>
        /// Export metrics ra CSV file
        /// </summary>
        public void ExportToCsv(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                
                // Header
                sb.AppendLine("Stage,Run,Time_ms,CPU_percent,Memory_MB");

                // Data
                foreach (var kvp in _stageMetrics.OrderBy(x => x.Key))
                {
                    var stageName = kvp.Key;
                    var data = kvp.Value;

                    for (int i = 0; i < data.Count; i++)
                    {
                        sb.AppendLine($"{stageName},{i + 1},{data[i].Time:F2},{data[i].Cpu:F2},{data[i].Memory:F2}");
                    }
                }

                System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"[PERF ACCUMULATOR] Exported to CSV: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PERF ACCUMULATOR] Export CSV failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Data structure cho mỗi lần đo
        /// </summary>
        private class StageData
        {
            public long Time { get; set; }
            public double Cpu { get; set; }
            public double Memory { get; set; }
        }
    }
}
