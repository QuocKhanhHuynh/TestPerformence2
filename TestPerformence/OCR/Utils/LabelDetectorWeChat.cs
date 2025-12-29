using OpenCvSharp;
using OpenCvSharp.Text;
using System;
using System.Diagnostics;
using System.Drawing;

namespace GarmentGridApp.Presentation.OCR.Utils
{
    /// <summary>
    /// QR Code detector sử dụng OpenCV WeChatQRCode (Deep Learning based)
    /// </summary>
    public static class LabelDetectorWeChat
    {
        /// <summary>
        /// Phát hiện QR code trong ảnh sử dụng WeChatQRCode detector
        /// </summary>
        /// <param name="image">Ảnh bitmap cần tìm QR code</param>
        /// <param name="weChatQRCode">Instance của WeChatQRCode detector</param>
        /// <returns>Tọa độ 4 điểm của QR code (Point2f[]) và text nếu tìm thấy, null nếu không tìm thấy</returns>
        public static (Point2f[]? qrPoints, string? qrText) DetectQRCodeWeChat(Bitmap image, WeChatQRCode weChatQRCode)
        {
            if (image == null || weChatQRCode == null)
                return (null, null);

            Mat? inputMat = null;

            try
            {
                // Convert Bitmap to Mat
                inputMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(image);

                // WeChatQRCode.DetectAndDecode: void method with out Mat[] bbox, out string[] results
                weChatQRCode.DetectAndDecode(inputMat, out Mat[] qrPointMats, out string[] qrTexts);

                // Check if any QR code was detected
                if (qrTexts == null || qrTexts.Length == 0 || qrPointMats == null || qrPointMats.Length == 0)
                {
                    return (null, null);
                }

                // Get first QR code (if multiple detected)
                string qrText = qrTexts[0];

                // Convert Mat points to Point2f[]
                var pointsMat = qrPointMats[0];
                Point2f[]? qrPoints = null;

                if (pointsMat != null && !pointsMat.Empty())
                {
                    // WeChatQRCode returns points as Mat with shape (4, 2) or (4, 1, 2)
                    // Each row is a point with (x, y) coordinates
                    int numPoints = pointsMat.Rows;

                    if (numPoints == 4)
                    {
                        qrPoints = new Point2f[4];

                        // Check if Mat is (4, 2) or (4, 1, 2)
                        if (pointsMat.Dims == 2 && pointsMat.Cols == 2)
                        {
                            // Shape: (4, 2) - Direct x,y coordinates
                            for (int i = 0; i < 4; i++)
                            {
                                float x = pointsMat.At<float>(i, 0);
                                float y = pointsMat.At<float>(i, 1);
                                qrPoints[i] = new Point2f(x, y);
                            }
                        }
                        else if (pointsMat.Channels() == 2)
                        {
                            // Shape: (4, 1) with 2 channels
                            for (int i = 0; i < 4; i++)
                            {
                                var point = pointsMat.At<Vec2f>(i, 0);
                                qrPoints[i] = new Point2f(point.Item0, point.Item1);
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[WECHAT QR] Unexpected Mat format: Dims={pointsMat.Dims}, Rows={pointsMat.Rows}, Cols={pointsMat.Cols}, Channels={pointsMat.Channels()}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[WECHAT QR] Expected 4 points, got {numPoints}");
                    }
                }

                // Dispose Mat arrays to prevent memory leak
                foreach (var m in qrPointMats)
                {
                    m?.Dispose();
                }

                return (qrPoints, qrText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetectQRCodeWeChat ERROR] {ex.Message}");
                Debug.WriteLine($"[DetectQRCodeWeChat STACK] {ex.StackTrace}");
                return (null, null);
            }
            finally
            {
                // Cleanup
                inputMat?.Dispose();
            }
        }

        /// <summary>
        /// Phát hiện QR code từ Mat (OpenCV image)
        /// </summary>
        /// <param name="mat">Mat từ OpenCV</param>
        /// <param name="weChatQRCode">Instance của WeChatQRCode detector</param>
        /// <returns>Tọa độ 4 điểm của QR code và text</returns>
        public static (Point2f[]? qrPoints, string? qrText) DetectQRCodeWeChat(Mat mat, WeChatQRCode weChatQRCode)
        {
            if (mat == null || mat.Empty() || weChatQRCode == null)
                return (null, null);

            try
            {
                // Detect and decode directly from Mat
                weChatQRCode.DetectAndDecode(mat, out Mat[] qrPointMats, out string[] qrTexts);

                if (qrTexts == null || qrTexts.Length == 0 || qrPointMats == null || qrPointMats.Length == 0)
                {
                    return (null, null);
                }

                string qrText = qrTexts[0];
                var pointsMat = qrPointMats[0];
                Point2f[]? qrPoints = null;

                if (pointsMat != null && !pointsMat.Empty())
                {
                    int numPoints = pointsMat.Rows;

                    if (numPoints == 4)
                    {
                        qrPoints = new Point2f[4];

                        if (pointsMat.Dims == 2 && pointsMat.Cols == 2)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                float x = pointsMat.At<float>(i, 0);
                                float y = pointsMat.At<float>(i, 1);
                                qrPoints[i] = new Point2f(x, y);
                            }
                        }
                        else if (pointsMat.Channels() == 2)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                var point = pointsMat.At<Vec2f>(i, 0);
                                qrPoints[i] = new Point2f(point.Item0, point.Item1);
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[WECHAT QR] Unexpected Mat format: Dims={pointsMat.Dims}, Rows={pointsMat.Rows}, Cols={pointsMat.Cols}, Channels={pointsMat.Channels()}");
                        }
                    }
                }

                // Dispose Mat arrays
                foreach (var m in qrPointMats)
                {
                    m?.Dispose();
                }

                return (qrPoints, qrText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetectQRCodeWeChat ERROR] {ex.Message}");
                return (null, null);
            }
        }
    }
}
