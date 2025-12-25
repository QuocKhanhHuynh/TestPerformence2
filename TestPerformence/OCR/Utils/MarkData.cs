using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    public class MaskData
    {
        public string ClassName { get; set; }
        public double Confidence { get; set; }
        public (int tlx, int tly, int brx, int bry) BBox { get; set; }
        public List<(int x, int y)> Marks { get; set; }

        public MaskData()
        {
            Marks = new List<(int x, int y)>();
        }
    }
}
