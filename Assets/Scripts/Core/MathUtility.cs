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
	}
}
