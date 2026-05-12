using System;
using System.Collections.Generic;
using System.Linq;

namespace Calculator_Foundation_Etabs_API.Models
{
    public static class FoundationCalculator
    {
        // 1. Logic Hợp nhất lực về tâm dải móng
        public static (double N, double M) MergeForces(RawStripData raw)
        {
            double centerCoord = (raw.MinCoord + raw.MaxCoord) / 2.0;
            double totalN = 0;
            double totalM = 0;

            foreach (var col in raw.ColumnLoads)
            {
                double colCoord = raw.IsHorizontal ? col.X : col.Y;
                double Ni = Math.Abs(col.P);
                double Mi = 0;
                double Hi = 0;

                if (raw.IsHorizontal)
                {
                    Mi = col.M3;
                    Hi = col.V2;
                }
                else
                {
                    Mi = col.M2;
                    Hi = col.V3;
                }

                double di = colCoord - centerCoord;
                totalN += Ni;
                totalM += (Mi + Ni * di - Hi * raw.H);
            }

            return (totalN, Math.Abs(totalM));
        }

        // 2. Hàm tính Áp lực tính toán cực đại (Pmax)
        public static (double Pmax, double Pmin) CalculateGroundPressure(double Ntt, double Mtt, double b, double L, double gamma_tb, double Df)
        {
            double F = b * L;
            double W = (b * L * L) / 6.0;

            // p = 1.1 * (N/F + M/W) + gamma_tb * Df
            // Hệ số 1.1 tính đến trọng lượng bản thân móng và đất đè lên móng
            double p_struct_max = 1.1 * (Ntt / F + Mtt / W);
            double p_struct_min = 1.1 * (Ntt / F - Mtt / W);
            
            double p_soil = gamma_tb * Df;

            return (p_struct_max + p_soil, p_struct_min + p_soil);
        }

        // 3. Hàm tính Sức chịu tải cực hạn của đất nền (q_ult)
        public static double CalculateUltimateBearingCapacity(double phi, double c, double q, double gamma, double b)
        {
            var (Nc, Nq, Ngamma) = GetTerzaghiFactors(phi);
            
            // q_ult = c*Nc + q*Nq + 0.5*gamma*b*Ngamma
            double q_ult = c * Nc + q * Nq + 0.5 * gamma * b * Ngamma;
            return q_ult;
        }

        // 4. Hàm tính toán kiểm tra đâm thủng (Punching Shear)
        public static (double Pdt, double Pcdt) CalculatePunchingShear(double b, double bw, double h, double pmax, double pmin)
        {
            double h0 = h - 0.05; // Chiều cao có ích
            if (h0 <= 0) return (0, 0);

            // Cánh móng (từ mép dầm ra mép móng)
            double wing = (b - bw) / 2.0;
            
            // Khoảng cách từ mép dầm đến mép tháp đâm thủng 45 độ là h0
            double a_out = wing - h0;

            if (a_out <= 0) return (0, 1000000); // Không bị đâm thủng nếu cánh ngắn hơn h0

            // Áp lực tại mép dầm (x=wing) và mép tháp (x=h0 từ mép dầm)
            // Giả sử p phân bố đều hoặc tuyến tính. Ở đây móng băng lấy p_tb trung bình
            double p_avg = (pmax + pmin) / 2.0;

            // Lực đâm thủng tác dụng lên phần cánh ngoài tháp (tính trên 1m dài)
            double Pdt = p_avg * a_out * 1.0; 

            // Khả năng chịu đâm thủng của bê tông (Giả sử B20: Rbt = 0.9 MPa = 900 kPa)
            // Pcdt = Rbt * b_tb * h0. Với móng băng 1m dài, chu vi đâm thủng b_tb = 1.0m
            double Rbt = 900; 
            double Pcdt = Rbt * 1.0 * h0;

            return (Pdt, Pcdt);
        }

        // 4. Logic Nội suy Terzaghi
        private static readonly double[,] TerzaghiTable = new double[,]
        {
            {0, 5.70, 1.00, 0.00}, {5, 7.30, 1.60, 0.50}, {10, 9.60, 2.70, 1.20}, {15, 12.90, 4.40, 2.50},
            {20, 17.70, 7.40, 5.00}, {25, 25.10, 12.70, 9.70}, {30, 37.20, 22.50, 19.70}, {33, 48.10, 32.20, 28.40},
            {34, 52.60, 36.50, 31.50}, {35, 57.80, 41.40, 42.40}, {40, 95.70, 81.30, 100.40}, {45, 172.30, 173.30, 297.50},
            {50, 347.50, 415.10, 1153.20}
        };

        public static (double Nc, double Nq, double Ngamma) GetTerzaghiFactors(double phi)
        {
            if (phi <= 0) return (TerzaghiTable[0, 1], TerzaghiTable[0, 2], TerzaghiTable[0, 3]);
            int n = TerzaghiTable.GetLength(0);
            if (phi >= TerzaghiTable[n - 1, 0]) return (TerzaghiTable[n - 1, 1], TerzaghiTable[n - 1, 2], TerzaghiTable[n - 1, 3]);
            
            for (int i = 0; i < n - 1; i++)
            {
                double p1 = TerzaghiTable[i, 0]; double p2 = TerzaghiTable[i + 1, 0];
                if (phi >= p1 && phi <= p2)
                {
                    double ratio = (phi - p1) / (p2 - p1);
                    return (TerzaghiTable[i, 1] + ratio * (TerzaghiTable[i + 1, 1] - TerzaghiTable[i, 1]),
                            TerzaghiTable[i, 2] + ratio * (TerzaghiTable[i + 1, 2] - TerzaghiTable[i, 2]),
                            TerzaghiTable[i, 3] + ratio * (TerzaghiTable[i + 1, 3] - TerzaghiTable[i, 3]));
                }
            }
            return (0, 0, 0);
        }

        // 5. Hàm chạy tính toán tổng hợp cho 1 móng
        public static void RunCalculation(FoundationModel f, double phi, double c, double gamma1, double gamma, double df, double fs, double rs)
        {
            if (f.B <= 0 || f.H <= 0) return;

            // A. Tính sức chịu tải cực hạn và cho phép
            double q_surcharge = gamma1 * df; // q = gamma1 * hm
            double q_ult = CalculateUltimateBearingCapacity(phi, c, q_surcharge, gamma, f.B);
            f.Rtc = q_ult / (fs > 0 ? fs : 1.0); // qa = qult / FS

            // B. Tính áp lực thực tế dưới đáy móng
            double L = f.Length > 0 ? f.Length : 1.0;
            var (pmax, pmin) = CalculateGroundPressure(f.N, f.M, f.B, L, gamma1, df);
            f.Pmax = pmax;
            f.Pmin = pmin;

            // C. Tính toán thép và kiểm tra điều kiện đâm thủng
            double m_face = f.Pmax * Math.Pow((f.B - f.Bw) / 2, 2) / 2.0;
            f.AsReq = (m_face * 1000) / (0.9 * rs * (f.H - 0.05) * 100);
            
            var (pdt, pcdt) = CalculatePunchingShear(f.B, f.Bw, f.H, f.Pmax, f.Pmin);
            f.PPunchMax = pdt;
            f.PCdt = pcdt;
            f.StatusPunch = (pdt <= pcdt) ? "ĐẠT" : "KHÔNG ĐẠT";

            f.Status = (f.Pmax <= f.Rtc && f.Pmin >= 0) ? "ĐẠT" : "KHÔNG ĐẠT";
        }
    }
}
