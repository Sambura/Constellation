using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using Core;

namespace UnityCore
{
	public class GradientJsonSerializer : IJsonPropertySerializer<Gradient>
	{
		public string ToJson(object obj, bool prettyPrint)
		{
			if (obj == null) return "null";

			Gradient gradient = obj as Gradient;
			if (gradient == null) throw new ArgumentException("The object to serialize is not of type Gradient");

			StringBuilder json = new StringBuilder(64);
			json.Append('{');
			JsonSerializerUtility.SerializeDefault(json, nameof(gradient.mode), gradient.mode);
			JsonSerializerUtility.SerializeDefault(json, nameof(gradient.colorKeys), gradient.colorKeys);
			JsonSerializerUtility.SerializeDefault(json, nameof(gradient.alphaKeys), gradient.alphaKeys);
			JsonSerializerUtility.RemoveLastComma(json);
			json.Append('}');

			return JsonSerializerUtility.Prettyfy(json.ToString(), prettyPrint);
		}

		public object FromJson(string json, Type type)
		{
			if (type != typeof(Gradient)) throw new ArgumentException("Type should be Gradient");

			Dictionary<string, string> data = JsonSerializerUtility.GetProperties(json);
			Gradient gradient = new Gradient();

			if (data.TryGetValue(nameof(gradient.mode), out string value))
				gradient.mode = (GradientMode)DefaultJsonSerializer.Default.FromJson(value, typeof(GradientMode));
			if (data.TryGetValue(nameof(gradient.colorKeys), out value))
				gradient.colorKeys = (GradientColorKey[])DefaultJsonSerializer.Default.FromJson(value, typeof(GradientColorKey[]));
			if (data.TryGetValue(nameof(gradient.alphaKeys), out value))
				gradient.alphaKeys = (GradientAlphaKey[])DefaultJsonSerializer.Default.FromJson(value, typeof(GradientAlphaKey[]));

			return gradient;
		}

		public void FromJsonOverwrite(string json, object obj)
		{
			throw new NotImplementedException();
		}
	}
}
