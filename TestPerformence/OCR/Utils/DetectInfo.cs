using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectQRCode.Models.Camera
{
    public class DetectInfo
    {
        public string? QRCode { get; set; }
        public string? Size { get; set; }
        public string? ProductTotal { get; set; }
        public string? Color { get; set; }
        public string? ProductCode { get; set; }

        public override string ToString()
        {
            return $"QRCode: {QRCode}\n" +
                   $"Size: {Size}\n" +
                   $"Total: {ProductTotal}\n" +
                   $"Color: {Color}\n" +
                   $"Product Code: {ProductCode}";
        }
    }

    
}
