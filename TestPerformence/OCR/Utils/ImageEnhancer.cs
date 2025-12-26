using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CvSize = OpenCvSharp.Size;
using CvPoint = OpenCvSharp.Point;


namespace GarmentGridApp.Presentation.OCR.Utils
{
    public static class ImageEnhancer
    {

        /// <summary>
        /// Chuyển đổi ảnh từ Grayscale sang BGR (3 kênh)
        /// Thường dùng để chuẩn bị ảnh trước khi vẽ các annotation màu lên ảnh xám
        /// </summary>
        /// <summary>
        /// Chuyển đổi ảnh màu BGR sang định dạng ảnh xám nhưng vẫn giữ 3 kênh (Gray-BGR).
        /// Kết quả: Ảnh nhìn là trắng đen nhưng có cấu trúc 3 kênh Blue=Green=Red.
        /// </summary>
        public static Bitmap ConvertToGrayBgr(Bitmap input)
        {
            try
            {
                using Mat src = BitmapToMat(input);
                using Mat gray = new Mat();
                using Mat grayBgr = new Mat();

                // Bước 1: Chuyển từ BGR (3 kênh màu) sang Gray (1 kênh duy nhất)
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                // Bước 2: Chuyển ngược từ Gray (1 kênh) sang BGR (3 kênh)
                // Lúc này giá trị mỗi pixel sẽ là (gray, gray, gray)
                Cv2.CvtColor(gray, grayBgr, ColorConversionCodes.GRAY2BGR);

                return MatToBitmap(grayBgr);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConvertToGrayBgr] Error: {ex.Message}");
                return (Bitmap)input.Clone();
            }
        }

