using System;
using System.Collections.Generic;

namespace DATN_AUTO_CREATE_PART.Models
{
    public class TextData
    {
        public XyzData Point { get; set; }
        public string Text { get; set; }
    }

    public class XyzData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public XyzData(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistanceTo(XyzData other)
        {
            return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2) + Math.Pow(Z - other.Z, 2));
        }

        public XyzData Mid(XyzData other)
        {
            return new XyzData((X + other.X) / 2, (Y + other.Y) / 2, (Z + other.Z) / 2);
        }
    }

    public class CadBeams
    {
        public XyzData StartPoint { get; set; }
        public XyzData EndPoint { get; set; }
        public string Text { get; set; }
    }

    public class CadRectangle
    {
        public XyzData P1 { get; set; }
        public XyzData P2 { get; set; }
        public XyzData P3 { get; set; }
        public XyzData P4 { get; set; }

        public string Mask { get; set; }

        public List<XyzData> Points => new List<XyzData> { P1, P2, P3, P4 };
    }

    public class CadFloor
    {
        public List<XyzData> Points { get; set; } = new List<XyzData>();
        public double Area { get; set; }
    }
}
