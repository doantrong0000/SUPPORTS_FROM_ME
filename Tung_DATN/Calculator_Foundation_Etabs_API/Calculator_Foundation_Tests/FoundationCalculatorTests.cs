using Calculator_Foundation_Etabs_API.Models;
using Calculator_Foundation_Etabs_API.Services;
using Xunit;

namespace Calculator_Foundation_Tests
{
    public class FoundationCalculatorTests
    {
        [Fact]
        public void CalculateGroundPressure_ReturnsCorrectValues()
        {
            // Arrange
            double Ntt = 1000; // kN
            double Mtt = 100;  // kNm
            double b = 1.2;    // m
            double L = 6.0;    // m
            double gamma_tb = 18.0;
            double Df = 1.5;

            // Act
            (double pmax, double pmin) = FoundationCalculator.CalculateGroundPressure(Ntt, Mtt, b, L, gamma_tb, Df);

            // Assert
            // F = 1.2 * 6.0 = 7.2
            // W = 1.2 * 6.0^2 / 6 = 7.2
            // p_struct_max = 1.1 * (1000/7.2 + 100/7.2) = 1.1 * (1100/7.2) = 168.05
            // p_soil = 18.0 * 1.5 = 27.0
            // pmax = 168.05 + 27.0 = 195.05
            Assert.Equal(195.06, pmax, 2);
            Assert.Equal(164.5, pmin, 2);
        }

        [Fact]
        public void GetTerzaghiFactors_Phi33_ReturnsSpecificValues()
        {
            // Arrange
            double phi = 33;

            // Act
            (double Nc, double Nq, double Ngamma) = FoundationCalculator.GetTerzaghiFactors(phi);

            // Assert
            Assert.Equal(48.10, Nc, 2);
            Assert.Equal(32.20, Nq, 2);
            Assert.Equal(28.40, Ngamma, 2);
        }

        [Fact]
        public void CalculateUltimateBearingCapacity_BasicTest()
        {
            // Arrange
            double phi = 33;
            double c = 15;
            double q = 27; // gamma1 * Df
            double gamma = 18;
            double b = 1.2;

            // Act
            double q_ult = FoundationCalculator.CalculateUltimateBearingCapacity(phi, c, q, gamma, b);

            // Assert
            // q_ult = 15 * 48.10 + 27 * 32.20 + 0.5 * 18 * 1.2 * 28.40
            // q_ult = 721.5 + 869.4 + 306.72 = 1897.62
            Assert.Equal(1897.62, q_ult, 2);
        }

        [Fact]
        public void MergeForces_HorizontalStrip_CalculatesCorrectNandM()
        {
            // Arrange
            var raw = new RawStripData
            {
                IsHorizontal = true,
                MinCoord = 0,
                MaxCoord = 10, // Center = 5
                H = 0.5,
                ColumnLoads = new System.Collections.Generic.List<RawColumnLoad>
                {
                    new RawColumnLoad { X = 2, P = -100, M3 = 10, V2 = 5 }, // d = 2 - 5 = -3
                    new RawColumnLoad { X = 8, P = -200, M3 = 20, V2 = 10 } // d = 8 - 5 = 3
                }
            };

            // Act
            (double totalN, double totalM) = FoundationCalculator.MergeForces(raw);

            // Assert
            // N = |-100| + |-200| = 300
            // M1 = 10 + 100 * (-3) - 5 * 0.5 = 10 - 300 - 2.5 = -292.5
            // M2 = 20 + 200 * (3) - 10 * 0.5 = 20 + 600 - 5 = 615
            // totalM = |-292.5 + 615| = |322.5|
            Assert.Equal(300, totalN);
            Assert.Equal(322.5, totalM);
        }
        [Fact]
        public void CalculatePunchingShear_Test()
        {
            // Arrange
            double b = 2.0;
            double bw = 0.4;
            double h = 0.5; // h0 = 0.45
            double pmax = 250;
            double pmin = 150; // p_avg = 200

            // Act
            (double pdt, double pcdt) = FoundationCalculator.CalculatePunchingShear(b, bw, h, pmax, pmin);

            // Assert
            // wing = (2.0 - 0.4) / 2 = 0.8
            // a_out = 0.8 - 0.45 = 0.35
            // pdt = 200 * 0.35 = 70
            // pcdt = 900 * 0.45 = 405
            Assert.Equal(70, pdt, 2);
            Assert.Equal(405, pcdt, 2);
        }
    }
}
