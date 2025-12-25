using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.Design.AxImporter;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    /// <summary>
    /// Result class for YOLO11 detection
    /// </summary>
    public class DetectionResult1
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public Rect BoundingBox { get; set; }

        // Thay vì Mat, ta lưu danh sách các contour
        public List<OpenCvSharp.Point[]>? Contours { get; set; }
    }

    /// <summary>
    /// YOLO11 Segmentation Detector using ONNX Runtime
    /// </summary>
    public class Yolo11Seg : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly SessionOptions _options;
        private readonly int _inputWidth = 640;
        private readonly int _inputHeight = 640;
        private readonly float _confThreshold = 0.25f;
        private readonly float _iouThreshold = 0.45f;
        private readonly string[] _classNames;

        // Hằng số quan trọng cho mô hình segmentation
        private const int MASK_COEFFICIENTS_COUNT = 32;

        // Cấu trúc tạm thời để chứa dữ liệu thô trước NMS
        private class YoloPrediction
        {
            public float[] Box { get; set; } = new float[4]; // [x1, y1, x2, y2] (scaled to original frame size)
            public float Confidence { get; set; }
            public int ClassId { get; set; }
            public float[] MaskCoefficients { get; set; } = new float[MASK_COEFFICIENTS_COUNT];
        }

        public Yolo11Seg(string modelPath, string[] classNames, float confThreshold = 0.25f, float iouThreshold = 0.45f)
        {
            try
            {
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"ONNX model not found at: {modelPath}");
                }

                Console.WriteLine($"[YOLO11] Initializing with model: {modelPath}");

                _classNames = classNames;
                _confThreshold = confThreshold;
                _iouThreshold = iouThreshold;


                // Configure session options
                Console.WriteLine("[YOLO11] Creating SessionOptions...");

                _options = new SessionOptions();
                //_options.AppendExecutionProvider_OpenVINO("CPU");
                _options.AppendExecutionProvider_OpenVINO("CPU_FP32");
                //_options.AppendExecutionProvider_OpenVINO(@"CPU_FP32");
                Console.WriteLine("[YOLO11] SessionOptions created successfully");
                
                _options.EnableCpuMemArena = true;
                _options.EnableMemoryPattern = true;
                _options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                Console.WriteLine("[YOLO11] SessionOptions configured");

                // Create inference session
                Console.WriteLine("[YOLO11] Creating InferenceSession...");
                _session = new InferenceSession(modelPath, _options);
                Console.WriteLine("[YOLO11] InferenceSession created successfully");

                // Get model metadata
                var inputMeta = _session.InputMetadata.First();
                var shape = inputMeta.Value.Dimensions;
                // Input shape is typically [1, 3, H, W]
                if (shape.Length >= 4)
                {
                    _inputHeight = (int)shape[2];
                    _inputWidth = (int)shape[3];
                }
                
                Console.WriteLine($"[YOLO11] Model initialized: {_inputWidth}x{_inputHeight}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YOLO11] FATAL ERROR during initialization: {ex.GetType().Name}");
                Console.WriteLine($"[YOLO11] Message: {ex.Message}");
                MessageBox.Show(ex.Message);
                Console.WriteLine($"[YOLO11] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[YOLO11] Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        // ----------------------------------------------------------------------------------
        // ## 1. Detection Core Method
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Detect objects and generate segmentation masks in an image frame
        /// </summary>
        /// <param name="frame">Input image (BGR format from OpenCV)</param>
        /// <returns>List of detection results with bounding boxes and masks</returns>
        public List<DetectionResult> Detect(Mat frame)
        {
            if (frame == null || frame.Empty())
            {
                return new List<DetectionResult>();
            }

            var originalFrameWidth = frame.Width;
            var originalFrameHeight = frame.Height;
            var scaleX = originalFrameWidth / (float)_inputWidth;
            var scaleY = originalFrameHeight / (float)_inputHeight;

            try
            {
                // Preprocessing
                var inputTensor = PreprocessImage(frame);

                var inputName = _session.InputMetadata.Keys.First();
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
                };

                // Run inference
                using var results = _session.Run(inputs);

                // Get outputs
                var resultsList = results.ToList();
                if (resultsList.Count < 2)
                {
                    Console.WriteLine("[YOLO11] Model did not return 2 outputs (Detection + Prototypes)");
                    return new List<DetectionResult>();
                }

                var output0 = resultsList[0].AsTensor<float>();  // Detection + coefficients [1, 116, 8400]
                var output1 = resultsList[1].AsTensor<float>();  // Mask prototypes [1, 32, 160, 160]

                // Get output dimensions
                int numBoxes = (int)output0.Dimensions[2];  // e.g., 8400
                int featureSize = (int)output0.Dimensions[1];  // e.g., 116
                // Calculate number of classes dynamically: 116 - 4 (box) - 32 (coeffs) = 80
                int numClasses = featureSize - 4 - MASK_COEFFICIENTS_COUNT;
                int coeffOffset = 4 + numClasses;

                var rawPredictions = new List<YoloPrediction>();

                // 1. Process and Collect Raw Detections
                for (int i = 0; i < numBoxes; i++)
                {
                    // Get bounding box (center format)
                    float cx = output0[0, 0, i];
                    float cy = output0[0, 1, i];
                    float w = output0[0, 2, i];
                    float h = output0[0, 3, i];

                    // Find class with highest probability
                    float maxProb = 0f;
                    int classId = 0;
                    for (int c = 0; c < numClasses; c++)
                    {
                        float prob = output0[0, 4 + c, i];
                        if (prob > maxProb)
                        {
                            maxProb = prob;
                            classId = c;
                        }
                    }

                    // Filter by confidence threshold
                    if (maxProb < _confThreshold)
                        continue;

                    // Convert center format to corner format and scale to original image
                    float x1 = (cx - w / 2f) * scaleX;
                    float y1 = (cy - h / 2f) * scaleY;
                    float x2 = (cx + w / 2f) * scaleX;
                    float y2 = (cy + h / 2f) * scaleY;

                    // Clamp to image bounds and ensure minimum size
                    x1 = Math.Clamp(x1, 0, originalFrameWidth - 1);
                    y1 = Math.Clamp(y1, 0, originalFrameHeight - 1);
                    x2 = Math.Clamp(x2, 0, originalFrameWidth - 1);
                    x2 = Math.Max(x1 + 1, x2);
                    y2 = Math.Clamp(y2, 0, originalFrameHeight - 1);
                    y2 = Math.Max(y1 + 1, y2);


                    var coeffs = new float[MASK_COEFFICIENTS_COUNT];
                    for (int c = 0; c < MASK_COEFFICIENTS_COUNT; c++)
                    {
                        coeffs[c] = output0[0, coeffOffset + c, i];
                    }

                    rawPredictions.Add(new YoloPrediction
                    {
                        ClassId = classId,
                        Confidence = maxProb,
                        Box = new[] { x1, y1, x2, y2 },
                        MaskCoefficients = coeffs
                    });
                }

                // 2. Run NMS to filter detections
                var finalPredictions = RunNMS(rawPredictions);
                var finalDetections = new List<DetectionResult>();

                // 3. Generate Masks for NMS-filtered results
                foreach (var pred in finalPredictions)
                {
                    var contours = GenerateMaskContours(output1, pred.MaskCoefficients, pred.Box, originalFrameWidth, originalFrameHeight);

                    finalDetections.Add(new DetectionResult
                    {
                        ClassId = pred.ClassId,
                        ClassName = pred.ClassId < _classNames.Length ? _classNames[pred.ClassId] : $"Class_{pred.ClassId}",
                        Confidence = pred.Confidence,
                        BoundingBox = new Rect(
                            (int)pred.Box[0],
                            (int)pred.Box[1],
                            (int)(pred.Box[2] - pred.Box[0]),
                            (int)(pred.Box[3] - pred.Box[1])
                        ),
                        Contours = contours // Trả về danh sách điểm contour
                    });
                }


                return finalDetections;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YOLO11] Detection error: {ex.Message}");
                return new List<DetectionResult>();
            }
        }


        private List<OpenCvSharp.Point[]> GenerateMaskContours(Tensor<float> prototypes, float[] coeffs, float[] box, int originalFrameWidth, int originalFrameHeight)
        {
            // 1. Tạo mask nhị phân (có thể là CV_32F)
            var binaryMaskFloat = GenerateMask(prototypes, coeffs, box, originalFrameWidth, originalFrameHeight);

            // 2. Nếu mask không phải CV_8UC1, convert
            Mat mask8U;
            if (binaryMaskFloat.Type() != MatType.CV_8UC1)
            {
                mask8U = new Mat();
                binaryMaskFloat.ConvertTo(mask8U, MatType.CV_8UC1);
                binaryMaskFloat.Dispose();
            }
            else
            {
                mask8U = binaryMaskFloat;
            }

            // 3. Find contours on mask8U (mask8U size == bounding box size)
            Cv2.FindContours(mask8U, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy,
                             RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // 4. Map contour points from mask coordinates to original image coordinates
            int xOffset = (int)box[0];
            int yOffset = (int)box[1];

            var mappedContours = new List<OpenCvSharp.Point[]>();
            foreach (var contour in contours)
            {
                var mapped = contour.Select(p => new OpenCvSharp.Point(p.X + xOffset, p.Y + yOffset)).ToArray();
                mappedContours.Add(mapped);
            }

            mask8U.Dispose();
            return mappedContours;
        }



        // ----------------------------------------------------------------------------------
        // ## 2. Preprocessing
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Preprocess image: resize, normalize, and convert to tensor
        /// </summary>
        private DenseTensor<float> PreprocessImage(Mat frame)
        {
            // 1. Resize to input size (direct resize, no letterbox)
            var resized = new Mat();
            Cv2.Resize(frame, resized, new OpenCvSharp.Size(_inputWidth, _inputHeight), 0, 0, InterpolationFlags.Linear);

            // 2. Convert BGR to RGB
            var rgb = new Mat();
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

            // 3. Normalize to [0, 1] and convert to CHW format
            var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

            unsafe
            {
                byte* ptr = (byte*)rgb.Data;
                int channels = 3; // RGB

                for (int y = 0; y < _inputHeight; y++)
                {
                    for (int x = 0; x < _inputWidth; x++)
                    {
                        int pixelIndex = (y * _inputWidth + x) * channels;

                        // RGB order, normalize to [0, 1]
                        tensor[0, 0, y, x] = ptr[pixelIndex + 0] / 255f;  // R
                        tensor[0, 1, y, x] = ptr[pixelIndex + 1] / 255f;  // G
                        tensor[0, 2, y, x] = ptr[pixelIndex + 2] / 255f;  // B
                    }
                }
            }

            resized.Dispose();
            rgb.Dispose();

            return tensor;
        }

        // ----------------------------------------------------------------------------------
        // ## 3. Non-Maximum Suppression (NMS)
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Thực hiện Non-Maximum Suppression (NMS) để lọc các hộp giới hạn trùng lặp.
        /// </summary>
        private List<YoloPrediction> RunNMS(List<YoloPrediction> predictions)
        {
            if (predictions.Count == 0)
                return new List<YoloPrediction>();

            // Sắp xếp các box theo confidence giảm dần
            var sortedPredictions = predictions.OrderByDescending(p => p.Confidence).ToList();
            var selectedPredictions = new List<YoloPrediction>();

            var keep = new bool[sortedPredictions.Count];
            for (int i = 0; i < keep.Length; i++)
                keep[i] = true;

            for (int i = 0; i < sortedPredictions.Count; i++)
            {
                if (!keep[i])
                    continue;

                selectedPredictions.Add(sortedPredictions[i]);

                for (int j = i + 1; j < sortedPredictions.Count; j++)
                {
                    if (!keep[j])
                        continue;

                    // Tính IoU
                    float iou = CalculateIoU(sortedPredictions[i].Box, sortedPredictions[j].Box);

                    if (iou > _iouThreshold)
                    {
                        keep[j] = false; // Đánh dấu box trùng lặp để loại bỏ
                    }
                }
            }

            return selectedPredictions;
        }

        /// <summary>
        /// Tính Intersection over Union (IoU) giữa hai bounding box.
        /// </summary>
        private float CalculateIoU(float[] box1, float[] box2)
        {
            // Tọa độ giao điểm
            float xA = Math.Max(box1[0], box2[0]);
            float yA = Math.Max(box1[1], box2[1]);
            float xB = Math.Min(box1[2], box2[2]);
            float yB = Math.Min(box1[3], box2[3]);

            // Diện tích giao điểm (Intersection)
            float interWidth = Math.Max(0, xB - xA);
            float interHeight = Math.Max(0, yB - yA);
            float intersectionArea = interWidth * interHeight;

            // Diện tích Box 1 và Box 2
            float box1Area = (box1[2] - box1[0]) * (box1[3] - box1[1]);
            float box2Area = (box2[2] - box2[0]) * (box2[3] - box2[1]);

            // Diện tích hợp (Union)
            float unionArea = box1Area + box2Area - intersectionArea;
            if (unionArea <= 0) return 0; // Tránh chia cho 0

            return intersectionArea / unionArea;
        }

        // ----------------------------------------------------------------------------------
        // ## 4. Mask Generation
        // ----------------------------------------------------------------------------------

        /// <summary>
        /// Tạo mặt nạ phân đoạn từ hệ số và nguyên mẫu.
        /// </summary>
        /// <param name="prototypes">Tensor chứa các nguyên mẫu mask (output1: [1, 32, 160, 160])</param>
        /// <param name="coeffs">Mảng 32 hệ số mask.</param>
        /// <param name="box">Bounding Box đã được scale về kích thước ảnh gốc ([x1, y1, x2, y2])</param>
        /// <returns>Mat chứa mask nhị phân đã được cắt và resize (CV_8UC1).</returns>
        private Mat GenerateMask(Tensor<float> prototypes, float[] coeffs, float[] box, int originalFrameWidth, int originalFrameHeight)
        {
            int protoChannels = (int)prototypes.Dimensions[1]; // 32
            int protoHeight = (int)prototypes.Dimensions[2]; // 160
            int protoWidth = (int)prototypes.Dimensions[3]; // 160

            // 1. Matrix Multiplication (coeffs x prototypes) & Sigmoid
            // Kết quả: RawMask [160, 160], CV_32FC1 (float)
            var rawMask = new Mat(protoHeight, protoWidth, MatType.CV_32FC1);
            unsafe
            {
                float* rawMaskPtr = (float*)rawMask.Data;
                for (int y = 0; y < protoHeight; y++)
                {
                    for (int x = 0; x < protoWidth; x++)
                    {
                        float sum = 0;
                        for (int c = 0; c < protoChannels; c++)
                        {
                            // prototypes[0, c, y, x] * coeffs[c]
                            sum += prototypes[0, c, y, x] * coeffs[c];
                        }
                        // Sigmoid Activation: 1 / (1 + e^(-sum))
                        rawMaskPtr[y * protoWidth + x] = 1.0f / (1.0f + (float)Math.Exp(-sum));
                    }
                }
            }

            // 2. Cropping (Cắt mask)
            // Tỷ lệ scale từ ảnh gốc -> mask proto (160x160)
            float maskScaleX = protoWidth / (float)originalFrameWidth;
            float maskScaleY = protoHeight / (float)originalFrameHeight;

            // Tọa độ box trên mask proto (160x160)
            int maskX1 = (int)(box[0] * maskScaleX);
            int maskY1 = (int)(box[1] * maskScaleY);
            int maskX2 = (int)(box[2] * maskScaleX);
            int maskY2 = (int)(box[3] * maskScaleY);

            // Clamp tọa độ
            maskX1 = Math.Clamp(maskX1, 0, protoWidth);
            maskY1 = Math.Clamp(maskY1, 0, protoHeight);
            maskX2 = Math.Clamp(maskX2, 0, protoWidth);
            maskY2 = Math.Clamp(maskY2, 0, protoHeight);

            // Cắt mask thô (float) theo tọa độ đã scale
            var cropRect = new OpenCvSharp.Rect(maskX1, maskY1, maskX2 - maskX1, maskY2 - maskY1);
            var croppedRawMask = new Mat(rawMask, cropRect);

            // 3. Resize mask đã cắt về kích thước bounding box trên ảnh gốc
            var finalMask = new Mat();
            int finalW = (int)(box[2] - box[0]);
            int finalH = (int)(box[3] - box[1]);

            if (finalW > 0 && finalH > 0)
            {
                // Resize về kích thước thực của Bounding Box
                Cv2.Resize(croppedRawMask, finalMask, new OpenCvSharp.Size(finalW, finalH), 0, 0, InterpolationFlags.Nearest);
            }

            // 4. Thresholding to create final binary mask (CV_8UC1)
            var binaryMask = new Mat();
            // Lấy ngưỡng 0.5, đưa về 255 (trắng)
            Cv2.Threshold(finalMask, binaryMask, 0.5, 255, ThresholdTypes.Binary);

            // Giải phóng tài nguyên tạm thời
            rawMask.Dispose();
            croppedRawMask.Dispose();
            finalMask.Dispose();

            return binaryMask; // Trả về mask nhị phân 8-bit
        }

        // ----------------------------------------------------------------------------------
        // ## 5. IDisposable Implementation
        // ----------------------------------------------------------------------------------

        public void Dispose()
        {
            _session?.Dispose();
            _options?.Dispose();
        }
    }
}
