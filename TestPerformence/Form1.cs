using demo_ocr_label;
using GarmentGridApp.Presentation.OCR.Utils;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace TestPerformence
{
    public partial class Form1 : Form
    {
        private Yolo11SegOpenVINO? _yoloDetector;
        private Mat? _currentFrame;
        private bool _isInitialized = false;

        public Form1()
        {
            InitializeComponent();
            InitializeModels();
        }

        /// <summary>
        /// Khởi tạo YOLO detector và OCR engine
        /// </summary>
        private void InitializeModels()
        {
            Debug.WriteLine("[INIT] Khởi tạo YOLO detector...");
            _yoloDetector = InitializeYoloDetector();
            if (_yoloDetector != null)
            {
                Debug.WriteLine("[INIT] ✓ YOLO detector khởi tạo thành công");
            }
            else
            {
                Debug.WriteLine("[INIT] ⚠ YOLO detector không khởi tạo được");
            }

           

            _isInitialized = true;
            Debug.WriteLine("[INIT] ✓ Tất cả models đã sẵn sàng");
        }

        private Yolo11SegOpenVINO? InitializeYoloDetector()
        {
            try
            {
                // Cấu hình đường dẫn model và class names
                string modelPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "yolo11n-seg-version-1-0-0.xml");
                string[] classNames = new[] { "label" };

                if (!System.IO.File.Exists(modelPath))
                {
                    Debug.WriteLine($"[⚠] YOLO model not found: {modelPath}");
                    Debug.WriteLine("[ℹ] YOLO detector will not be available");
                    return null;
                }

                var detector = new Yolo11SegOpenVINO(
                    modelPath,
                    classNames,
                    confThreshold: 0.5f,
                    iouThreshold: 0.45f
                );

                return detector;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[⚠] Failed to initialize YOLO: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Xử lý sự kiện click nút chọn ảnh
        /// </summary>
        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Models chưa được khởi tạo thành công. Vui lòng kiểm tra đường dẫn models.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*",
                Title = "Chọn ảnh label"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Dispose frame cũ nếu có
                    _currentFrame?.Dispose();

                    // Load ảnh bằng OpenCV
                    _currentFrame = Cv2.ImRead(openFileDialog.FileName, ImreadModes.Color);

                    if (_currentFrame == null || _currentFrame.Empty())
                    {
                        MessageBox.Show("Không thể load ảnh. Vui lòng thử ảnh khác.",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Debug.WriteLine($"[IMAGE] Loaded: {openFileDialog.FileName}");
                    Debug.WriteLine($"[IMAGE] Size: {_currentFrame.Width}x{_currentFrame.Height}");

                    // Xử lý detection
                    ProcessDetection();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi load ảnh:\n{ex.Message}",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine($"[IMAGE ERROR] {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Thực hiện detection và hiển thị kết quả
        /// </summary>
        private void ProcessDetection()
        {
          

            try
            {
                //InitializeModels();
                picOriginal.Image = BitmapConverter.ToBitmap(_currentFrame);
                Debug.WriteLine("[PROCESS] Bắt đầu detection...");

                // Gọi hàm DetectLabel từ DetectLabelFromImageV2
                var result = DetectLabelFromImageV2.DetectLabel(
                    workSessionId: 1,
                    frame: _currentFrame,
                    yoloDetector: _yoloDetector,
                    cameraBox: picOriginal,
                    isDebugOcr: true
                );

                // Hiển thị kết quả
                if (result != null)
                {
                    txtQRCode.Text = result.QRCode ?? "";
                    txtProductTotal.Text = result.ProductTotal ?? "";
                    txtProductCode.Text = result.ProductCode ?? "";
                    txtSize.Text = result.Size ?? "";
                    txtColor.Text = result.Color ?? "";

                    Debug.WriteLine("[PROCESS] ✓ Detection hoàn thành");
                    Debug.WriteLine($"  QR Code: {result.QRCode}");
                    Debug.WriteLine($"  Product Total: {result.ProductTotal}");
                    Debug.WriteLine($"  Product Code: {result.ProductCode}");
                    Debug.WriteLine($"  Size: {result.Size}");
                    Debug.WriteLine($"  Color: {result.Color}");
                }
                else
                {
                    ClearResults();
                    Debug.WriteLine("[PROCESS] ⚠ Detection trả về null");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý detection:\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"[PROCESS ERROR] {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Xóa các kết quả hiển thị
        /// </summary>
        private void ClearResults()
        {
            txtQRCode.Text = "";
            txtProductTotal.Text = "";
            txtProductCode.Text = "";
            txtSize.Text = "";
            txtColor.Text = "";
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _currentFrame?.Dispose();
                _yoloDetector?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
