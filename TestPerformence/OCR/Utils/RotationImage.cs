using OpenCvSharp;
using Sdcb.RotationDetector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    public static class RotationImage
    {
        public static RotatedRect CreateRotatedBBox(List<(int x, int y)> marks)
        {
            // Chuyển dữ liệu sang Point2f
            Point2f[] pts = marks
                .Select(m => new Point2f(m.x, m.y))
                .ToArray();

            // MinAreaRect: tính bounding box xoay
            RotatedRect rr = Cv2.MinAreaRect(pts);

            return rr;
        }

        public static Mat CropAndRotateMask(Mat imgOriginal, RotatedRect rotatedRect)
        {
            // 1. CHUẨN HÓA GÓC VÀ KÍCH THƯỚC ĐÍCH
            OpenCvSharp.Point2f centerOriginal = rotatedRect.Center;
            OpenCvSharp.Size2f size = rotatedRect.Size;
            double angle = rotatedRect.Angle;

            // Chuẩn hóa góc: Đảm bảo cạnh dài nhất nằm ngang (Angle ~ 0 hoặc ~-180)
            if (size.Width < size.Height)
            {
                angle += 90.0;
                // Hoán đổi kích thước đích
                float temp = size.Width;
                size.Width = size.Height;
                size.Height = temp;
            }

            // 2. CẮT ẢNH ĐỨNG SƠ BỘ (Bounding Rect bao quanh Rotated Rect)
            Rect boundingBox = rotatedRect.BoundingRect();
            Mat preCroppedImg = new Mat(imgOriginal, boundingBox);

            // 3. ĐIỀU CHỈNH TÂM XOAY
            // Tâm xoay phải là tâm vật thể so với góc trên bên trái của ảnh đã cắt
            OpenCvSharp.Point2f centerOfRotation = new OpenCvSharp.Point2f(
                centerOriginal.X - boundingBox.X,
                centerOriginal.Y - boundingBox.Y
            );

            // 4. TẠO MA TRẬN XOAY VÀ ÁP DỤNG
            Mat rotMatrix = Cv2.GetRotationMatrix2D(centerOfRotation, angle, 1.0);
            Mat rotatedImg = new Mat();

            // Kích thước ảnh xoay phải đủ lớn để chứa toàn bộ vật thể sau khi xoay
            // Ta sử dụng kích thước của Bounding Rect (ảnh đã cắt) cho đơn giản, chấp nhận mất góc nếu góc xoay lớn
            Cv2.WarpAffine(preCroppedImg, rotatedImg, rotMatrix, preCroppedImg.Size());

            // 5. CẮT ẢNH CUỐI CÙNG THEO ROTATED RECT ĐÃ CĂN CHỈNH

            // finalRect sẽ là ROI trên ảnh rotatedImg: 
            // Về lý thuyết, nó phải là hình chữ nhật đứng (Rect) với kích thước size.Width x size.Height
            // và nằm tại tâm centerOfRotation

            // Tính toán ROI chính xác (kích thước size đã chuẩn hóa, nằm ở tâm đã dịch chuyển)
            Rect finalCropRoi = new Rect(
                (int)Math.Round(centerOfRotation.X - size.Width / 2f),
                (int)Math.Round(centerOfRotation.Y - size.Height / 2f),
                (int)Math.Round(size.Width),
                (int)Math.Round(size.Height)
            );

            // Đảm bảo ROI không vượt ra ngoài biên của ảnh đã xoay (rotatedImg)
            if (finalCropRoi.X < 0 || finalCropRoi.Y < 0 ||
                finalCropRoi.X + finalCropRoi.Width > rotatedImg.Width ||
                finalCropRoi.Y + finalCropRoi.Height > rotatedImg.Height)
            {
                // Nếu ROI không hợp lệ do lỗi làm tròn hoặc ảnh quá nhỏ, trả về ảnh đã xoay
                return rotatedImg;
            }

            return new Mat(rotatedImg, finalCropRoi);
        }


        /// <summary>
        /// Display image with visualization of contours and bounding boxes (Đã tích hợp xoay và cắt)
        /// </summary>
        public static Mat ProcessRotationImage(Mat img, List<(int x, int y)> mask)
        {
            RotatedRect rotatedRect = CreateRotatedBBox(mask);

            // 2. CẮT VÀ XOAY ẢNH THEO ROTATED BBOX
            Mat resultMat = CropAndRotateMask(img, rotatedRect);

            return resultMat;


            //System.Drawing.Bitmap bitmap = BitmapConverter.ToBitmap(resultMat); // Hiển thị ảnh đã xoay và cắt
            //ptb.Image = bitmap;

            //// Cleanup
            //img.Dispose();
            //resultMat.Dispose();
            // preCroppedImg và rotatedImg đã được giải phóng trong hàm CropAndRotateMask nếu cần

        }


        /// <summary>
        /// Kiểm tra và trả về góc xoay của văn bản trong ảnh (0, 90, 180, 270 độ)
        /// </summary>
        /// <param name="img">Ảnh đầu vào cần kiểm tra</param>
        /// <returns>Góc xoay của văn bản (0, 90, 180, hoặc 270 độ)</returns>
        public static RotationDegree CheckLabelRotation(Mat img, PaddleRotationDetector rotatioDetector)
        {
            RotationResult r = rotatioDetector.Run(img);
            return r.Rotation;
        }

        public static Mat Rotate(Mat src, RotationDegree angle)
        {
            Mat dst = new Mat();

            switch (angle)
            {
                case RotationDegree._90:
                    Cv2.Rotate(src, dst, RotateFlags.Rotate90Clockwise);
                    break;
                case RotationDegree._180:
                    Cv2.Rotate(src, dst, RotateFlags.Rotate180);
                    break;
                case RotationDegree._270:
                    Cv2.Rotate(src, dst, RotateFlags.Rotate90Counterclockwise);
                    break;
                default:
                    dst = src.Clone();
                    break;
            }

            return dst;
        }

    }
}
