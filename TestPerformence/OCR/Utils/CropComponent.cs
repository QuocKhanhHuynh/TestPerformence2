using demo_ocr_label;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Drawing;

namespace demo_ocr_label
{
    public static class CropComponent
    {
        // C?t 2 vùng (góc du?i bên trái + vùng phía trên QR) r?i ghép ?nh l?i (KHÔNG OCR)
        public static Bitmap CropAndMergeBottomLeftAndAboveQr(Bitmap aligned, OpenCvSharp.Point[] qrBox)
        {
            Bitmap bottomLeftCrop = null;
            Bitmap aboveQrCrop = null;
            Bitmap mergedCrop = null;

            try
            {
                if (aligned == null || qrBox == null || qrBox.Length != 4)
                    return null;

                int width = aligned.Width;
                int height = aligned.Height;

                // Clone để tránh lỗi GDI+
                Bitmap safeAligned = aligned.Clone(
                    new Rectangle(0, 0, aligned.Width, aligned.Height),
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                using var mat = LabelDetector.BitmapToMat(safeAligned);

                // === 1) Vùng góc dưới bên trái ===
                Rectangle roiBottomLeft = new Rectangle(
                    0,
                    (int)(height * (1 - utils.fileConfig.bottomLeftComponent.height)),
                    (int)(width * utils.fileConfig.bottomLeftComponent.width),
                    (int)(height * utils.fileConfig.bottomLeftComponent.height)
                );
                roiBottomLeft.Intersect(new Rectangle(0, 0, width, height));
                if (roiBottomLeft.Width <= 0 || roiBottomLeft.Height <= 0)
                {
                    Debug.WriteLine("[??] ROI BottomLeft invalid: " + roiBottomLeft);
                    safeAligned.Dispose();
                    return null;
                }
                bottomLeftCrop = safeAligned.Clone(roiBottomLeft, safeAligned.PixelFormat);

                // === 2) Vùng phía trên cạnh QR (ĐÃ SỬA ĐỔI ĐIỀU CHỈNH) ===
                var p0 = qrBox[0]; // top-left
                var p1 = qrBox[1]; // top-right
                var p2 = qrBox[2]; // bottom-right

                var topVec = new OpenCvSharp.Point2f(p1.X - p0.X, p1.Y - p0.Y);
                var rightVec = new OpenCvSharp.Point2f(p2.X - p1.X, p2.Y - p1.Y);
                float qrWidth = (float)Math.Sqrt(topVec.X * topVec.X + topVec.Y * topVec.Y);
                float qrHeight = (float)Math.Sqrt(rightVec.X * rightVec.X + rightVec.Y * rightVec.Y);

                // --- CÁC HỆ SỐ ĐIỀU CHỈNH MỚI ---
                // Sử dụng giá trị cấu hình gốc, sau đó điều chỉnh chúng.
                float currentHeightRatio = utils.fileConfig.aboveQrComponent.height;
                float currentWidthRatio = utils.fileConfig.aboveQrComponent.width;
                float currentShiftUpRatio = utils.fileConfig.aboveQrComponent.doiTamLenTren;
                float currentShiftRightRatio = utils.fileConfig.aboveQrComponent.doiTamSangPhai;

                // 1. Tăng chiều cao (Ví dụ: tăng thêm 20% so với kích thước cũ)
                float newHeightRatio = currentHeightRatio * 2.0f;

                // 2. Tăng chiều rộng (Ví dụ: tăng thêm 15% so với kích thước cũ)
                float newWidthRatio = currentWidthRatio * 2.0f;

                // 3. Dịch chuyển tâm vùng cắt SANG TRÁI (Ví dụ: dịch 10% chiều rộng QR)
                float newShiftRightRatio = currentShiftRightRatio + 0.5f;
                // 4. Dịch chuyển tâm vùng cắt LÊN TRÊN (Để vùng cắt cao hơn, ví dụ: dịch thêm 5% chiều rộng QR)
                float newShiftUpRatio = currentShiftUpRatio + 0.2f;
                // ---------------------------------

                var normal = new OpenCvSharp.Point2f(topVec.Y, -topVec.X);
                float len = (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
                if (len != 0) { normal.X /= len; normal.Y /= len; }

                // Áp dụng các giá trị đã điều chỉnh
                float offset = newShiftUpRatio * qrWidth; // Dịch chuyển lên trên
                float widthAbove = newWidthRatio * qrWidth; // Chiều rộng mới
                float heightAbove = newHeightRatio * qrHeight; // Chiều cao mới
                float shiftDist = newShiftRightRatio * qrWidth; // Dịch chuyển ngang (âm = sang trái)

                var dir = new OpenCvSharp.Point2f(topVec.X / qrWidth, topVec.Y / qrWidth);

                // Tính điểm Top-Right của vùng cắt mới
                var baseTopRight = new OpenCvSharp.Point2f(
                    p1.X + normal.X * offset + dir.X * shiftDist,
                    p1.Y + normal.Y * offset + dir.Y * shiftDist);

                var rectTopRight = baseTopRight;
                var rectTopLeft = new OpenCvSharp.Point2f(
                    rectTopRight.X - dir.X * widthAbove,
                    rectTopRight.Y - dir.Y * widthAbove);

                var rectBottomRight = new OpenCvSharp.Point2f(
                    rectTopRight.X + normal.X * heightAbove,
                    rectTopRight.Y + normal.Y * heightAbove);

                var rectBottomLeft = new OpenCvSharp.Point2f(
                    rectTopLeft.X + normal.X * heightAbove,
                    rectTopLeft.Y + normal.Y * heightAbove);

                OpenCvSharp.Point2f[] srcQuad =
                {
            rectTopLeft,
            rectTopRight,
            rectBottomRight,
            rectBottomLeft
        };

                // Vùng đích luôn là hình chữ nhật
                OpenCvSharp.Point2f[] dstQuad =
                {
            new(0, heightAbove),
            new(widthAbove, heightAbove),
            new(widthAbove, 0),
            new(0, 0)
        };

                var M = Cv2.GetPerspectiveTransform(srcQuad, dstQuad);
                using var croppedTopRight = new Mat();
                Cv2.WarpPerspective(mat, croppedTopRight, M, new OpenCvSharp.Size(widthAbove, heightAbove),
                    InterpolationFlags.Linear, BorderTypes.Replicate);

                // Convert sang Bitmap
                aboveQrCrop = LabelDetector.MatToBitmap(croppedTopRight);

                // Resize vùng aboveQrCrop lên gấp 2 lần bằng OpenCV
                using var aboveQrMat = LabelDetector.BitmapToMat(aboveQrCrop);
                using var resizedMat = new Mat();
                
                int newWidth = aboveQrMat.Width * 2;
                int newHeight = aboveQrMat.Height * 2;
                
                // Sử dụng INTER_CUBIC cho chất lượng tốt khi upscale
                Cv2.Resize(aboveQrMat, resizedMat, new OpenCvSharp.Size(newWidth, newHeight), 
                    interpolation: InterpolationFlags.Cubic);
                
                // Dispose ảnh gốc và dùng ảnh đã resize
                aboveQrCrop.Dispose();
                aboveQrCrop = LabelDetector.MatToBitmap(resizedMat);

                // === 3) Ghép ảnh ===
                int mergedWidth = Math.Max(aboveQrCrop.Width, bottomLeftCrop.Width);
                int mergedHeight = aboveQrCrop.Height + bottomLeftCrop.Height;

                mergedCrop = new Bitmap(mergedWidth, mergedHeight);
                using (Graphics g = Graphics.FromImage(mergedCrop))
                {
                    g.Clear(System.Drawing.Color.Black);
                    using (Bitmap topClone = (Bitmap)aboveQrCrop.Clone())
                    using (Bitmap bottomClone = (Bitmap)bottomLeftCrop.Clone())
                    {
                        g.DrawImage(topClone, (mergedWidth - topClone.Width) / 2, 0);
                        g.DrawImage(bottomClone, (mergedWidth - bottomClone.Width) / 2, topClone.Height);
                    }
                }

                // Cleanup tạm
                aboveQrCrop?.Dispose();
                bottomLeftCrop?.Dispose();
                safeAligned.Dispose();

                return mergedCrop;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[? CropComponent ERROR] {ex.Message}");
                try { aboveQrCrop?.Dispose(); bottomLeftCrop?.Dispose(); mergedCrop?.Dispose(); } catch { }
                return null;
            }
        }
        //public static Bitmap CropAndMergeBottomLeftAndAboveQr(Bitmap aligned, OpenCvSharp.Point[] qrBox)
        //{
        //    Bitmap bottomLeftCrop = null;
        //    Bitmap aboveQrCrop = null;
        //    Bitmap mergedCrop = null;

        //    try
        //    {
        //        if (aligned == null || qrBox == null || qrBox.Length != 4)
        //            return null;

        //        int width = aligned.Width;
        //        int height = aligned.Height;

        //        // Clone d? tránh l?i GDI+
        //        Bitmap safeAligned = aligned.Clone(
        //            new Rectangle(0, 0, aligned.Width, aligned.Height),
        //            System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        //        using var mat = LabelDetector.BitmapToMat(safeAligned);

        //        // === 1) Vùng góc du?i bên trái ===
        //        Rectangle roiBottomLeft = new Rectangle(
        //            0,
        //            (int)(height * (1 - utils.fileConfig.bottomLeftComponent.height)),
        //            (int)(width * utils.fileConfig.bottomLeftComponent.width),
        //            (int)(height * utils.fileConfig.bottomLeftComponent.height)
        //        );
        //        roiBottomLeft.Intersect(new Rectangle(0, 0, width, height));
        //        if (roiBottomLeft.Width <= 0 || roiBottomLeft.Height <= 0)
        //        {
        //            Debug.WriteLine("[??] ROI BottomLeft invalid: " + roiBottomLeft);
        //            safeAligned.Dispose();
        //            return null;
        //        }
        //        bottomLeftCrop = safeAligned.Clone(roiBottomLeft, safeAligned.PixelFormat);

        //        // === 2) Vùng phía trên c?nh QR ===
        //        var p0 = qrBox[0]; // top-left
        //        var p1 = qrBox[1]; // top-right
        //        var p2 = qrBox[2]; // bottom-right

        //        var topVec = new OpenCvSharp.Point2f(p1.X - p0.X, p1.Y - p0.Y);
        //        var rightVec = new OpenCvSharp.Point2f(p2.X - p1.X, p2.Y - p1.Y);
        //        float qrWidth = (float)Math.Sqrt(topVec.X * topVec.X + topVec.Y * topVec.Y);
        //        float qrHeight = (float)Math.Sqrt(rightVec.X * rightVec.X + rightVec.Y * rightVec.Y);

        //        var normal = new OpenCvSharp.Point2f(topVec.Y, -topVec.X);
        //        float len = (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
        //        if (len != 0) { normal.X /= len; normal.Y /= len; }

        //        float offset = (float)(utils.fileConfig.aboveQrComponent.doiTamLenTren * qrWidth);
        //        float widthAbove = (float)(utils.fileConfig.aboveQrComponent.width * qrWidth);
        //        float heightAbove = (float)(utils.fileConfig.aboveQrComponent.height * qrHeight);

        //        var dir = new OpenCvSharp.Point2f(topVec.X / qrWidth, topVec.Y / qrWidth);
        //        float shiftDist = utils.fileConfig.aboveQrComponent.doiTamSangPhai * qrWidth;

        //        var baseTopRight = new OpenCvSharp.Point2f(
        //            p1.X + normal.X * offset + dir.X * shiftDist,
        //            p1.Y + normal.Y * offset + dir.Y * shiftDist);

        //        var rectTopRight = baseTopRight;
        //        var rectTopLeft = new OpenCvSharp.Point2f(
        //            rectTopRight.X - dir.X * widthAbove,
        //            rectTopRight.Y - dir.Y * widthAbove);

        //        var rectBottomRight = new OpenCvSharp.Point2f(
        //            rectTopRight.X + normal.X * heightAbove,
        //            rectTopRight.Y + normal.Y * heightAbove);

        //        var rectBottomLeft = new OpenCvSharp.Point2f(
        //            rectTopLeft.X + normal.X * heightAbove,
        //            rectTopLeft.Y + normal.Y * heightAbove);

        //        OpenCvSharp.Point2f[] srcQuad =
        //        {
        //            rectTopLeft,
        //            rectTopRight,
        //            rectBottomRight,
        //            rectBottomLeft
        //        };
        //        OpenCvSharp.Point2f[] dstQuad =
        //        {
        //            new(0, heightAbove),
        //            new(widthAbove, heightAbove),
        //            new(widthAbove, 0),
        //            new(0, 0)
        //        };

        //        var M = Cv2.GetPerspectiveTransform(srcQuad, dstQuad);
        //        using var croppedTopRight = new Mat();
        //        Cv2.WarpPerspective(mat, croppedTopRight, M, new OpenCvSharp.Size(widthAbove, heightAbove),
        //            InterpolationFlags.Linear, BorderTypes.Replicate);

        //        // Convert sang Bitmap
        //        aboveQrCrop = LabelDetector.MatToBitmap(croppedTopRight);

        //        // === 3) Ghép ?nh ===
        //        int mergedWidth = Math.Max(aboveQrCrop.Width, bottomLeftCrop.Width);
        //        int mergedHeight = aboveQrCrop.Height + bottomLeftCrop.Height;

        //        mergedCrop = new Bitmap(mergedWidth, mergedHeight);
        //        using (Graphics g = Graphics.FromImage(mergedCrop))
        //        {
        //            g.Clear(System.Drawing.Color.Black);
        //            using (Bitmap topClone = (Bitmap)aboveQrCrop.Clone())
        //            using (Bitmap bottomClone = (Bitmap)bottomLeftCrop.Clone())
        //            {
        //                g.DrawImage(topClone, (mergedWidth - topClone.Width) / 2, 0);
        //                g.DrawImage(bottomClone, (mergedWidth - bottomClone.Width) / 2, topClone.Height);
        //            }
        //        }

        //        // Cleanup t?m
        //        aboveQrCrop?.Dispose();
        //        bottomLeftCrop?.Dispose();
        //        safeAligned.Dispose();

        //        return mergedCrop;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"[? CropComponent ERROR] {ex.Message}");
        //        try { aboveQrCrop?.Dispose(); bottomLeftCrop?.Dispose(); mergedCrop?.Dispose(); } catch { }
        //        return null;
        //    }
        //}
    }
}