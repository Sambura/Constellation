using System;

namespace Core
{
	public interface IJsonPropertySerializer<T> : IJsonPropertySerializer { }

	public interface IJsonPropertySerializer
	{
		public string ToJson(object obj, bool prettyPrint);
		public object FromJson(string json, Type type);
		public void FromJsonOverwrite(string json, object obj);
	}
}