using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectApp.ModelFromCad
{
    public class BeamInfoCollection: ObservableObject
    {
        public List<BeamInfo> BeamInfos { get; set; } = new List<BeamInfo>();

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

        private string _text;
        private string _mark;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }

        public string Mark
        {
            get => _mark;
            set
            {
                _mark = value;
                OnPropertyChanged();
            }
        }

        public ElementType ElementType { get; set; }

        public int Number { get; set; }
    }

    public class BeamInfo
    {
        public XYZ StartPoint { get; set; }

        public XYZ EndPoint { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string Mark { get; set; }

        public BeamInfo(XYZ start, XYZ end, string text)
        {
            StartPoint = start;
            EndPoint = end;
            GetBeam(text);
        }

        public void GetBeam(string text)
        {
            var numbers = Regex.Split(text, @"\D+").Where(x => string.IsNullOrEmpty(x) == false).ToList();

            if (numbers.Count >= 2)
            {
                var last = numbers[numbers.Count - 1];
                var last1 = numbers[numbers.Count - 2];
                Width = Convert.ToDouble(last1);
                Height = Convert.ToDouble(last);
            }

        }

        public class BeamInfoComparerByPoint : IEqualityComparer<BeamInfo>
        {
            public bool Equals(BeamInfo x, BeamInfo y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return false;
            }

            public int GetHashCode(BeamInfo obj)
            {
                return 0;
            }
        }

    }
}
