using System.Collections.Generic;
using Calculator_Foundation_Etabs_API.Models;

namespace Calculator_Foundation_Etabs_API.Models
{
    public class RawColumnLoad
    {
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double P { get; set; }
        public double V2 { get; set; }
        public double V3 { get; set; }
        public double M2 { get; set; }
        public double M3 { get; set; }
    }

    public class RawStripData
    {
        public string Name { get; set; }
        public bool IsHorizontal { get; set; }
        public double AlignmentValue { get; set; }
        public double MinCoord { get; set; }
        public double MaxCoord { get; set; }
        public double B { get; set; }
        public double H { get; set; }
        public double Bw { get; set; }
        public List<RawColumnLoad> ColumnLoads { get; set; } = new List<RawColumnLoad>();
    }
}
