using demo_ocr_label;
using GarmentGridApp.Presentation.OCR.Utils;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using PaddleOCRSharp;
using Sdcb.RotationDetector;
using System.Diagnostics;
using System.IO;
using ZXing;
using ZXing.Common;

namespace TestPerformence
{
    public partial class Form1 : Form
    {
        private Yolo11Seg? yolo11Seg;
        private Yolo11SegOpenVINO? _yoloDetector;
        private PaddleOCREngine? _ocrEngine;
        private PaddleRotationDetector? _rotationDetector;
        private ZXing.Windows.Compatibility.BarcodeReader _readerZxing;
        private Mat? _currentFrame;
        private bool _isInitialized = false;
        private bool _useOpenVINO = false;

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



            Debug.WriteLine("[INIT] Khởi tạo YOLO seg...");
            yolo11Seg = InitializeYoloDeg();
            if (yolo11Seg != null)
            {
                Debug.WriteLine("[INIT] ✓ yolo11Seg detector khởi tạo thành công");
            }
            else
            {
                Debug.WriteLine("[INIT] ⚠ yolo11Seg detector không khởi tạo được");
            }

            Debug.WriteLine("[INIT] Khởi tạo PaddleOCR engine...");
            _ocrEngine = InitializeOCREngine();
            if (_ocrEngine != null)
            {
                Debug.WriteLine("[INIT] ✓ PaddleOCR engine khởi tạo thành công");
            }
            else
            {
                Debug.WriteLine("[INIT] ⚠ PaddleOCR engine không khởi tạo được");
            }

            Debug.WriteLine("[INIT] Khởi tạo Rotation Detector...");
            _rotationDetector = InitializeRotationDetector();
            if (_rotationDetector != null)
            {
                Debug.WriteLine("[INIT] ✓ Rotation Detector khởi tạo thành công");
            }
            else
            {
                Debug.WriteLine("[INIT] ⚠ Rotation Detector không khởi tạo được");
            }
            _readerZxing = new ZXing.Windows.Compatibility.BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    TryHarder = true,
                }
            };



            _isInitialized = true;
            Debug.WriteLine("[INIT] ✓ Tất cả models đã sẵn sàng");
        }


        private PaddleOCREngine? InitializeOCREngine()
        {
            try
            {
                // Khởi tạo với config từ Config.cs
                var config = new Config();
                var modelParams = config.modelParams;

                var ocrParams = new OCRParameter
                {
                    det = modelParams.det,
                    cls = modelParams.cls,
                    use_angle_cls = modelParams.use_angle_cls,
                    rec = modelParams.rec,
                    det_db_thresh = modelParams.det_db_thresh,
                    det_db_box_thresh = modelParams.det_db_box_thresh,
                    cls_thresh = modelParams.cls_thresh,
                    enable_mkldnn = modelParams.enable_mkldnn,
                    cpu_math_library_num_threads = modelParams.cpu_math_library_num_threads,
                    det_db_score_mode = modelParams.det_db_score_mode
                };

                // Thử khởi tạo với OCRParameter, để PaddleOCRSharp tự tìm model (inferencePath = null)
                try
                {
                    var engine = new PaddleOCREngine(null, ocrParams);
                    return engine;
                }
                catch (DllNotFoundException)
                {
                    Debug.WriteLine("[OCR] DllNotFoundException - Thử default constructor...");
                    // Fallback: thử với constructor không tham số
                    return new PaddleOCREngine();
                }
            }
            catch (DllNotFoundException dllEx)
            {
                Debug.WriteLine($"[OCR] PaddleOCR native DLL not found: {dllEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OCR] Failed to initialize OCR engine: {ex.Message}");
                return null;
            }
        }

        private PaddleRotationDetector? InitializeRotationDetector()
        {
            try
            {
                return new PaddleRotationDetector(RotationDetectionModel.EmbeddedDefault);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ROTATION] Failed to initialize: {ex.Message}");
                return null;
            }
        }

        private Yolo11Seg? InitializeYoloDeg()
        {
            try
            {
                // Cấu hình đường dẫn model và class names
                string modelPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "yolo11n.onnx");
                string[] classNames = new[] { "label" };

                if (!System.IO.File.Exists(modelPath))
                {
                    Debug.WriteLine($"[⚠] YOLO model not found: {modelPath}");
                    Debug.WriteLine("[ℹ] YOLO detector will not be available");
                    return null;
                }

                var detector = new Yolo11Seg(
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

        private Yolo11SegOpenVINO? InitializeYoloDetector()
        {
            try
            {
                // Cấu hình đường dẫn model và class names
                string modelPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "yolo11n.xml");
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
                    yolo11Seg: yolo11Seg,
                    yoloDetector: _useOpenVINO ? _yoloDetector : null,
                    ocr: _ocrEngine,
                    rotationDetector: _rotationDetector,
                    zxingReader: _readerZxing,
                    currentThreshold: 0, // Không dùng trong V2
                    cameraBox: picOriginal,
                    processImage: picProcessed
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
        /// Hiển thị thống kê thời gian xử lý trung bình
        /// </summary>
        private void btnConfig_Click(object sender, EventArgs e)
        {
            using (var configForm = new FormBatchConfig(_useOpenVINO))
            {
                if (configForm.ShowDialog(this) == DialogResult.OK)
                {
                    _useOpenVINO = configForm.UseOpenVINO;
                }
            }
        }

        private void btnStatistics_Click(object sender, EventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Models chưa được khởi tạo.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Chọn thư mục chứa ảnh để chạy thống kê";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;

                    string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                                 s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                                                 s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) || 
                                                 s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                     .ToArray();

                    if (files.Length == 0)
                    {
                        MessageBox.Show("Không tìm thấy file ảnh nào trong thư mục đã chọn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Reset dữ liệu thống kê trước khi chạy
                    DetectLabelFromImageV2.ResetTimes();

                    Cursor.Current = Cursors.WaitCursor;

                    foreach (var file in files)
                    {
                        try
                        {
                            using (Mat mat = Cv2.ImRead(file, ImreadModes.Color))
                            {
                                if (mat != null && !mat.Empty())
                                {
                                    DetectLabelFromImageV2.DetectLabel(
                                        workSessionId: 1,
                                        frame: mat,
                                        yolo11Seg: yolo11Seg,
                                        yoloDetector: _useOpenVINO ? _yoloDetector : null,
                                        ocr: _ocrEngine,
                                        rotationDetector: _rotationDetector,
                                        zxingReader: _readerZxing,
                                        currentThreshold: 0,
                                        cameraBox: picOriginal,
                                        processImage: picProcessed,
                                        isStatistic: true,
                                        fileName: Path.GetFileName(file)
                                    );
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[BATCH ERROR] Error processing {file}: {ex.Message}");
                        }
                    }

                    Cursor.Current = Cursors.Default;

                    // Hiện Form thông kê
                    using (FormStats statsForm = new FormStats())
                    {
                        statsForm.ShowDialog(this);
                    }
                }
            }
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
