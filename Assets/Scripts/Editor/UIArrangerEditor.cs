using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// This thing is horrible 
/// did not even try to make it usable
/// gl
/// </summary>
[CustomEditor(typeof(UIArranger))]
public class UIArrangerEditor : Editor
{
	private bool _configurationsExpanded = true;

	public override void OnInspectorGUI()
	{
		UIArranger uiArranger = (UIArranger)target;
		SerializedObject serialized = new SerializedObject(uiArranger);
		SerializedProperty selectedConfigIndex = serialized.FindProperty("_selectedConfiguration");
		var configurations = (List<UIArranger.UIConfiguration>)typeof(UIArranger)
			.GetField("_configurations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(uiArranger);

		int selectedIndex = selectedConfigIndex.intValue;
		GUIStyle listStyle = new GUIStyle();
		listStyle.normal.background = Texture2D.linearGrayTexture;

		_configurationsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(_configurationsExpanded, "Configurations");

		if (_configurationsExpanded)
		{
			EditorGUILayout.BeginVertical(listStyle);
			for (int i = 0; i < configurations.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				if (EditorGUILayout.ToggleLeft(configurations[i].Name, i == selectedIndex))
				{
					if (i != selectedIndex)
					{
						Undo.RecordObject(uiArranger, "Change selected configuration");
						selectedConfigIndex.intValue = i;
						uiArranger.SelectedConfigurationIndex = selectedConfigIndex.intValue;
					}
				}
				configurations[i].Name = EditorGUILayout.TextField(configurations[i].Name);
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("+", GUILayout.Width(GUIStyle.none.lineHeight * 2)))
			{
				Undo.RecordObject(uiArranger, "Add new configuration to UIArranger");
				var newConfig = new UIArranger.UIConfiguration() { Name = "New configuration" };
				configurations.Add(newConfig);

				RectTransform currentObject = uiArranger.gameObject.GetComponent<RectTransform>();

				foreach (RectTransform child in currentObject)
				{
					if (child == null) continue;
					newConfig.UIElements.Add(child);
				}

				newConfig.UIElements.Add(currentObject);
			}

			if (GUILayout.Button("-", GUILayout.Width(GUIStyle.none.lineHeight * 2)))
			{
				Undo.RecordObject(uiArranger, "Remove configuration from UIArranger");
				configurations.RemoveAt(selectedConfigIndex.intValue);
				serialized.Update();
				selectedConfigIndex.intValue = Mathf.Clamp(selectedConfigIndex.intValue - 1, 0, configurations.Count - 1);
				uiArranger.SelectedConfigurationIndex = selectedConfigIndex.intValue;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

		}

		EditorGUILayout.EndFoldoutHeaderGroup();

		if (configurations.Count > 0) 
			configurations[selectedConfigIndex.intValue].SyncAllElements();

		serialized.ApplyModifiedProperties();
	}
}