        /// <summary>
        /// Chuyển đổi hẳn về ảnh mức xám 1 kênh màu (8bpp Indexed).
        /// </summary>
        public static Bitmap ConvertToGrayscale(Bitmap input)
        {
            try
            {
                using Mat src = BitmapToMat(input);
                using Mat gray = new Mat();

                // Chuyển sang Gray (1 kênh duy nhất)
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                return MatToBitmap(gray);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConvertToGrayscale] Error: {ex.Message}");
                return (Bitmap)input.Clone();
            }
        }


        #region 1. XỬ LÝ ẢNH BỊ CONG (Distortion Correction)

        /// <summary>
        /// Sửa ảnh bị cong bằng perspective transform
        /// Input: Bitmap ROI có thể bị cong
        /// Output: Bitmap đã được straighten
        /// Thời gian: ~15-20ms
        /// </summary>
        public static Bitmap CorrectDistortion(Bitmap input)
        {
            try
            {
                using Mat src = BitmapToMat(input);
                using Mat gray = new Mat();
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                // Tăng contrast trước
                using Mat enhanced = new Mat();
                Cv2.EqualizeHist(gray, enhanced);

                // Tìm edges
                using Mat edges = new Mat();
                Cv2.Canny(enhanced, edges, 30, 100);

                // Dilate để kết nối các edges gần nhau
                using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new CvSize(3, 3));
                using Mat dilated = new Mat();
                Cv2.Dilate(edges, dilated, kernel);

                // Tìm contours
                Cv2.FindContours(dilated, out var contours, out _,
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length == 0)
                    return (Bitmap)input.Clone();

                // Tìm contour lớn nhất có diện tích > 20% ảnh
                double minArea = src.Width * src.Height * 0.2;
                CvPoint[]? bestContour = null;
                double maxArea = 0;

                foreach (var contour in contours)
                {
                    double area = Cv2.ContourArea(contour);
                    if (area > minArea && area > maxArea)
                    {
                        maxArea = area;
                        bestContour = contour;
                    }
                }

                if (bestContour == null)
                    return (Bitmap)input.Clone();

                // Approximate polygon với epsilon nhỏ hơn
                var epsilon = 0.01 * Cv2.ArcLength(bestContour, true);
                var approx = Cv2.ApproxPolyDP(bestContour, epsilon, true);

                // Nếu có 4 điểm, thực hiện perspective transform
                if (approx.Length == 4)
                {
                    var srcPoints = new Point2f[4];
                    for (int i = 0; i < 4; i++)
                    {
                        srcPoints[i] = new Point2f(approx[i].X, approx[i].Y);
                    }

                    // Sắp xếp điểm: TL, TR, BR, BL
                    srcPoints = SortPoints(srcPoints);

                    // Tính kích thước output
                    float width = Math.Max(
                        Distance(srcPoints[0], srcPoints[1]),
                        Distance(srcPoints[2], srcPoints[3])
                    );
                    float height = Math.Max(
                        Distance(srcPoints[0], srcPoints[3]),
                        Distance(srcPoints[1], srcPoints[2])
                    );

                    var dstPoints = new Point2f[]
                    {
                        new Point2f(0, 0),
                        new Point2f(width, 0),
                        new Point2f(width, height),
                        new Point2f(0, height)
                    };

                    using Mat transform = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);
                    using Mat corrected = new Mat();
                    Cv2.WarpPerspective(src, corrected, transform,
                        new CvSize((int)width, (int)height));

                    Debug.WriteLine($"[CorrectDistortion] Applied perspective transform");
                    return MatToBitmap(corrected);
                }

                return (Bitmap)input.Clone();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CorrectDistortion] Error: {ex.Message}");
                return (Bitmap)input.Clone();
            }
        }

        #endregion

        #region 2. XỬ LÝ ẢNH MỜ (Deblurring)

        /// <summary>
        /// Làm sắc nét ảnh bị mờ bằng Unsharp Mask
        /// Input: Bitmap ROI bị mờ
        /// Output: Bitmap đã được sharpen
        /// Thời gian: ~10-15ms
        /// </summary>
        public static Bitmap SharpenBlurry(Bitmap input)
        {
            try
            {
                using Mat src = BitmapToMat(input);

                // Gaussian blur
                using Mat blurred = new Mat();
                Cv2.GaussianBlur(src, blurred, new CvSize(0, 0), 1.0);

                // Unsharp mask: original + amount * (original - blurred)
                using Mat sharpened = new Mat();
                double amount = 1.5;
                Cv2.AddWeighted(src, 1.0 + amount, blurred, -amount, 0, sharpened);

                return MatToBitmap(sharpened);
            }
            catch
            {
                return (Bitmap)input.Clone();
            }
        }

        #endregion

        #region 3. XỬ LÝ ẢNH NGHIÊNG (Deskewing)

        /// <summary>
        /// Xoay ảnh nghiêng về thẳng
        /// Input: Bitmap ROI bị nghiêng
        /// Output: Bitmap đã được deskew
        /// Thời gian: ~15-20ms
        /// </summary>
        public static Bitmap CorrectSkew(Bitmap input)
        {
            try
            {
                using Mat src = BitmapToMat(input);
                using Mat gray = new Mat();
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

                // Edge detection
                using Mat edges = new Mat();
                Cv2.Canny(gray, edges, 50, 150, 3);

                // Hough Line Transform để tìm các đường thẳng
                var lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 100, 100, 10);

                if (lines == null || lines.Length == 0)
                    return (Bitmap)input.Clone();

                // Tính góc của các đường thẳng
                var angles = new List<double>();
                foreach (var line in lines)
                {
                    double angle = Math.Atan2(line.P2.Y - line.P1.Y, line.P2.X - line.P1.X) * 180.0 / Math.PI;

                    // Normalize angle to [-45, 45]
                    while (angle < -45) angle += 90;
                    while (angle > 45) angle -= 90;

                    // Chỉ lấy các góc gần horizontal hoặc vertical
                    if (Math.Abs(angle) < 45)
                        angles.Add(angle);
                }

                if (angles.Count == 0)
                    return (Bitmap)input.Clone();

                // Lấy median angle (robust hơn mean)
                angles.Sort();
                double medianAngle = angles[angles.Count / 2];

                // Nếu góc quá nhỏ, không cần xoay
                if (Math.Abs(medianAngle) < 0.5)
                    return (Bitmap)input.Clone();

                Debug.WriteLine($"[CorrectSkew] Detected angle: {medianAngle:F2}°");

                // Xoay ảnh
                var center = new Point2f(src.Width / 2f, src.Height / 2f);
                using Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, -medianAngle, 1.0);

                // Tính kích thước mới
                double radians = Math.Abs(medianAngle) * Math.PI / 180.0;
                int newWidth = (int)(src.Height * Math.Abs(Math.Sin(radians)) +
                                     src.Width * Math.Abs(Math.Cos(radians)));
                int newHeight = (int)(src.Height * Math.Abs(Math.Cos(radians)) +
                                      src.Width * Math.Abs(Math.Sin(radians)));

                // Điều chỉnh translation
                rotationMatrix.Set(0, 2, rotationMatrix.At<double>(0, 2) +
                    (newWidth - src.Width) / 2.0);
                rotationMatrix.Set(1, 2, rotationMatrix.At<double>(1, 2) +
                    (newHeight - src.Height) / 2.0);

                using Mat rotated = new Mat();
                Cv2.WarpAffine(src, rotated, rotationMatrix,
                    new CvSize(newWidth, newHeight),
                    InterpolationFlags.Cubic,
                    BorderTypes.Constant,
                    Scalar.White);

                return MatToBitmap(rotated);
            }
            catch
            {
                return (Bitmap)input.Clone();
            }
        }

        #endregion

        #region 4. XỬ LÝ ẢNH NHỎ (Upscaling)

        /// <summary>
        /// Phóng to ảnh nhỏ bằng Bicubic interpolation
        /// Input: Bitmap ROI kích thước nhỏ
        /// Output: Bitmap đã được upscale
        /// Thời gian: ~5-10ms
        /// </summary>
        public static Bitmap UpscaleSmall(Bitmap input, double scaleFactor = 2.0)
        {
            try
            {
                using Mat src = BitmapToMat(input);

                // Upscale
                using Mat upscaled = new Mat();
                Cv2.Resize(src, upscaled, new CvSize(), scaleFactor, scaleFactor,
                    InterpolationFlags.Cubic);

                // Denoise nhẹ sau khi upscale
                using Mat denoised = new Mat();
                Cv2.BilateralFilter(upscaled, denoised, 5, 50, 50);

                return MatToBitmap(denoised);
            }
            catch
            {
                return (Bitmap)input.Clone();
            }
        }

        #endregion

        #region 5. XỬ LÝ ẢNH TỐI (Brightness Enhancement)

        /// <summary>
        /// Tăng độ sáng cho ảnh tối bằng CLAHE
        /// Input: Bitmap ROI tối
        /// Output: Bitmap đã được enhance
        /// Thời gian: ~10-15ms
        /// </summary>
        public static Bitmap EnhanceDark(Bitmap input, double clipLimit = 3.0)
        {
            try
            {
                using Mat src = BitmapToMat(input);

                // Convert to LAB color space
                using Mat lab = new Mat();
                Cv2.CvtColor(src, lab, ColorConversionCodes.BGR2Lab);

                // Split channels
                Mat[] channels = Cv2.Split(lab);

                // Apply CLAHE to L channel
                using var clahe = Cv2.CreateCLAHE(
                    clipLimit: clipLimit,
                    tileGridSize: new CvSize(8, 8)
                );
                clahe.Apply(channels[0], channels[0]);

                // Merge back
                Cv2.Merge(channels, lab);
                foreach (var ch in channels) ch.Dispose();

                // Convert back to BGR
                using Mat result = new Mat();
                Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);

                return MatToBitmap(result);
            }
            catch
            {
                return (Bitmap)input.Clone();
            }
        }

        #endregion

        #region BONUS: Auto Enhancement (Tự động phát hiện và xử lý)

        /// <summary>
        /// Tự động phát hiện vấn đề và áp dụng enhancement phù hợp
        /// Input: Bitmap ROI
        /// Output: Bitmap đã được enhance
        /// Thời gian: ~30-50ms (tùy số lượng enhancement cần áp dụng)
        /// </summary>
        public static Bitmap AutoEnhance(Bitmap input)
        {
            try
            {
                var current = (Bitmap)input.Clone();

                using Mat mat = BitmapToMat(current);

                // 1. Kiểm tra độ tối
                bool isDark = CheckIfDark(mat);
                if (isDark)
                {
                    var enhanced = EnhanceDark(current);
                    current.Dispose();
                    current = enhanced;
                }

                // 2. Kiểm tra độ mờ
                bool isBlurry = CheckIfBlurry(mat);
                if (isBlurry)
                {
                    var sharpened = SharpenBlurry(current);
                    current.Dispose();
                    current = sharpened;
                }

                // 3. Kiểm tra kích thước
                int minDim = Math.Min(current.Width, current.Height);
                if (minDim < 400)
                {
                    double scale = 400.0 / minDim;
                    var upscaled = UpscaleSmall(current, scale);
                    current.Dispose();
                    current = upscaled;
                }

                // 4. Kiểm tra nghiêng (tốn thời gian, có thể bỏ qua)
                // var deskewed = CorrectSkew(current);
                // current.Dispose();
                // current = deskewed;

                return current;
            }
            catch
            {
                return (Bitmap)input.Clone();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Kiểm tra ảnh có tối không
        /// </summary>
        private static bool CheckIfDark(Mat image, double threshold = 100)
        {
            using Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            var mean = Cv2.Mean(gray);
            return mean.Val0 < threshold;
        }

        /// <summary>
        /// Kiểm tra ảnh có mờ không (Laplacian variance)
        /// </summary>
        private static bool CheckIfBlurry(Mat image, double threshold = 100)
        {
            using Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            using Mat laplacian = new Mat();
            Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

            Cv2.MeanStdDev(laplacian, out _, out var stddev);
            double variance = stddev.Val0 * stddev.Val0;

            return variance < threshold;
        }

        /// <summary>
        /// Sắp xếp 4 điểm theo thứ tự: TL, TR, BR, BL
        /// </summary>
        private static Point2f[] SortPoints(Point2f[] points)
        {
            // Tính tổng và hiệu
            var sorted = new Point2f[4];

            // Top-left: tổng nhỏ nhất
            // Bottom-right: tổng lớn nhất
            var sums = new float[4];
            for (int i = 0; i < 4; i++)
                sums[i] = points[i].X + points[i].Y;

            int tlIdx = Array.IndexOf(sums, sums.Min());
            int brIdx = Array.IndexOf(sums, sums.Max());

            sorted[0] = points[tlIdx]; // TL
            sorted[2] = points[brIdx]; // BR

            // Top-right: hiệu lớn nhất (x - y)
            // Bottom-left: hiệu nhỏ nhất
            var diffs = new float[4];
            for (int i = 0; i < 4; i++)
                diffs[i] = points[i].X - points[i].Y;

            int trIdx = Array.IndexOf(diffs, diffs.Max());
            int blIdx = Array.IndexOf(diffs, diffs.Min());

            sorted[1] = points[trIdx]; // TR
            sorted[3] = points[blIdx]; // BL

            return sorted;
        }

        /// <summary>
        /// Tính khoảng cách giữa 2 điểm
        /// </summary>
        private static float Distance(Point2f p1, Point2f p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Convert Bitmap to Mat
        /// </summary>
        private static Mat BitmapToMat(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
        }

        /// <summary>
        /// Convert Mat to Bitmap
        /// </summary>
        private static Bitmap MatToBitmap(Mat mat)
        {
            int w = mat.Width;
            int h = mat.Height;
            int channels = mat.Channels();

            // Nếu 1 kênh dùng Format8bppIndexed, nếu 3 kênh dùng Format24bppRgb
            PixelFormat format = (channels == 1) ? PixelFormat.Format8bppIndexed : PixelFormat.Format24bppRgb;
            Bitmap bmp = new Bitmap(w, h, format);

            if (channels == 1)
            {
                // Cấu hình Palette cho ảnh xám 8-bit
                ColorPalette palette = bmp.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
                bmp.Palette = palette;
            }

            var rect = new Rectangle(0, 0, w, h);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

            try
            {
                int stride = bmpData.Stride;
                int rowLength = w * channels;
                byte[] buffer = new byte[rowLength];

                for (int y = 0; y < h; y++)
                {
                    IntPtr srcPtr = mat.Data + y * (int)mat.Step();
                    System.Runtime.InteropServices.Marshal.Copy(srcPtr, buffer, 0, rowLength);

                    IntPtr dstPtr = bmpData.Scan0 + y * stride;
                    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, dstPtr, rowLength);
                }
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        #endregion
    }
}
