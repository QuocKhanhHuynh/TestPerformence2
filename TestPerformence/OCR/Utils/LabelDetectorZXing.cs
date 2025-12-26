using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    public static class LabelDetectorZXing
    {
        /// <summary>
        /// Phát hiện QR code trong ảnh sử dụng thư viện ZXing
        /// </summary>
        /// <param name="roi">Vùng ảnh cần tìm QR code</param>
        /// <returns>Tọa độ 4 điểm của QR code (Point2f[]) nếu tìm thấy, null nếu không tìm thấy</returns>
        public static (Point2f[]? qrPoints, string qrText) DetectQRCodeZXing(Bitmap roi, ZXing.Windows.Compatibility.BarcodeReader reader)
        {
            if (roi == null || reader == null)
                return (null, null);

            try
            {
                // Khởi tạo ZXing reader cho Bitmap
                /*var reader = new ZXing.Windows.Compatibility.BarcodeReader
                {
                    AutoRotate = true,
                    TryInverted = true,
                    Options = new DecodingOptions
                    {
                        PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                        TryHarder = true,
                    }
                };*/

                // Decode QR code trực tiếp từ Bitmap
                var result = reader.Decode(roi);

                if (result != null && !string.IsNullOrEmpty(result.Text))
                {
                    // ZXing trả về các ResultPoint, ta cần convert sang Point2f[]
                    if (result.ResultPoints != null && result.ResultPoints.Length >= 3)
                    {
                        // ZXing thường trả về 3 hoặc 4 điểm (finder patterns)
                        // Nếu có 3 điểm, ta cần tính điểm thứ 4
                        Point2f[] qrPoints;

                        if (result.ResultPoints.Length == 4)
                        {
                            // Đã có đủ 4 điểm
                            qrPoints = new Point2f[4];
                            for (int i = 0; i < 4; i++)
                            {
                                qrPoints[i] = new Point2f(result.ResultPoints[i].X, result.ResultPoints[i].Y);
                            }
                        }
                        else if (result.ResultPoints.Length == 3)
                        {
                            // 1. Lấy 3 điểm Finder Patterns dưới dạng Point2f
                            var pA = new Point2f(result.ResultPoints[0].X, result.ResultPoints[0].Y);
                            var pB = new Point2f(result.ResultPoints[1].X, result.ResultPoints[1].Y);
                            var pC = new Point2f(result.ResultPoints[2].X, result.ResultPoints[2].Y);

                            // Dùng List để dễ dàng tìm kiếm
                            var points = new List<Point2f> { pA, pB, pC };

                            // 2. Tính khoảng cách bình phương (squared distance) giữa các cặp điểm.
                            // Dùng khoảng cách bình phương để tránh căn bậc hai (sqrt) giúp tính toán nhanh hơn
                            double dAB = Math.Pow(pA.X - pB.X, 2) + Math.Pow(pA.Y - pB.Y, 2);
                            double dBC = Math.Pow(pB.X - pC.X, 2) + Math.Pow(pB.Y - pC.Y, 2);
                            double dCA = Math.Pow(pC.X - pA.X, 2) + Math.Pow(pC.Y - pA.Y, 2);

                            // 3. Xác định điểm Top-Left (pTL)
                            // Cạnh dài nhất (đường chéo) nối Top-Right và Bottom-Left. 
                            // Điểm còn lại là Top-Left (pTL).
                            Point2f pTL, pTR, pBL, pBR;

                            if (dAB > dBC && dAB > dCA)
                            {
                                // Cạnh AB là dài nhất. Điểm C là pTL.
                                pTL = pC;
                                points.Remove(pC);
                            }
                            else if (dBC > dAB && dBC > dCA)
                            {
                                // Cạnh BC là dài nhất. Điểm A là pTL.
                                pTL = pA;
                                points.Remove(pA);
                            }
                            else // dCA là dài nhất
                            {
                                // Cạnh CA là dài nhất. Điểm B là pTL.
                                pTL = pB;
                                points.Remove(pB);
                            }

                            // 4. Phân loại hai điểm còn lại (pTR và pBL)
                            // Sau khi tìm thấy pTL, hai điểm còn lại là points[0] và points[1].
                            // Trong ảnh đã được căn chỉnh (aligned), pTR luôn nằm gần phía trên/bên phải hơn pBL.

                            // Ta chọn điểm có tọa độ Y nhỏ hơn (gần đỉnh ảnh hơn) làm pTR
                            if (points[0].Y < points[1].Y)
                            {
                                pTR = points[0];
                                pBL = points[1];
                            }
                            else
                            {
                                pTR = points[1];
                                pBL = points[0];
                            }

                            // 5. Tính điểm thứ 4 (pBR - Bottom-Right)
                            // Dùng hình học vector: pBR = pTR - pTL + pBL
                            pBR = new Point2f(
                                pTR.X - pTL.X + pBL.X,
                                pTR.Y - pTL.Y + pBL.Y
                            );

                            // 6. Sắp xếp lại theo thứ tự yêu cầu (TL, TR, BR, BL)
                            qrPoints = new Point2f[] { pTL, pTR, pBR, pBL };
                        }
                        else
                        {
                            return (null, null);
                        }

                        return (qrPoints, result.Text);
                    }
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetectQRCodeZXing ERROR] {ex.Message}");
                return (null, null);
            }
        }
    }
}
