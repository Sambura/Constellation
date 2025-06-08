using System;

namespace Core
{
    static class MathUtility
    {
        public const float HalfPI = (float)(Math.PI / 2);
        public const float TwoPI = (float)(Math.PI * 2);
        public const float Angle0 = 0;
        public const float Angle90 = HalfPI;
        public const float Angle180 = (float)(Math.PI);
        public const float Angle270 = (float)(1.5f * Math.PI);
        public const float Angle360 = TwoPI;

        /// <summary>
        /// Finds minimum number among two non-negative (or negative) numbers.
        /// If one of the numbers is non-negative, and the other is negative - 
        /// the non-negative one is retuned.
        /// </summary>
        public static int MinPositive(int v1, int v2)
        {
            if (v1 < 0)
            {
                if (v2 < 0) return Math.Min(v1, v2);
                return v2;
            }

            if (v2 < 0)
                return v1;

            return Math.Min(v1, v2);
        }

        /// <summary>
        /// Computes a point on an axis-aligned ellipse with horizontal radius a, vertical radius b at a specified 
        /// angle in radians, centered at (0, 0)
        /// </summary>
        public static (float x, float y) GetEllipsePoint(float a, float b, float angle)
        {
            float s = MathF.Sin(angle);
            float c = MathF.Cos(angle);

            float x = a * s;
            float y = b * c;
            float d = MathF.Sqrt(x * x + y * y);
            float factor = a * b / d;

            return (factor * c, factor * s);
        }

        /// <summary>
        /// Computes a point on a circle of radius r at a specified angle in radians, centered at (0, 0)
        /// </summary>
        public static (float x, float y) GetCirclePoint(float r, float angle)
        {
            float s = MathF.Sin(angle);
            float c = MathF.Cos(angle);

            return (r * c, r * s);
        }

        public static double LerpUnclamped(double a, double b, double t) => a * (1 - t) + b * t;
    }
}
