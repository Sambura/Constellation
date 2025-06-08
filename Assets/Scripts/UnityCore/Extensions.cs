using UnityEngine;

namespace UnityCore
{
    public static class Extensions
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
#pragma warning disable IDE0029 // Unity's null checking also ensures that the object is not destroyed
            return existing != null ? existing : go.AddComponent<T>();
#pragma warning restore IDE0029 // Use coalesce expression
        }

        public static Vector2 ToVector2(this (float x, float y) vector) => new Vector2(vector.x, vector.y);
        public static Vector2 Rotate90CCW(this Vector2 vector) => new Vector2(-vector.y, vector.x);
        public static Vector2 Abs(this Vector2 vector) => new Vector2(System.MathF.Abs(vector.x), System.MathF.Abs(vector.y));

        public static Vector2 Rotate(this Vector2 vector, float angle)
        {
            float c = System.MathF.Cos(angle);
            float s = System.MathF.Sin(angle);

            return new Vector2(vector.x * c - vector.y * s, vector.x * s + vector.y * c);
        }
    }
}
