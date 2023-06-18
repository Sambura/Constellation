using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using Core;

namespace UnityCore
{
	public class AnimationCurveJsonSerializer : IJsonPropertySerializer<AnimationCurve>
	{
		public string ToJson(object obj, bool prettyPrint)
		{
			if (obj == null) return "null";

			AnimationCurve curve = obj as AnimationCurve;
			if (curve == null) throw new ArgumentException("The object to serialize is not of type AnimationCurve");

			StringBuilder json = new StringBuilder(64); // pretty arbitrary
			json.Append('{');
			JsonSerializerUtility.SerializeDefault(json, nameof(curve.keys), curve.keys);
			JsonSerializerUtility.RemoveLastIfComma(json);
			json.Append('}');

			return JsonSerializerUtility.Prettyfy(json.ToString(), prettyPrint);
		}

		public object FromJson(string json, Type type)
		{
			if (type != typeof(AnimationCurve)) throw new ArgumentException("Type should be AnimationCurve");

			Dictionary<string, string> data = JsonSerializerUtility.GetProperties(json);
			AnimationCurve curve = new AnimationCurve();

			if (data.TryGetValue(nameof(curve.keys), out string value))
				curve.keys = (Keyframe[])DefaultJsonSerializer.Default.FromJson(value, typeof(Keyframe[]));

			return curve;
		}

		public void FromJsonOverwrite(string json, object obj)
		{
			throw new NotImplementedException();
		}
	}
	
	public class KeyframeJsonSerializer : IJsonPropertySerializer<Keyframe>
	{
		public string ToJson(object obj, bool prettyPrint)
		{
			if (obj == null) return "null";
			if (obj is Keyframe == false) throw new ArgumentException("The object to serialize is not of type Keyframe");

			Keyframe keyframe = (Keyframe)obj;

			StringBuilder json = new StringBuilder(64); // pretty arbitrary
			json.Append('{');
			JsonSerializerUtility.SerializeDefault(json, nameof(keyframe.time), keyframe.time);
			JsonSerializerUtility.SerializeDefault(json, nameof(keyframe.value), keyframe.value);
			JsonSerializerUtility.SerializeDefault(json, nameof(keyframe.inTangent), keyframe.inTangent);
			JsonSerializerUtility.SerializeDefault(json, nameof(keyframe.outTangent), keyframe.outTangent);
			JsonSerializerUtility.SerializeDefault(json, nameof(keyframe.inWeight), keyframe.inWeight);
			JsonSerializerUtility.SerializeDefault(json, nameof(keyframe.outWeight), keyframe.outWeight);
			JsonSerializerUtility.RemoveLastIfComma(json);
			json.Append('}');

			return JsonSerializerUtility.Prettyfy(json.ToString(), prettyPrint);
		}

		public object FromJson(string json, Type type)
		{
			if (type != typeof(Keyframe)) throw new ArgumentException("Type should be Keyframe");

			Dictionary<string, string> data = JsonSerializerUtility.GetProperties(json);
			Keyframe keyframe = new Keyframe();

			if (data.TryGetValue(nameof(keyframe.time), out string value))
				keyframe.time = (float)DefaultJsonSerializer.Default.FromJson(value, typeof(float));
			if (data.TryGetValue(nameof(keyframe.value), out value))
				keyframe.value = (float)DefaultJsonSerializer.Default.FromJson(value, typeof(float));
			if (data.TryGetValue(nameof(keyframe.inTangent), out value))
				keyframe.inTangent = (float)DefaultJsonSerializer.Default.FromJson(value, typeof(float));
			if (data.TryGetValue(nameof(keyframe.outTangent), out value))
				keyframe.outTangent = (float)DefaultJsonSerializer.Default.FromJson(value, typeof(float));
			if (data.TryGetValue(nameof(keyframe.inWeight), out value))
				keyframe.inWeight = (float)DefaultJsonSerializer.Default.FromJson(value, typeof(float));
			if (data.TryGetValue(nameof(keyframe.outWeight), out value))
				keyframe.outWeight = (float)DefaultJsonSerializer.Default.FromJson(value, typeof(float));

			return keyframe;
		}

		public void FromJsonOverwrite(string json, object obj)
		{
			throw new NotImplementedException();
		}
	}
}