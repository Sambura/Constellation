using System;

namespace ConfigSerialization.Structuring
{
	public class ConfigGroupToggleAttribute : Attribute
	{
		public int? GroupIndex { get; private set; }
		public string GroupId { get; private set; }

		public int? InverseGroupIndex { get; private set; }
		public string InverseGroupId { get; private set; }

		public bool InvertToggle { get; set; } = false;

		public ConfigGroupToggleAttribute(int groupIndex, string inverseGroupId = null)
		{
			GroupIndex = groupIndex;
			InverseGroupId = inverseGroupId;
		}

		public ConfigGroupToggleAttribute(int groupIndex, int inverseGroupIndex)
		{
			GroupIndex = groupIndex;
			InverseGroupIndex = inverseGroupIndex;
		}

		public ConfigGroupToggleAttribute(string groupId, string inverseGroupId = null)
		{
			GroupId = groupId;
			InverseGroupId = inverseGroupId;
		}

		public ConfigGroupToggleAttribute(string groupId, int inverseGroupIndex)
		{
			GroupId = groupId;
			InverseGroupIndex = inverseGroupIndex;
		}
	}
}
