namespace ProjectApp.Utils
{
    public static class DoubleUtils
    {
        public static double MillimetersToFeet(this double millimeters, int decimals)
        {
            if (decimals < 0) throw new ArgumentOutOfRangeException(nameof(decimals), "decimals must be >= 0");
            return Math.Round(MillimetersToFeet(millimeters), decimals);
        }

        public static double MillimetersToFeet(this double millimeters)
        {
            return millimeters / 304.8;
        }
    }
}