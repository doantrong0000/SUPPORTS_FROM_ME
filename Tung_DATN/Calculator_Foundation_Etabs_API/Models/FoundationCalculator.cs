using System;
using System.Collections.Generic;
using System.Linq;

namespace Calculator_Foundation_Etabs_API.Models
{
    public static class FoundationCalculator
    {
        // Calculate longitudinal reinforcement for foundation strips (like beams)
        public static void RunCalculation(FoundationModel f, double rs)
        {
            //if (f.B <= 0 || f.H <= 0) 
            //{
            //    f.Status = "LỖI KÍCH THƯỚC";
            //    return;
            //}

            //double h0 = f.H - 0.05; // 5cm cover
            //if (h0 <= 0) h0 = 0.01; // Avoid division by zero
            
            //// MTop corresponds to MaxM3Neg (which causes tension at the top in typical ETABS setup)
            //// MBot corresponds to MaxM3Pos (which causes tension at the bottom)
            //// Note: MTop and MBot are in kNm.
            //// As (cm2) = M(kNm) * 10 / (0.9 * Rs(MPa) * h0(m))

            //f.AsTop_Start = (f.MTop_Start * 10) / (0.9 * rs * h0);
            //f.AsBot_Start = (f.MBot_Start * 10) / (0.9 * rs * h0);

            //f.AsTop_Mid = (f.MTop_Mid * 10) / (0.9 * rs * h0);
            //f.AsBot_Mid = (f.MBot_Mid * 10) / (0.9 * rs * h0);

            //f.AsTop_End = (f.MTop_End * 10) / (0.9 * rs * h0);
            //f.AsBot_End = (f.MBot_End * 10) / (0.9 * rs * h0);
            
            //// Basic status logic
            //if (f.AsTop_Start > 0 || f.AsBot_Start > 0 || f.AsTop_Mid > 0 || f.AsBot_Mid > 0 || f.AsTop_End > 0 || f.AsBot_End > 0)
            //{
            //    f.Status = "ĐÃ TÍNH";
            //}
            //else
            //{
            //    f.Status = "KHÔNG CÓ LỰC";
            //}
        }
    }
}
