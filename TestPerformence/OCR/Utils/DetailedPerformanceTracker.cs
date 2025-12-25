using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    /// <summary>
    /// Tracker hiệu năng chi tiết: đo CPU, Memory và Time cho từng stage
    /// </summary>
    public class DetailedPerformanceTracker : IDisposable
    {
        private readonly Process _currentProcess;
        private readonly Stopwatch _totalStopwatch;
        private readonly List<StageMetrics> _stages;
        private Stopwatch _currentStageStopwatch;
        private string _currentStageName;
        private TimeSpan _lastCpuTime;
        private DateTime _lastCheckTime;
        private long _startMemory;

        public DetailedPerformanceTracker()
        {
            _currentProcess = Process.GetCurrentProcess();
            _totalStopwatch = Stopwatch.StartNew();
            _stages = new List<StageMetrics>();
            _lastCpuTime = _currentProcess.TotalProcessorTime;
            _lastCheckTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Bắt đầu đo một stage mới
        /// </summary>
        public void StartStage(string stageName)
        {
            EndCurrentStage();
            _currentStageName = stageName;
            _currentStageStopwatch = Stopwatch.StartNew();
            _lastCpuTime = _currentProcess.TotalProcessorTime;
            _lastCheckTime = DateTime.UtcNow;
            _startMemory = GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Kết thúc stage hiện tại và lưu metrics
        /// </summary>
        public void EndCurrentStage()
        {
            if (_currentStageStopwatch != null && _currentStageStopwatch.IsRunning)
            {
                _currentStageStopwatch.Stop();

                // Đo CPU usage
                _currentProcess.Refresh();
                var endCpuTime = _currentProcess.TotalProcessorTime;
                var endTime = DateTime.UtcNow;
                var endMemory = GC.GetTotalMemory(false);

                var cpuUsed = (endCpuTime - _lastCpuTime).TotalMilliseconds;
                var timeElapsed = (endTime - _lastCheckTime).TotalMilliseconds;
                var cpuPercent = timeElapsed > 0 
                    ? (cpuUsed / (Environment.ProcessorCount * timeElapsed)) * 100 
                    : 0;
                var memoryUsedMB = (endMemory - _startMemory) / (1024.0 * 1024.0);

                var metrics = new StageMetrics
                {
                    Name = _currentStageName,
                    ElapsedMs = _currentStageStopwatch.ElapsedMilliseconds,
                    CpuPercent = cpuPercent,
                    MemoryMB = memoryUsedMB
                };

                _stages.Add(metrics);

                Debug.WriteLine($"[⏱ {_currentStageName}] " +
                    $"Time: {metrics.ElapsedMs}ms | " +
                    $"CPU: {metrics.CpuPercent:F1}% | " +
                    $"Memory: {metrics.MemoryMB:+0.0;-0.0;+0.0}MB");
            }
        }

        /// <summary>
        /// Lấy summary dạng text
        /// </summary>
        public string GetSummary()
        {
            EndCurrentStage();
            _totalStopwatch.Stop();

            var sb = new StringBuilder();
            sb.AppendLine("========== PERFORMANCE SUMMARY ==========");
            sb.AppendLine($"Total Time: {_totalStopwatch.ElapsedMilliseconds}ms");
            sb.AppendLine($"Total Stages: {_stages.Count}");
            sb.AppendLine();

            // Tìm stage có CPU cao nhất
            var maxCpuStage = _stages.OrderByDescending(s => s.CpuPercent).FirstOrDefault();

            sb.AppendLine("┌────────────────────────────────┬────────┬─────────┬──────────┬─────────┐");
            sb.AppendLine("│ Stage                          │ Time   │ Time %  │ CPU %    │ Memory  │");
            sb.AppendLine("├────────────────────────────────┼────────┼─────────┼──────────┼─────────┤");

            foreach (var stage in _stages)
            {
                var timePercent = (_totalStopwatch.ElapsedMilliseconds > 0)
                    ? (double)stage.ElapsedMs / _totalStopwatch.ElapsedMilliseconds * 100
                    : 0;

                // Đánh dấu stage có CPU cao nhất
                var marker = (stage == maxCpuStage) ? "🔥" : "  ";

                sb.AppendLine($"│ {marker}{stage.Name,-28} │ {stage.ElapsedMs,5}ms │ {timePercent,5:F1}% │ {stage.CpuPercent,6:F1}% │ {stage.MemoryMB,+6:F1;-6:F1;+6:F1}MB │");
            }

            sb.AppendLine("└────────────────────────────────┴────────┴─────────┴──────────┴─────────┘");
            sb.AppendLine();

            if (maxCpuStage != null)
            {
                sb.AppendLine($"🔥 HIGHEST CPU USAGE: {maxCpuStage.Name} ({maxCpuStage.CpuPercent:F1}%)");
            }

            sb.AppendLine("=========================================");

            return sb.ToString();
        }

        /// <summary>
        /// Lấy stage có CPU usage cao nhất
        /// </summary>
        public StageMetrics GetHighestCpuStage()
        {
            return _stages.OrderByDescending(s => s.CpuPercent).FirstOrDefault();
        }

        /// <summary>
        /// Hiển thị summary trong Debug Output
        /// </summary>
        public void PrintSummary()
        {
            var summary = GetSummary();
            Debug.WriteLine(summary);
        }

        /// <summary>
        /// Lấy tất cả stage metrics (cho PerformanceAccumulator)
        /// </summary>
        public List<StageMetrics> GetAllStages()
        {
            EndCurrentStage();
            return _stages;
        }

        public void Dispose()
        {
            // Auto cleanup khi dispose
            EndCurrentStage();
        }

        /// <summary>
        /// Metrics cho một stage
        /// </summary>
        public class StageMetrics
        {
            public string Name { get; set; }
            public long ElapsedMs { get; set; }
            public double CpuPercent { get; set; }
            public double MemoryMB { get; set; }

            public override string ToString()
            {
                return $"{Name}: {ElapsedMs}ms, CPU {CpuPercent:F1}%, Memory {MemoryMB:+0.0;-0.0;+0.0}MB";
            }
        }
    }
}
