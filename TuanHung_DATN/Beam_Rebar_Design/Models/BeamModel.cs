namespace Beam_Rebar_Design.Models
{
    public class BeamModel
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Story { get; set; }
        public string SectionName { get; set; }

        public double B { get; set; }
        public double H { get; set; }
        public double Ltt { get; set; }

        public double SpanLeft { get; set; }
        public double SpanRight { get; set; }
        public double FloorThicknessLeft { get; set; }
        public double FloorThicknessRight { get; set; }
        public double WallHeight { get; set; }

        // Nội lực tại 3 vị trí
        public double Momen1 { get; set; }
        public double Momen2 { get; set; }
        public double Momen3 { get; set; }
        public double Shear1 { get; set; }
        public double Shear2 { get; set; }
        public double Shear3 { get; set; }

        // Diện tích thép yêu cầu (mm2)
        public double AsRequiredTrai { get; set; }
        public double AsRequiredDuoi { get; set; }
        public double AsRequiredPhai { get; set; }

        // Khoảng cách đai (mm)
        public double StirrupSpacingV1 { get; set; }
        public double StirrupSpacingV2 { get; set; }
        public double StirrupSpacingV3 { get; set; }

        // Vị trí station (0.0 to 1.0)
        public double Station1 { get; set; }
        public double Station2 { get; set; }
        public double Station3 { get; set; }
    }
}
