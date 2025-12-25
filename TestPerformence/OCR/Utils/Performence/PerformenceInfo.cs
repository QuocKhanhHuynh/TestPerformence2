using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPerformence.OCR.Utils.Performence
{
    public class PerformenceInfo
    {
        public string StageName { get; set; }
        public double ElapsedTimeMs { get; set; }
        public double CpuUsagePercent { get; set; }
        public double MemoryDeltaMB { get; set; }
    }
}
