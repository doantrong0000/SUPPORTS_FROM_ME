using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjectApp.ModelFromCad
{
    /// <summary>
    /// Class tiện ích để kết nối với ứng dụng đang chạy thông qua COM (Component Object Model)
    /// Dùng để lấy instance của AutoCAD đang mở.
    /// </summary>
    public static class ComRunningObject
    {
        // Import hàm từ thư viện hệ thống ole32.dll để lấy Class ID từ ProgID
        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        private static extern int CLSIDFromProgIDEx(string progId, out Guid clsid);

        // Import hàm từ oleaut32.dll để lấy Active Object
        [DllImport("oleaut32.dll")]
        public static extern int GetActiveObject(ref Guid clsid, IntPtr reserved, out IntPtr punk);

        /// <summary>
        /// Lấy đối tượng đang chạy dựa trên ProgId (ví dụ "AutoCAD.Application")
        /// </summary>
        /// <param name="progId">Tên định danh chương trình</param>
        /// <returns>Đối tượng COM instance</returns>
        public static object GetActiveObjectByProgId(string progId)
        {
            if (string.IsNullOrWhiteSpace(progId))
                throw new ArgumentNullException(nameof(progId));

            // Lấy CLSID từ tên ProgID
            int hr = CLSIDFromProgIDEx(progId, out var clsid);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);

            // Lấy con trỏ tới active object
            hr = GetActiveObject(ref clsid, IntPtr.Zero, out var punk);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);

            try
            {
                // Chuyển đổi con trỏ thành đối tượng .NET
                return Marshal.GetObjectForIUnknown(punk);
            }
            finally
            {
                // Giải phóng con trỏ
                Marshal.Release(punk);
            }
        }
    }
}
