using demo_ocr_label;
using DetectQRCode.Models.Camera;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Text;
using PaddleOCRSharp;
using Sdcb.RotationDetector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
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
        public static List<double> SuccessTimes = new List<double>();
        public static List<double> CannotExtractOCRTimes = new List<double>();
        public static List<string> CannotExtractOCRDetails = new List<string>();
        public static List<double> CannotExtractQRTimes = new List<double>();
        public static List<string> CannotExtractQRDetails = new List<string>();


        public static void ResetTimes()
        {
            SuccessTimes.Clear();
            CannotExtractOCRTimes.Clear();
            CannotExtractQRTimes.Clear();
            CannotExtractQRDetails.Clear();
            CannotExtractOCRDetails.Clear();
        }
        public static DetectInfo DetectLabel(
            int workSessionId,
            Mat frame,
            Yolo11Seg yolo11Seg,
            PaddleOCREngine ocr,
             PaddleRotationDetector rotationDetector,
             WeChatQRCode weChatQRCode,
             ZXing.Windows.Compatibility.BarcodeReader zxingReader,
            int currentThreshold,
            PictureBox cameraBox,
            PictureBox processImage,
            Yolo11SegOpenVINO? yoloDetector = null,
            bool isStatistic = false,
            string? fileName = null
            )
        {

            try
            {
                sw.Restart();
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
                List<DetectionResult> detections;
                if (yoloDetector != null)
                {
                    detections = yoloDetector.Detect(frame);
                }
                else
                {
                    detections = yolo11Seg.Detect(frame);
                }



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



                if (labelDetection != null)
                {
                    if (isSaving == false)
                    {
                        isSaving = true;
                    }

                    counter++;
                    var maskContours = new List<(int x, int y)>();

                    foreach (var contour in labelDetection.Contours)
                    {
                        var polygon = contour
                            .Select(p => (p.X, p.Y))
                            .ToList();
                        foreach (var p in polygon)
                        {
                            maskContours.Add((p.X, p.Y));
                        }

                    }

                    var croptImage = RotationImage.ProcessRotationImage(frame, maskContours); //********************************************************************************************************************
                    EstimatePerformence.EndPerformence();
                    EstimatePerformence.StartPerformence("Rotation");
                    croptYoloMat = croptImage.Clone();

                    /*var rotation = RotationImage.CheckLabelRotation(croptImage, rotationDetector);
                    croptImage = RotationImage.Rotate(croptImage, rotation);//********************************************************************************************************************
                    rotationMat = croptImage.Clone();*/

                    var croppedBmp = MatToBitmap(croptImage);
                    EstimatePerformence.EndPerformence();

                    

                    var grayStandard = ImageEnhancer.ConvertToGrayscale(croppedBmp);



                   
                    EstimatePerformence.StartPerformence("Enhancement");
                    try
                    {
                        var enhanced = grayStandard;  // Start with original
                        /*var enhanced = croppedBmp;  // Start with original 99 100 */



                        // 1️⃣ Tăng sáng (nhà xưởng thường tối)
                        var brightened = ImageEnhancer.EnhanceDark(enhanced, clipLimit: 2.5);
                        if (enhanced != croppedBmp) enhanced.Dispose();
                        enhanced = brightened;
                        Debug.WriteLine($"[ENHANCEMENT] ✓ EnhanceDark completed");

                        // 2️⃣ Làm sắc nét (cải thiện QR detection)
                        var sharpened = ImageEnhancer.SharpenBlurry(enhanced);
                        if (enhanced != croppedBmp) enhanced.Dispose();
                        enhanced = sharpened;
                        Debug.WriteLine($"[ENHANCEMENT] ✓ SharpenBlurry completed");

                        // 3️⃣ Upscale nếu ảnh quá nhỏ
                        int minDim = Math.Min(enhanced.Width, enhanced.Height);
                        if (minDim < 400)
                        {
                            var upscaled = ImageEnhancer.UpscaleSmall(enhanced, 2.0);
                            if (enhanced != croppedBmp) enhanced.Dispose();
                            enhanced = upscaled;
                            Debug.WriteLine($"[ENHANCEMENT] ✓ UpscaleSmall completed: {enhanced.Width}x{enhanced.Height}");
                        }
                        else
                        {
                            Debug.WriteLine($"[ENHANCEMENT] ⊘ UpscaleSmall skipped (size ok)");
                        }

                        // Dispose original cropped bitmap nếu đã enhance
                        if (enhanced != grayStandard)
                        {
                            grayStandard.Dispose();
                            croppedBmp.Dispose();
                            croppedBmp = enhanced;  // Use enhanced version //********************************************************************************************************************
                            preProcessImageMat = enhanced.ToMat();
                        }
                        /*if (enhanced != croppedBmp)
                        {
                            croppedBmp.Dispose();
                            croppedBmp = enhanced;  // Use enhanced version //********************************************************************************************************************
                            preProcessImageMat = enhanced.ToMat();
                        }*/

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[⚠ IMAGE ENHANCEMENT ERROR] {ex.Message}");
                        Debug.WriteLine($"[ENHANCEMENT] ⚠️ Using original image (fallback)");
                        // Continue with original cropped bitmap
                    }
                    EstimatePerformence.EndPerformence();
                    EstimatePerformence.StartPerformence("QR Detection");


                    /*if (!isStatistic)
                    {
                        processImage.BeginInvoke(new Action(() =>
                        {
                            var old = processImage.Image;
                            processImage.Image = croppedBmp;
                            old?.Dispose();
                        }));
                    }*/

                    var (qrPoints, qrText) = LabelDetectorWeChat.DetectQRCodeWeChat(croppedBmp, weChatQRCode);
                  

                    if (qrPoints == null)
                    {
                        var timeProcess = sw.ElapsedMilliseconds;
                        CannotExtractQRTimes.Add(timeProcess);
                        CannotExtractQRDetails.Add(fileName);

                        return result;
                    }

                    result.QRCode = qrText;

                    OpenCvSharp.Point[] qrBox = qrPoints
                        .Select(p => new OpenCvSharp.Point((int)Math.Round(p.X), (int)Math.Round(p.Y)))
                        .ToArray();
                    EstimatePerformence.EndPerformence();






                    EstimatePerformence.StartPerformence("OCR Region");
                    // Gọi hàm với kiểu dữ liệu đã đúng
                    var mergedCrop = CropComponent.CropAndMergeBottomLeftAndAboveQr(croppedBmp, qrBox);

                    var gray = ImageEnhancer.ConvertToGrayBgr(mergedCrop);
                    mergedCrop.Dispose();
                    mergedCrop = gray;//********************************************************************************************************************
                    croptMergeMat = gray.ToMat();

                    if (mergedCrop != null)
                    {
                        bool imageDisplayed = false;

                        // Safety check: Ensure processImage is valid and handle is created
                        if (processImage != null && processImage.IsHandleCreated)
                        {
                            // CRITICAL: Clone bitmap before passing to UI thread
                            // to prevent "Object is currently in use" error (race condition)
                            var mergedCropClone = (Bitmap)mergedCrop.Clone();

                           if (!isStatistic)
                            {
                                processImage.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        var old = processImage.Image;
                                        processImage.Image = mergedCropClone;
                                        old?.Dispose();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[⚠ DISPLAY PROCESS IMAGE ERROR] {ex.Message}");
                                        // If display failed, dispose the clone to prevent memory leak
                                        mergedCropClone?.Dispose();
                                    }
                                }));
                            }

                            
                            imageDisplayed = true;
                        }
                        else
                        {
                            Debug.WriteLine("[⚠] processImage is null or handle not created - skipping display");
                        }
                        EstimatePerformence.EndPerformence();
                        EstimatePerformence.StartPerformence("OCR Extract");
                        var (ocrTexts, minScore, debugText) = ExtractTextsFromMergedCrop(ocr, mergedCrop);
                        if (ocrTexts == null || ocrTexts.Count == 0)
                        {
                            var timeOCRProcess = sw.ElapsedMilliseconds;
                            CannotExtractOCRTimes.Add(timeOCRProcess);
                            CannotExtractOCRDetails.Add(fileName);
                            //SaveImageWithStep(2, workSessionId.ToString(), 5, null, originMat, croptYoloMat, rotationMat, preProcessImageMat, croptMergeMat);
                            return result;
                        }
                        var timeProcess = sw.ElapsedMilliseconds;
                        SuccessTimes.Add(timeProcess);

                        // Always dispose mergedCrop after OCR processing
                        // (UI thread has its own clone if display succeeded)
                        mergedCrop.Dispose();

                        result.ProductTotal = ocrTexts[0];
                        result.ProductCode = ocrTexts[1];
                        result.Size = ocrTexts[2];
                        result.Color = ocrTexts[3];
                        EstimatePerformence.EndPerformence();
                        if (result.ProductTotal == null || result.ProductCode == null || result.Size == null || result.Color == null)
                        {
                            //SaveImageWithStep(1, workSessionId.ToString(), 5, null, originMat, croptYoloMat, rotationMat, preProcessImageMat, croptMergeMat);
                            //return result;
                        }

                        //SaveImageWithStep(0, workSessionId.ToString(), null, result, originMat, croptYoloMat, rotationMat, preProcessImageMat, croptMergeMat);
                    }
                    else
                    {
                        //SaveImageWithStep(2, workSessionId.ToString(), 4, null, originMat, croptYoloMat, rotationMat, preProcessImageMat, null);
                        //return result;
                    }

                }

                var processEndTime = sw.Elapsed.TotalMilliseconds;
                




                var displayBmp = MatToBitmap(displayFrame);

                if (!isStatistic)
                {
                    cameraBox.BeginInvoke(new Action(() =>
                    {
                        var old = cameraBox.Image;
                        cameraBox.Image = displayBmp;
                        old?.Dispose();
                    }));
                }
                


                originMat?.Dispose();
                croptYoloMat?.Dispose();
                rotationMat?.Dispose();
                preProcessImageMat?.Dispose();
                croptMergeMat?.Dispose();

                //ShowPerformence(EstimatePerformence);


                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[⚠ DETECT LABEL V2 ERROR] {ex.Message}");
                Debug.WriteLine($"[⚠ Stack Trace] {ex.StackTrace}");
                return null;
            }
        }



        public static (List<string> texts, float minScore, string DebugText) ExtractTextsFromMergedCrop(PaddleOCREngine ocr, Bitmap mergedCrop)
        {
            var texts = new List<string>();
            string DebugText = "";
            float minScore = 999;

            try
            {
                if (ocr == null || mergedCrop == null)
                    return (texts, -999, "[?] Input null");

                OCRResult result;
                lock (ocr)
                {
                    result = ocr.DetectText(mergedCrop);
                }

                if (result?.TextBlocks?.Count > 0)
                {
                    texts = result.TextBlocks
                        .Where(tb => !string.IsNullOrWhiteSpace(tb.Text))
                        .Select(tb => tb.Text.Trim())
                        .ToList();

                    foreach (var tb in result.TextBlocks)
                    {
                        if (tb.Score < minScore)
                            minScore = tb.Score;
                        DebugText += $"{tb.Text?.Trim()} | Score: {tb.Score * 100:F2}%\r\n";
                    }
                }

                return (texts, minScore, DebugText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[? OCR ONLY ERROR] {ex.Message}");
                return (texts, -999, DebugText);
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
