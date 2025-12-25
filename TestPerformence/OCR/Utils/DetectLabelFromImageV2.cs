using demo_ocr_label;
using DetectQRCode.Models.Camera;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestPerformence.OCR.Utils.Performence;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    /// <summary>
    /// Version 2: Sử dụng YOLO11 Detector để phát hiện QR và Label
    /// </summary>
    public static class DetectLabelFromImageV2
    {
        /// <summary>
        /// Detect label sử dụng YOLO11 detector
        /// </summary>
        /// <param name="frame">Mat frame từ camera</param>
        /// <param name="yoloDetector">YOLO11 detector instance (được tạo từ Form1)</param>
        /// <param name="ocr">PaddleOCR engine</param>
        /// <param name="currentThreshold">Threshold hiện tại (không dùng trong version này)</param>
        /// <param name="cameraBox">PictureBox để hiển thị camera</param>
        /// <param name="picPreprocessed">PictureBox để hiển thị preprocessed image</param>
        /// <returns>DetectInfo chứa thông tin QR và label</returns>
        /// 

        private static int counter = 0;
        private static bool isSaving = false;
        private static Stopwatch sw = Stopwatch.StartNew();
        private static Stopwatch swEstinate = Stopwatch.StartNew();
        public static DetectInfo DetectLabel(
            int workSessionId,
            Mat frame,
            Yolo11SegOpenVINO yoloDetector,
          
            PictureBox cameraBox,
            bool isDebugOcr
            )
        {
            try
            {
                var result = new DetectInfo();
               
                Mat originMat = null;
                Mat croptYoloMat = null;
                Mat rotationMat = null;
                Mat preProcessImageMat = null;
                Mat croptMergeMat = null;

                var EstimatePerformence = new EstimatePerformence();
                if (frame == null || frame.Empty())
                {
                    Debug.WriteLine("[⚠] Frame is null or empty");
                    return null;
                }

                Mat compressed = new Mat();

                EstimatePerformence.StartPerformence("Preprocessing");
                var encodeParams = new[]
                {
                    new ImageEncodingParam(ImwriteFlags.JpegQuality, 100)
                };

                // Encode (nén)
                Cv2.ImEncode(".jpg", frame, out byte[] jpegData, encodeParams);

                // Decode lại thành Mat
                compressed = Cv2.ImDecode(jpegData, ImreadModes.Color);
                frame.Dispose();
                frame = compressed; //*****************************************************************************************************************************************************************
                originMat = frame.Clone();

                    
                EstimatePerformence.EndPerformence();
                EstimatePerformence.StartPerformence("Yolo Detection");
                // ============================================
                // 1. YOLO DETECTION - Detect trực tiếp trên frame (Mat)
                // ============================================
                var detections = yoloDetector.Detect(frame);

               

                // ============================================
                // 2. VẼ BOUNDING BOXES LÊN FRAME
                // ============================================
                using var displayFrame = frame.Clone();

                DetectionResult labelDetection = null;

                foreach (var detection in detections)
                {
                    var bbox = detection.BoundingBox;

                    // Vẽ bounding box
                    Cv2.Rectangle(displayFrame, bbox, Scalar.Yellow, 2);

                    // Vẽ label text
                    string label = $"{detection.ClassName}: {detection.Confidence:P0}";
                    Cv2.PutText(displayFrame, label,
                        new OpenCvSharp.Point(bbox.X, bbox.Y - 5),
                        HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 2);


                    if (detection.ClassName.ToLower().Contains("label"))
                    {
                        labelDetection = detection;
                    }
                }

                

                var displayBmp = MatToBitmap(displayFrame);
                cameraBox.BeginInvoke(new Action(() =>
                {
                    var old = cameraBox.Image;
                    cameraBox.Image = displayBmp;
                    old?.Dispose();
                }));


                originMat?.Dispose();
                croptYoloMat?.Dispose();
                rotationMat?.Dispose();
                preProcessImageMat?.Dispose();
                croptMergeMat?.Dispose();


                //ShowPerformence(EstimatePerformence);


                if (isSaving && isDebugOcr)
                {
                    var totalMs = sw.ElapsedMilliseconds;
                    var estinateMs = swEstinate.ElapsedMilliseconds;
                    if (totalMs >= 1000 && estinateMs <= 300000)
                    {
                        if (result.ProductTotal != null && result.ProductCode != null)
                        {
                            SaveImageTemp(0, workSessionId.ToString(), frame);
                        }
                        else
                        {
                            SaveImageTemp(1, workSessionId.ToString(), frame);
                        }
                        sw.Restart();
                    }
                }
                


                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[⚠ DETECT LABEL V2 ERROR] {ex.Message}");
                Debug.WriteLine($"[⚠ Stack Trace] {ex.StackTrace}");
                return null;
            }
        }



        public static void ShowPerformence(EstimatePerformence estimatePerformence)
        {
            var content = estimatePerformence.ShowPerformence();
            MessageBox.Show(content, "Performance Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        

        /// <summary>
        /// Convert Mat to Bitmap
        /// </summary>
        private static Bitmap MatToBitmap(Mat mat)
        {
            int w = mat.Width;
            int h = mat.Height;
            int channels = mat.Channels();

            Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var rect = new Rectangle(0, 0, w, h);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

            int stride = bmpData.Stride;
            int rowLength = w * channels;

            byte[] buffer = new byte[rowLength];

            for (int y = 0; y < h; y++)
            {
                IntPtr src = mat.Data + y * (int)mat.Step();
                System.Runtime.InteropServices.Marshal.Copy(src, buffer, 0, rowLength);

                IntPtr dst = bmpData.Scan0 + y * stride;
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, dst, rowLength);
            }

            bmp.UnlockBits(bmpData);
            return bmp;
        }

        /// <summary>
        /// Áp dụng Unsharp Mask để làm sắc nét ảnh
        /// </summary>
        /// <param name="src">Mat nguồn</param>
        /// <param name="amount">Cường độ sharpening (1.0 = 100%, 1.5 = 150%, khuyến nghị: 1.0-2.0)</param>
        /// <param name="radius">Bán kính Gaussian blur (pixels, khuyến nghị: 1-3)</param>
        /// <param name="threshold">Ngưỡng (0-255, 0 = không ngưỡng, khuyến nghị: 0-10)</param>
        /// <returns>Mat đã được sharpened (cần dispose sau khi dùng)</returns>
        private static Mat ApplyUnsharpMask(Mat src, double amount = 1.5, int radius = 2, int threshold = 0)
        {
            // 1. Tạo bản mờ của ảnh gốc (Gaussian Blur)
            var blurred = new Mat();
            int ksize = radius * 2 + 1; // Kernel size phải là số lẻ
            Cv2.GaussianBlur(src, blurred, new OpenCvSharp.Size(ksize, ksize), 0);

            // 2. Tính "mask" = original - blurred
            var mask = new Mat();
            Cv2.Subtract(src, blurred, mask);

            // 3. Nếu có threshold, chỉ sharpen vùng có độ tương phản cao
            if (threshold > 0)
            {
                var maskAbs = new Mat();
                Cv2.ConvertScaleAbs(mask, maskAbs);

                var thresholdMask = new Mat();
                Cv2.Threshold(maskAbs, thresholdMask, threshold, 255, ThresholdTypes.Binary);

                Cv2.BitwiseAnd(mask, mask, mask, thresholdMask);

                maskAbs.Dispose();
                thresholdMask.Dispose();
            }

            // 4. Nhân mask với amount
            var weightedMask = new Mat();
            Cv2.ConvertScaleAbs(mask, weightedMask, amount, 0);

            // 5. Cộng vào ảnh gốc: sharpened = original + (amount * mask)
            var sharpened = new Mat();
            Cv2.Add(src, weightedMask, sharpened);

            // Cleanup
            blurred.Dispose();
            mask.Dispose();
            weightedMask.Dispose();

            return sharpened;
        }

        private static void SaveImageWithStep(int type, string workSessionId, int? stepFail, DetectInfo? info, Mat? originMat, Mat? croptYoloMat, Mat? rotationMat, Mat? preProcessImageMat, Mat? croptMergeMat)
        {
            if ((type == 0 || type == 1) && originMat != null && croptYoloMat != null && rotationMat != null && preProcessImageMat != null && croptMergeMat != null)
            {
                SaveImageWithName(type, workSessionId, 1, originMat);
                SaveImageWithName(type, workSessionId, 2, croptYoloMat);
                SaveImageWithName(type, workSessionId, 3, rotationMat);
                SaveImageWithName(type, workSessionId, 4, preProcessImageMat);
                SaveImageWithName(type, workSessionId, 5, croptMergeMat);
                SaveTextWithName(workSessionId, info);
            }
            else
            {
                if (stepFail.HasValue)
                {
                    if (stepFail.Value == 1 && originMat != null)
                    {
                        SaveImageWithName(type, workSessionId, 1, originMat);
                    }
                    else if (stepFail.Value == 2 && originMat != null && croptYoloMat != null)
                    {
                        SaveImageWithName(type, workSessionId, 1, originMat);
                        SaveImageWithName(type, workSessionId, 2, croptYoloMat);
                    }
                    else if (stepFail.Value == 3 && originMat != null && croptYoloMat != null && rotationMat != null)
                    {
                        SaveImageWithName(type, workSessionId, 1, originMat);
                        SaveImageWithName(type, workSessionId, 2, croptYoloMat);
                        SaveImageWithName(type, workSessionId, 3, rotationMat);
                    }
                    else if (stepFail.Value == 4 && originMat != null && croptYoloMat != null && rotationMat != null && preProcessImageMat != null)
                    {
                        SaveImageWithName(type, workSessionId, 1, originMat);
                        SaveImageWithName(type, workSessionId, 2, croptYoloMat);
                        SaveImageWithName(type, workSessionId, 3, rotationMat);
                        SaveImageWithName(type, workSessionId, 4, preProcessImageMat);
                    }
                    else if (stepFail.Value == 5 && originMat != null && croptYoloMat != null && rotationMat != null && preProcessImageMat != null && croptMergeMat != null)
                    {
                        SaveImageWithName(type, workSessionId, 1, originMat);
                        SaveImageWithName(type, workSessionId, 2, croptYoloMat);
                        SaveImageWithName(type, workSessionId, 3, rotationMat);
                        SaveImageWithName(type, workSessionId, 4, preProcessImageMat);
                        SaveImageWithName(type, workSessionId, 5, croptMergeMat);
                    }
                }
            }
        }

        private static void SaveImageTemp(int type, string workSessionId, Mat mat)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Tạo đường dẫn thư mục (không bao gồm tên file)
            var folderPath = Path.Combine(baseDir, "OCR", type == 0 ? "Success" : "Failed", workSessionId);

            // Tạo tên file cụ thể
            var fileName = $"{DateTime.Now:HHmmss}.jpg";
            var fullPath = Path.Combine(folderPath, fileName);

            // Gọi hàm lưu
            SaveOcrImage(fullPath, mat);
        }







        private static void SaveTextWithName(string workSessionId, DetectInfo info)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Tạo đường dẫn thư mục (không bao gồm tên file)
            var folderPath = Path.Combine(baseDir, "OCR", "Success", workSessionId, counter.ToString());

            // Tạo tên file cụ thể
            var fileName = $"info_{DateTime.Now:HHmmss}.txt";
            var fullPath = Path.Combine(folderPath, fileName);

            // Gọi hàm lưu
            SaveOcrText(fullPath, info.ToString());
        }

        private static void SaveOcrText(string fullFilePath, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(fullFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Lưu nội dung văn bản với mã hóa UTF-8 để hỗ trợ tiếng Việt/ký tự đặc biệt
                File.WriteAllText(fullFilePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveOcrText] Failed: {ex.Message}");
            }
        }


        private static void SaveImageWithName(int type, string workSessionId, int step, Mat mat)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Tạo đường dẫn thư mục (không bao gồm tên file)
            var folderPath = Path.Combine(baseDir, "OCR", type == 0 ? "Success" : type == 1 ? "PostProcessImageFailure" : "ProcessImageFailure", workSessionId, counter.ToString());

            // Tạo tên file cụ thể
            var fileName = $"{ConvertStepToString(step)}_{DateTime.Now:HHmmss}.jpg";
            var fullPath = Path.Combine(folderPath, fileName);

            // Gọi hàm lưu
            SaveOcrImage(fullPath, mat);
        }

        private static string ConvertStepToString(int step)
        {
            return step switch
            {
                1 => "Original",
                2 => "Cropped_YOLO",
                3 => "Rotated",
                4 => "Preprocessed",
                5 => "Cropped_Merged",
                _ => "Unknown_Step"
            };
        }

        private static void SaveOcrImage(string fullFilePath, Mat mat)
        {
            try
            {
                // Lấy đường dẫn thư mục từ đường dẫn file toàn vẹn
                var directory = Path.GetDirectoryName(fullFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Thiết lập chất lượng JPG (50% để tiết kiệm bộ nhớ)
                var jpegParams = new ImageEncodingParam[] {
                    new ImageEncodingParam(ImwriteFlags.JpegQuality, 50)
                };

                Cv2.ImWrite(fullFilePath, mat, jpegParams);
            }
            catch (Exception ex)
            {
                // Debug.WriteLine giúp bạn thấy lỗi trong cửa sổ Output của Visual Studio
                Debug.WriteLine($"[SaveOcrImage] Failed: {ex.Message}");
            }
        }

    }
}
