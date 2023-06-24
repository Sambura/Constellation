using System;
using UnityEngine;

namespace ConfigSerialization
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class SetComponentPropertyAttribute : Attribute
	{
		public Type ComponentType { get; }
		public string PropertyName { get; }
		public object Value { get; }
		public string ChildName { get; }

		public SetComponentPropertyAttribute(Type componentType, string propertyName, object value, string childName = null)
		{
			if (!componentType.IsSubclassOf(typeof(Component))) throw new ArgumentException("Type should be derived from UnityEngine.Component");

			ComponentType = componentType;
			PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
			Value = value;
			ChildName = childName;
		}

		public SetComponentPropertyAttribute(Type componentType, string propertyName, Type valueType, object[] constructorArguments, string childName = null)
		{
			if (!componentType.IsSubclassOf(typeof(Component))) throw new ArgumentException("Type should be derived from UnityEngine.Component");

			ComponentType = componentType;
			PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
			Value = Activator.CreateInstance(valueType, constructorArguments);
			ChildName = childName;
		}
	}
}
