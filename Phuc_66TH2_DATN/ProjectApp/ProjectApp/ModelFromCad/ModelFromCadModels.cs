using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectApp.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectApp.ModelFromCad
{
    /// <summary>
    /// Lớp chứa thông tin text và tọa độ từ CAD
    /// </summary>
    public class TextData
    {
        public XYZ point;
        public string text;
    }

    /// <summary>
    /// Lớp đại diện tọa độ XYZ tùy chỉnh
    /// </summary>
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

        public XyzData Mid(XyzData other)
        {
            var a = (X + other.X) / 2;
            var b = (Y + other.Y) / 2;
            var c = (Z + other.Z) / 2;
            return new XyzData(a, b, c);
        }
    }

    /// <summary>
    /// Lớp đại diện dầm lấy từ CAD
    /// </summary>
    public class CadBeams
    {
        public XyzData StartPoint { get; set; }
        public XyzData EndPoint { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Lớp đại diện hình chữ nhật từ CAD (dùng cho cột)
    /// </summary>
    public class CadRectangle
    {
        public XyzData P1 { get; set; }
        public XyzData P2 { get; set; }
        public XyzData P3 { get; set; }
        public XyzData P4 { get; set; }

        public string Mask;

        public List<XyzData> Points => new() { P1, P2, P3, P4 };
    }

    /// <summary>
    /// Collection chứa thông tin nhóm cột
    /// </summary>
    public class ColumnInfoCollection : ObservableObject
    {
        public List<ColumnInfo> ColumnInfos { get; set; } = new List<ColumnInfo>();

        private double _width;
        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged();
            }
        }

        private double _height;
        public double Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged();
            }
        }

        private string text;
        public string Text
        {
            get => text;
            set
            {
                text = value;
                OnPropertyChanged();
            }
        }

        public ElementType ElementType { get; set; }

        private FamilySymbol _selectedType;
        public FamilySymbol SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
            }
        }

        public int Number { get; set; }

        public ColumnInfoCollection()
        {
        }
    }

    /// <summary>
    /// Thông tin chi tiết một cột
    /// </summary>
    public class ColumnInfo
    {
        public XYZ Center { get; set; }
        public Line WidthLine { get; set; }
        public Line HeightLine { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotation { get; set; }
        public string Text { get; set; }

        public ColumnInfo(List<XYZ> points, string text)
        {
            GetInfo(points);
            Text = text;
        }

        public ColumnInfo()
        {
        }

        private void GetInfo(List<XYZ> points)
        {
            // Tính tâm cột
            Center = new XYZ(points.Average(x => x.X), points.Average(x => x.Y), points.Average(x => x.Z));
            var p1 = points[0];
            var p2 = points[1];
            var p3 = points[2];
            var p4 = points[3];
            var l1 = Line.CreateBound(p1, p2);
            var l2 = Line.CreateBound(p2, p3);

            // Xác định cạnh dài (Height) và cạnh ngắn (Width)
            if (l1.Length >= l2.Length)
            {
                HeightLine = l1;
                WidthLine = l2;
            }
            else
            {
                HeightLine = l2;
                WidthLine = l1;
            }

            var direction = HeightLine.Direction;

            if (direction.Y < 0)
            {
                direction = -direction;
            }

            Width = Math.Round(WidthLine.Length.FootToMm(), 1);
            Height = Math.Round(HeightLine.Length.FootToMm(), 1);
            Rotation = new XYZ(direction.X, direction.Y, 0).AngleTo(XYZ.BasisY);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            return obj is ColumnInfo;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    /// <summary>
    /// Collection chứa thông tin sàn
    /// </summary>
    public class FloorInfoCollection : ObservableObject
    {
        private double _area;
        public double Area
        {
            get => _area;
            set
            {
                _area = value;
                OnPropertyChanged();
            }
        }
    }
}
