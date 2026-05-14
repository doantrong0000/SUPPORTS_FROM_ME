namespace Beam_Rebar_Design.Models
{
    /// <summary>
    /// Class để lưu thông tin nội lực chi tiết tại từng station
    /// </summary>
    public class DetailedFrameForceData
    {
        public string FrameName { get; set; }
        public string LoadCase { get; set; }
        public double Station { get; set; }        // Vị trí trên dầm (m - từ ETABS)
        public double FrameLength { get; set; }    // Chiều dài frame (m - từ ETABS)
        public string StepType { get; set; }       // Max hoặc Min
        public double Shear2 { get; set; }         // Lực cắt theo hướng Y local (kN)
        public double Shear3 { get; set; }         // Lực cắt theo hướng Z local (kN)
        public double Moment2 { get; set; }        // Moment quay quanh trục Y local (kNm)
        public double Moment3 { get; set; }        // Moment quay quanh trục Z local (kNm)

        /// <summary>
        /// Tính Station Ratio (0.0 - 1.0) dựa trên vị trí station và chiều dài frame
        /// </summary>
        public double StationRatio
        {
            get
            {
                if (FrameLength > 0)
                    return Station / FrameLength;
                return 0;
            }
        }
    }
}
