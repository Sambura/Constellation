using UnityEngine;
using ConfigSerialization;
using ConfigSerialization.Structuring;
using UnityCore;

public class MiscConfigCollection : MonoBehaviour
{
	[Header("Objects")]
	[SerializeField] private StaticTimeFPSCounter _fpsCounter;
	[SerializeField] private GameObject _fpsCounterObject;
	[SerializeField] private TransparentWindow _transparentWindow;
	[SerializeField] private ConfigSerializer _configSerializer;

	[ConfigGroupToggle(3)] [ConfigGroupMember(GroupId = "AC+fps")]
	[ConfigProperty(hasEvent: false)] public bool ShowFPS
	{
		get => _fpsCounterObject.activeSelf;
		set
		{
			if (_fpsCounterObject.activeSelf == value) return;
			_fpsCounterObject.SetActive(value);
			ShowFPSChanged?.Invoke(value);
		}
	}
	public event System.Action<bool> ShowFPSChanged;

	[ConfigGroupMember(3, 0)]
	[SliderProperty(0.1f, 5, 0, 999, "0.00 s", @"([-+]?[0-9]*\.?[0-9]+) *s?", hasEvent: false)] 
	public float TimeWindow
	{
		get => _fpsCounter.TimeWindow;
		set => _fpsCounter.TimeWindow = value;
	}

	[ConfigGroupMember("Save/Load configuration", 1)]
	[InvokableMethod] public void SaveConfig() => _configSerializer.SaveConfig();
	[ConfigGroupMember("Save/Load configuration", 1)]
	[InvokableMethod] public void LoadConfig() => _configSerializer.LoadConfig();
	[ConfigGroupMember(2, GroupId = "Misc+other")] [ConfigMemberOrder(0)]
	[InvokableMethod] public void MakeTransparent() => _transparentWindow.MakeTransparent();
}
