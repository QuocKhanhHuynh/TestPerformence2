using GarmentGridApp.Presentation.OCR.Utils;
using System;
using System.Linq;
using System.Windows.Forms;

namespace TestPerformence
{
    public partial class FormStats : Form
    {
        public FormStats()
        {
            InitializeComponent();
            LoadStatistics();
            
            // Đăng ký sự kiện click/thay đổi để copy
            lstOCRFailFiles.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            lstQRFailFiles.SelectedIndexChanged += ListBox_SelectedIndexChanged;
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem != null)
            {
                string fileName = lb.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(fileName))
                {
                    Clipboard.SetText(fileName);
                    // Thông báo nhẹ ở status bar hoặc tooltip nếu cần (tạm thời dùng Debug)
                    System.Diagnostics.Debug.WriteLine($"Copied: {fileName}");
                }
            }
        }

        private void LoadStatistics()
        {
            // Tính toán trung bình
            double avgSuccess = DetectLabelFromImageV2.SuccessTimes.Count > 0 ? DetectLabelFromImageV2.SuccessTimes.Average() : 0;
            double avgOCRFail = DetectLabelFromImageV2.CannotExtractOCRTimes.Count > 0 ? DetectLabelFromImageV2.CannotExtractOCRTimes.Average() : 0;
            double avgQRFail = DetectLabelFromImageV2.CannotExtractQRTimes.Count > 0 ? DetectLabelFromImageV2.CannotExtractQRTimes.Average() : 0;

            // Hiển thị labels
            lblSuccessAvg.Text = $"Thời gian TB thành công: {avgSuccess:F2} ms ({DetectLabelFromImageV2.SuccessTimes.Count} files)";
            lblOCRFailAvg.Text = $"Thời gian TB lỗi OCR: {avgOCRFail:F2} ms ({DetectLabelFromImageV2.CannotExtractOCRTimes.Count} files)";
            lblQRFailAvg.Text = $"Thời gian TB lỗi QR: {avgQRFail:F2} ms ({DetectLabelFromImageV2.CannotExtractQRTimes.Count} files)";

            // Hiển thị danh sách file lỗi
            lstOCRFailFiles.Items.Clear();
            foreach (var file in DetectLabelFromImageV2.CannotExtractOCRDetails)
            {
                lstOCRFailFiles.Items.Add(file);
            }

            lstQRFailFiles.Items.Clear();
            foreach (var file in DetectLabelFromImageV2.CannotExtractQRDetails)
            {
                lstQRFailFiles.Items.Add(file);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
