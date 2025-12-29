using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPerformence.OCR.Utils
{
    public class OpenVinoSetting
    {
        public int NumThreads { get; set; }
        public int NumStreams { get; set; }
        public string PerformanceHint { get; set; }
        public bool EnableHyperThreading { get; set; }
        public bool EnableCpuPinning { get; set; }
        public string DeviceName { get; set; }
        public bool EnableProfiling { get; set; }
        public int InferPrecision { get; set; }
        public string InferencePriority { get; set; }
    }
}
