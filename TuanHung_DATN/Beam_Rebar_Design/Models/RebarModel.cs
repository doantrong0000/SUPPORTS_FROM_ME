namespace Beam_Rebar_Design.Models
{
    public class RebarModel
    {
        public string Name { get; set; }
        public double Rs { get; set; }
        public double Rsw { get; set; }

        /// <summary>
        /// Hệ số moment giới hạn αR (dùng để kiểm tra cốt thép đơn/kép)
        /// </summary>
        public double AlphaR { get; set; }

        /// <summary>
        /// Hệ số giới hạn vùng nén ξR (chỉ để hiển thị)
        /// </summary>
        public double KsiR { get; set; }
    }
}
