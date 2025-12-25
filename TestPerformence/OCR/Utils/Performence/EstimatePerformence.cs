using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TestPerformence.OCR.Utils.Performence
{

    public class EstimatePerformence
    {
        private readonly Process _process;
        private readonly Stopwatch _stopwatch;
        private string _stageName;

        private TimeSpan _startCPUTime;
        private long _startMemoryBytes;

        public List<PerformenceInfo> PerInfo { get; }

        public EstimatePerformence()
        {
            _process = Process.GetCurrentProcess();
            _stopwatch = new Stopwatch();
            PerInfo = new List<PerformenceInfo>();
        }

        public void StartPerformence(string stageName)
        {
            _stageName = stageName;

            // Ép buộc dọn rác nếu bạn muốn đo bộ nhớ thuần túy của stage (tùy chọn)
            // GC.Collect();
            // GC.WaitForPendingFinalizers();

            _process.Refresh();
            _startCPUTime = _process.TotalProcessorTime;
            _startMemoryBytes = _process.WorkingSet64;

            _stopwatch.Restart(); // Reset và bắt đầu đo thời gian
        }

        public void EndPerformence()
        {
            if (string.IsNullOrEmpty(_stageName) || !_stopwatch.IsRunning)
                return;

            var endCPUTime = _process.TotalProcessorTime;
            var endMemoryBytes = _process.WorkingSet64;

            _stopwatch.Stop();
            _process.Refresh();

            

            // 1. Thời gian thực tế (Wall-clock time) dùng Stopwatch cực kỳ chính xác
            double actualElapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

            // 2. Thời gian CPU tiêu thụ (trên tất cả các nhân)
            double cpuUsedMs = (endCPUTime - _startCPUTime).TotalMilliseconds;

            // 3. Tính % CPU (theo phong cách Task Manager: 0-100%)
            // Công thức: (Thời gian CPU dùng / (Thời gian thực tế * Số nhân)) * 100
            double totalPotentialMs = actualElapsedMs * Environment.ProcessorCount;
            double cpuUsagePercent = totalPotentialMs > 0 ? (cpuUsedMs / totalPotentialMs) * 100.0 : 0;

            // Giới hạn max 100% để tránh sai số nhỏ của hệ điều hành
            if (cpuUsagePercent > 100) cpuUsagePercent = 100;

            PerInfo.Add(new PerformenceInfo
            {
                StageName = _stageName,
                ElapsedTimeMs = actualElapsedMs,
                CpuUsagePercent = cpuUsagePercent,
                MemoryDeltaMB = (endMemoryBytes - _startMemoryBytes) / (1024.0 * 1024.0)
            });

            _stageName = null;
        }

        public string ShowPerformence()
        {
            var sb = new StringBuilder();
            sb.AppendLine("━━━━━━━━━━━━━━━ Performance Summary ━━━━━━━━━━━━━━━");
            sb.AppendLine(string.Format("{0,-25} {1,12} {2,10} {3,12}",
                "Stage", "Time (ms)", "CPU (%)", "Mem Delta (MB)"));
            sb.AppendLine("─────────────────────────────────────────────────────────");

            double totalTime = 0;
            double totalMemDelta = 0;

            foreach (var info in PerInfo)
            {
                totalTime += info.ElapsedTimeMs;
                totalMemDelta += info.MemoryDeltaMB;

                sb.AppendLine(string.Format("{0,-25} {1,12:F2} {2,10:F2} {3,12:F2}",
                    info.StageName,
                    info.ElapsedTimeMs,
                    info.CpuUsagePercent,
                    info.MemoryDeltaMB));
            }

            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine(string.Format("{0,-25} {1,12:F2} {2,10} {3,12:F2}",
                "TOTAL ELAPSED",
                totalTime,
                "",
                totalMemDelta));

            return sb.ToString();
        }
    }
}