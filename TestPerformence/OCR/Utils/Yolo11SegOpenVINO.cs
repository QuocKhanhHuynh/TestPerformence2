using OpenCvSharp;
using OpenVinoSharp;

//using OpenVinoSharp;
using OpenVinoSharp.Extensions; // Hỗ trợ xử lý ảnh
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    /// <summary>
    /// Result class for YOLO11 detection (Giữ nguyên)
    /// </summary>
    public class DetectionResult
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public Rect BoundingBox { get; set; }
        public List<OpenCvSharp.Point[]>? Contours { get; set; }
    }

    /// <summary>
    /// YOLO11 Segmentation Detector using OpenVINO IR (.xml & .bin)
    /// </summary>
    public class Yolo11SegOpenVINO : IDisposable
    {
        private Core _core;
        private Model _model;
        private CompiledModel _compiledModel;
        private InferRequest _inferRequest;

        private readonly int _inputWidth = 640;
        private readonly int _inputHeight = 640;
        private float _confThreshold = 0.25f;
        private float _iouThreshold = 0.45f;
        private readonly string[] _classNames;

        private const int MASK_COEFFICIENTS_COUNT = 32;

        // Cấu trúc nội bộ tạm thời
        private class YoloPrediction
        {
            public float[] Box { get; set; } = new float[4];
            public float Confidence { get; set; }
            public int ClassId { get; set; }
            public float[] MaskCoefficients { get; set; } = new float[MASK_COEFFICIENTS_COUNT];
        }

        public Yolo11SegOpenVINO(string modelXmlPath, string[] classNames, float confThreshold = 0.25f, float iouThreshold = 0.45f)
        {
            try
            {
                if (!File.Exists(modelXmlPath))
                    throw new FileNotFoundException($"Model XML not found at: {modelXmlPath}");

                _classNames = classNames;
                _confThreshold = confThreshold;
                _iouThreshold = iouThreshold;

                Console.WriteLine($"[OpenVINO] Initializing Core...");
                _core = new Core();

                // 1. Đọc model (Tự động tìm file .bin cùng tên trong cùng thư mục)
                Console.WriteLine($"[OpenVINO] Reading model: {modelXmlPath}");
                _model = _core.read_model(modelXmlPath);

                // 2. Compile model cho thiết bị (CPU tự động dùng INT8/FP32 tối ưu nhất)
                // Bạn có thể đổi "CPU" thành "GPU" nếu muốn chạy trên Intel Graphics
                Console.WriteLine("[OpenVINO] Compiling model for CPU...");
                _compiledModel = _core.compile_model(_model, "CPU");

                // 3. Tạo Request
                _inferRequest = _compiledModel.create_infer_request();

                // Lấy thông tin input shape (để chắc chắn)
                var inputShape = _model.input().get_shape();
                if (inputShape.Count >= 4)
                {
                    _inputHeight = (int)inputShape[2];
                    _inputWidth = (int)inputShape[3];
                }

                Console.WriteLine($"[OpenVINO] Ready. Input Shape: {_inputWidth}x{_inputHeight}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenVINO] Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Hàm Detect chính
        /// </summary>
        public List<DetectionResult> Detect(Mat frame)
        {
            if (frame == null || frame.Empty()) return new List<DetectionResult>();

            float scaleX = frame.Width / (float)_inputWidth;
            float scaleY = frame.Height / (float)_inputHeight;

            try
            {
                // 1. Preprocessing (Resize & Normalize & HWC -> NCHW)
                float[] inputData = PreprocessImage(frame);

                // 2. Set Input Tensor
                Tensor inputTensor = _inferRequest.get_input_tensor();
                inputTensor.set_data(inputData);

                // 3. Inference
                _inferRequest.infer();

                // 4. Get Outputs
                // Output 0: Detection boxes + Coeffs [1, 116, 8400]
                Tensor outputTensor0 = _inferRequest.get_output_tensor(0);
                int sizeTensor0 = (int)outputTensor0.get_size(); // Lấy tổng số phần tử
                float[] output0 = outputTensor0.get_data<float>(sizeTensor0);

                // Output 1: Prototypes [1, 32, 160, 160]
                Tensor outputTensor1 = _inferRequest.get_output_tensor(1);

                int sizeTensor1 = (int)outputTensor1.get_size(); // Lấy tổng số phần tử
                float[] output1 = outputTensor1.get_data<float>(sizeTensor1);

                // 5. Parse Output
                var shape0 = outputTensor0.get_shape();
                int numBoxes = (int)shape0[2];      // 8400
                int featureSize = (int)shape0[1];   // 116
                int numClasses = featureSize - 4 - MASK_COEFFICIENTS_COUNT; // 116 - 4 - 32 = 80

                var rawPredictions = new List<YoloPrediction>();

                // Xử lý dữ liệu Detection (Output0)
                // OpenVINO trả về mảng phẳng (flat array), cần tính toán index thủ công
                // Layout bộ nhớ là [batch, channel, anchor] -> index = channel * numBoxes + anchor

                // Tiền tính toán offset để tối ưu
                int stride = numBoxes;

                for (int i = 0; i < numBoxes; i++)
                {
                    // Lấy các chỉ số cơ bản: cx, cy, w, h
                    float cx = output0[0 * stride + i]; // channel 0
                    float cy = output0[1 * stride + i]; // channel 1
                    float w = output0[2 * stride + i]; // channel 2
                    float h = output0[3 * stride + i]; // channel 3

                    // Tìm class có confidence cao nhất
                    float maxProb = 0f;
                    int classId = 0;
                    for (int c = 0; c < numClasses; c++)
                    {
                        // Class bắt đầu từ channel 4
                        float prob = output0[(4 + c) * stride + i];
                        if (prob > maxProb)
                        {
                            maxProb = prob;
                            classId = c;
                        }
                    }

                    if (maxProb < _confThreshold) continue;

                    // Convert box
                    float x1 = (cx - w / 2f) * scaleX;
                    float y1 = (cy - h / 2f) * scaleY;
                    float x2 = (cx + w / 2f) * scaleX;
                    float y2 = (cy + h / 2f) * scaleY;

                    // Clamp
                    x1 = Math.Clamp(x1, 0, frame.Width - 1);
                    y1 = Math.Clamp(y1, 0, frame.Height - 1);
                    x2 = Math.Clamp(x2, 0, frame.Width - 1);
                    x2 = Math.Max(x1 + 1, x2);
                    y2 = Math.Clamp(y2, 0, frame.Height - 1);
                    y2 = Math.Max(y1 + 1, y2);

                    // Lấy Mask Coefficients (32 giá trị)
                    // Coefficients bắt đầu từ channel: 4 + numClasses
                    int coeffStartChannel = 4 + numClasses;
                    float[] coeffs = new float[MASK_COEFFICIENTS_COUNT];
                    for (int c = 0; c < MASK_COEFFICIENTS_COUNT; c++)
                    {
                        coeffs[c] = output0[(coeffStartChannel + c) * stride + i];
                    }

                    rawPredictions.Add(new YoloPrediction
                    {
                        ClassId = classId,
                        Confidence = maxProb,
                        Box = new[] { x1, y1, x2, y2 },
                        MaskCoefficients = coeffs
                    });
                }

                // 6. NMS
                var finalPredictions = RunNMS(rawPredictions);
                var finalDetections = new List<DetectionResult>();

                // 7. Generate Masks
                foreach (var pred in finalPredictions)
                {
                    // Truyền mảng phẳng output1 vào thay vì Tensor object
                    var contours = GenerateMaskContours(output1, pred.MaskCoefficients, pred.Box, frame.Width, frame.Height);

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
                        Contours = contours
                    });
                }

                return finalDetections;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenVINO] Detection Error: {ex.Message}");
                return new List<DetectionResult>();
            }
        }

        // --- PREPROCESSING ---
        private float[] PreprocessImage(Mat frame)
        {
            // 1. Resize
            using Mat resized = new Mat();
            Cv2.Resize(frame, resized, new OpenCvSharp.Size(_inputWidth, _inputHeight));

            // 2. Chuyển đổi màu BGR -> RGB (OpenVINO cần RGB)
            // Lưu ý: Nếu model train bằng OpenCV mặc định thì có thể là BGR, nhưng chuẩn YOLO là RGB.
            using Mat rgb = new Mat();
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

            // 3. Chuẩn hóa và chuyển sang mảng float [1, 3, H, W]
            // Cách thủ công nhưng chính xác và an toàn nhất
            float[] result = new float[1 * 3 * _inputHeight * _inputWidth];

            // Dùng unsafe code để loop nhanh hơn
            unsafe
            {
                byte* ptr = (byte*)rgb.Data;
                int pixelCount = _inputHeight * _inputWidth;

                // Offset cho từng channel trong mảng kết quả (NCHW layout)
                int offsetG = pixelCount;     // Channel Green bắt đầu sau channel Red
                int offsetB = pixelCount * 2; // Channel Blue bắt đầu sau channel Green

                for (int i = 0; i < pixelCount; i++)
                {
                    // RGB ảnh gốc: [R, G, B, R, G, B...]
                    // ptr[i*3+0] -> R
                    // ptr[i*3+1] -> G
                    // ptr[i*3+2] -> B

                    // Normalize chia 255.0f
                    result[i] = ptr[i * 3 + 0] / 255.0f; // R
                    result[i + offsetG] = ptr[i * 3 + 1] / 255.0f; // G
                    result[i + offsetB] = ptr[i * 3 + 2] / 255.0f; // B
                }
            }

            return result;
        }

        // --- MASK GENERATION (Logic cốt lõi đã sửa đổi cho mảng phẳng) ---
        private List<OpenCvSharp.Point[]> GenerateMaskContours(float[] prototypesFlat, float[] coeffs, float[] box, int origW, int origH)
        {
            int protoH = 160;
            int protoW = 160;
            int protoChannels = 32;
            int protoArea = protoH * protoW;

            // 1. Matrix Mul: (coeffs x prototypes) -> Sigmoid
            // prototypesFlat layout: [channel, y, x] -> index = c * (160*160) + y*160 + x
            using Mat rawMask = new Mat(protoH, protoW, MatType.CV_32FC1);

            unsafe
            {
                float* maskPtr = (float*)rawMask.Data;

                // Duyệt qua từng pixel của mask (160x160)
                // Dùng Parallel.For để tăng tốc độ nếu cần, ở đây dùng for thường cho đơn giản
                for (int i = 0; i < protoArea; i++)
                {
                    float sum = 0;
                    for (int c = 0; c < protoChannels; c++)
                    {
                        // prototypesFlat[c * protoArea + i] * coeffs[c]
                        sum += prototypesFlat[c * protoArea + i] * coeffs[c];
                    }
                    // Sigmoid
                    maskPtr[i] = 1.0f / (1.0f + (float)Math.Exp(-sum));
                }
            }

            // --- Phần dưới giữ nguyên logic crop và resize của bạn ---

            // 2. Tính toán Crop Rect trên mask 160x160
            float maskScaleX = protoW / (float)origW;
            float maskScaleY = protoH / (float)origH;

            int mx1 = Math.Clamp((int)(box[0] * maskScaleX), 0, protoW);
            int my1 = Math.Clamp((int)(box[1] * maskScaleY), 0, protoH);
            int mx2 = Math.Clamp((int)(box[2] * maskScaleX), 0, protoW);
            int my2 = Math.Clamp((int)(box[3] * maskScaleY), 0, protoH);

            int mw = mx2 - mx1;
            int mh = my2 - my1;

            if (mw <= 0 || mh <= 0) return new List<OpenCvSharp.Point[]>();

            Rect cropRect = new Rect(mx1, my1, mw, mh);
            using Mat croppedMask = new Mat(rawMask, cropRect);

            // 3. Resize về kích thước box thực tế
            int finalW = (int)(box[2] - box[0]);
            int finalH = (int)(box[3] - box[1]);

            if (finalW <= 0 || finalH <= 0) return new List<OpenCvSharp.Point[]>();

            using Mat finalMask = new Mat();
            Cv2.Resize(croppedMask, finalMask, new OpenCvSharp.Size(finalW, finalH), 0, 0, InterpolationFlags.Linear); // Linear mượt hơn Nearest cho mask

            // 4. Threshold -> Binary Mask
            using Mat mask8U = new Mat();
            // 0.5 là ngưỡng sigmoid
            Cv2.Threshold(finalMask, mask8U, 0.5, 255, ThresholdTypes.Binary);
            mask8U.ConvertTo(mask8U, MatType.CV_8UC1);

            // 5. Find Contours
            Cv2.FindContours(mask8U, out OpenCvSharp.Point[][] contours, out _,
                RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // 6. Map contours về tọa độ gốc
            int xOffset = (int)box[0];
            int yOffset = (int)box[1];

            var resultContours = new List<OpenCvSharp.Point[]>();
            foreach (var contour in contours)
            {
                var mapped = contour.Select(p => new OpenCvSharp.Point(p.X + xOffset, p.Y + yOffset)).ToArray();
                resultContours.Add(mapped);
            }

            return resultContours;
        }

        // --- Helper NMS (Giữ nguyên logic cũ) ---
        private List<YoloPrediction> RunNMS(List<YoloPrediction> predictions)
        {
            if (predictions.Count == 0) return new List<YoloPrediction>();

            var sorted = predictions.OrderByDescending(p => p.Confidence).ToList();
            var result = new List<YoloPrediction>();
            bool[] suppressed = new bool[sorted.Count];

            for (int i = 0; i < sorted.Count; i++)
            {
                if (suppressed[i]) continue;
                result.Add(sorted[i]);

                for (int j = i + 1; j < sorted.Count; j++)
                {
                    if (suppressed[j]) continue;
                    float iou = CalculateIoU(sorted[i].Box, sorted[j].Box);
                    if (iou > _iouThreshold) suppressed[j] = true;
                }
            }
            return result;
        }

        private float CalculateIoU(float[] b1, float[] b2)
        {
            float xA = Math.Max(b1[0], b2[0]);
            float yA = Math.Max(b1[1], b2[1]);
            float xB = Math.Min(b1[2], b2[2]);
            float yB = Math.Min(b1[3], b2[3]);

            float interArea = Math.Max(0, xB - xA) * Math.Max(0, yB - yA);
            float b1Area = (b1[2] - b1[0]) * (b1[3] - b1[1]);
            float b2Area = (b2[2] - b2[0]) * (b2[3] - b2[1]);
            float unionArea = b1Area + b2Area - interArea;

            return unionArea <= 0 ? 0 : interArea / unionArea;
        }

        public void Dispose()
        {
            _inferRequest?.Dispose();
            _compiledModel?.Dispose();
            _model?.Dispose();
            _core?.Dispose();
        }
    }
}