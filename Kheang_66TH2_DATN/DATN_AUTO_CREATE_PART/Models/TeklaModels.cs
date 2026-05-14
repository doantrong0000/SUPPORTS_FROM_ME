using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DATN_AUTO_CREATE_PART.Models
{
    public class BeamInfo
    {
        public XyzData StartPoint { get; set; }
        public XyzData EndPoint { get; set; }
        public string Text { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        public BeamInfo(XyzData startPoint, XyzData endPoint, string text)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Text = text;
            ParseDimensionsFromText(text);
        }

        private void ParseDimensionsFromText(string text)
        {
            // Parse dimensions from text like "(250x400)", "(300x600)", "400x400)", "B250x400" etc.
            Width = 200;  // fallback defaults
            Height = 400;

            if (string.IsNullOrWhiteSpace(text)) return;

            var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)\s*[xX]\s*(\d+)");
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double w))
                    Width = w;
                if (double.TryParse(match.Groups[2].Value, out double h))
                    Height = h;
            }
        }

        public class BeamInfoComparerByPoint : IEqualityComparer<BeamInfo>
        {
            public bool Equals(BeamInfo x, BeamInfo y)
            {
                if (x == null || y == null) return false;
                return Math.Abs(x.StartPoint.X - y.StartPoint.X) < 1 &&
                       Math.Abs(x.StartPoint.Y - y.StartPoint.Y) < 1 &&
                       Math.Abs(x.EndPoint.X - y.EndPoint.X) < 1 &&
                       Math.Abs(x.EndPoint.Y - y.EndPoint.Y) < 1;
            }

            public int GetHashCode(BeamInfo obj)
            {
                return 0; // Force Equals call
            }
        }
    }

    public class BeamInfoCollection : ObservableObject
    {
        public List<BeamInfo> BeamInfos { get; set; } = new List<BeamInfo>();

        private double _width;
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private double _height;
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        private string _text;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        private int _number;
        public int Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }
    }

    public class ColumnInfo
    {
        public XyzData Center { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotation { get; set; }
        public string Text { get; set; }

        public ColumnInfo(List<XyzData> points, string text)
        {
            Text = text;
            GetInfo(points);
        }

        private void GetInfo(List<XyzData> points)
        {
            Center = new XyzData(points.Average(x => x.X), points.Average(x => x.Y), points.Average(x => x.Z));
            
            var p1 = points[0];
            var p2 = points[1];
            var p3 = points[2];
            
            double d1 = p1.DistanceTo(p2);
            double d2 = p2.DistanceTo(p3);

            // Reverting to original logic but swapping Height and Width for Tekla compatibility
            if (d1 >= d2)
            {
                Height = d2;
                Width = d1;
                Rotation = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
            }
            else
            {
                Height = d1;
                Width = d2;
                Rotation = Math.Atan2(p3.Y - p2.Y, p3.X - p2.X);
            }

            Width = Math.Round(Width, 1);
            Height = Math.Round(Height, 1);
            
            // Adjust rotation to Tekla
            if (Rotation < 0) Rotation += Math.PI * 2;
        }

        public override bool Equals(object obj)
        {
            if (obj is ColumnInfo other)
            {
                return Math.Abs(Center.X - other.Center.X) < 1 &&
                       Math.Abs(Center.Y - other.Center.Y) < 1;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 0; // Force Equals
        }
    }

    public class ColumnInfoCollection : ObservableObject
    {
        public List<ColumnInfo> ColumnInfos { get; set; } = new List<ColumnInfo>();

        private double _width;
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private double _height;
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        private string _text;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        private int _number;
        public int Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }

        public string Size => $"{Width} x {Height}";
    }

    public class FloorInfoCollection : ObservableObject
    {
        public List<List<XyzData>> FloorPoints { get; set; } = new List<List<XyzData>>();

        private double _area;
        public double Area
        {
            get => _area;
            set => SetProperty(ref _area, value);
        }

        private int _number;
        public int Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }

        private double _thickness = 150;
        public double Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value);
        }
    }

    public class GridInfo : ObservableObject
    {
        private string _coordinateX = "0 5000 10000";
        public string CoordinateX { get => _coordinateX; set => SetProperty(ref _coordinateX, value); }
        
        private string _coordinateY = "0 5000 10000";
        public string CoordinateY { get => _coordinateY; set => SetProperty(ref _coordinateY, value); }
        
        private string _coordinateZ = "0 3000 6000";
        public string CoordinateZ { get => _coordinateZ; set => SetProperty(ref _coordinateZ, value); }

        private string _labelX = "1 2 3";
        public string LabelX { get => _labelX; set => SetProperty(ref _labelX, value); }
        
        private string _labelY = "A B C";
        public string LabelY { get => _labelY; set => SetProperty(ref _labelY, value); }
        
        private string _labelZ = "+0 +3000 +6000";
        public string LabelZ { get => _labelZ; set => SetProperty(ref _labelZ, value); }
    }
}
