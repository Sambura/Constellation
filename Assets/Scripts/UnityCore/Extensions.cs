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
	}
}
