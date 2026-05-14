using Autodesk.Revit.DB;
using ProjectApp.ModelFromCad;

namespace ProjectApp.Utils
{
    /// <summary>
    /// Các hàm tiện ích mở rộng (Extension Methods) dùng chung cho dự án
    /// </summary>
    public static class Utils
    {
        // Chuyển đổi dữ liệu tọa độ tùy chỉnh (XyzData) sang Revit XYZ (có đổi đơn vị mm -> feet)
        public static XYZ ToXyz(this XyzData data)
        {
            return new XYZ(data.X.MmToFoot(), data.Y.MmToFoot(), data.Z.MmToFoot());
        }

        // Chuyển đổi XYZ (đơn vị mm) sang XYZ (đơn vị feet)
        public static XYZ ToXyzfit(this XYZ data)
        {
            return new XYZ(data.X.MmToFoot(), data.Y.MmToFoot(), data.Z.MmToFoot());
        }

        /// <summary>
        /// Tìm phần tử có giá trị nhỏ nhất dựa trên một selector (tương tự MinBy của .NET 6+)
        /// </summary>
        public static tsource MinBy2<tsource, tkey>(
            this IEnumerable<tsource> source,
            Func<tsource, tkey> selector)
        {
            return source.MinBy2(selector, Comparer<tkey>.Default);
        }

        public static tsource MinBy2<tsource, tkey>(
            this IEnumerable<tsource> source,
            Func<tsource, tkey> selector,
            IComparer<tkey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            using (IEnumerator<tsource> sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                    throw new InvalidOperationException("Sequence was empty");
                tsource min = sourceIterator.Current;
                tkey minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    tsource candidate = sourceIterator.Current;
                    tkey candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }

                return min;
            }
        }

        // Chuyển milimet sang feet (Revit dùng feet làm đơn vị nội bộ)
        public static double MmToFoot(this double mm)
        {
            return mm / 304.8; // Cập nhật hệ số chính xác
        }

        // Chuyển feet sang milimet
        public static double FootToMm(this double ft)
        {
            return ft * 304.8;
        }

        // Tạo một điểm mới giữ nguyên X, Y và thay đổi Z
        public static XYZ EditZ(this XYZ p, double z)
        {
            return new XYZ(p.X, p.Y, z);
        }

        // Tạo một Line từ điểm và vector hướng (độ dài mặc định 1 đơn vị)
        public static Line CreateLineByPointAndDirection(this XYZ p, XYZ direction)
        {
            return Line.CreateBound(p, p.Add(direction));
        }

        // Chuyển đổi CurveArray (Revit API cũ) sang List<Curve>
        public static List<Curve> ToCurves(this CurveArray curveArray)
        {
            var result = new List<Curve>();
            if (curveArray == null) return result;

            foreach (Curve c in curveArray)
            {
                if (c != null) result.Add(c);
            }
            return result;
        }
    }
}
