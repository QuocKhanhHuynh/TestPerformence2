using System;
using System.Diagnostics;

namespace demo_ocr_label
{
    public static class utils
    {
        // Config mặc định - không còn đọc từ file JSON nữa
        public static Config fileConfig = new Config();
        
        // Static constructor - khởi tạo config mặc định khi class được load lần đầu
        static utils()
        {
            Debug.WriteLine("Khởi tạo OCR config mặc định (không dùng file JSON)");
        }
        
        // Giữ lại method này để tương thích với code cũ, nhưng không làm gì
        [Obsolete("Không còn dùng file JSON nữa, config mặc định được khởi tạo tự động")]
        public static void LoadConfigFile(string configFileName)
        {
            // Không làm gì - config đã được khởi tạo mặc định
            Debug.WriteLine("LoadConfigFile được gọi nhưng không còn dùng file JSON nữa");
        }
    }
}
