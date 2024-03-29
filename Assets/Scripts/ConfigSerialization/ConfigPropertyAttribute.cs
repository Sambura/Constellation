﻿using System;

namespace ConfigSerialization
{
	public class ConfigProperty : Attribute
	{
		public string Name { get; }
		public bool HasEvent { get; }

		public ConfigProperty(string name = null, bool hasEvent = true)
		{
			Name = name;
			HasEvent = hasEvent;
		}
	}
}
