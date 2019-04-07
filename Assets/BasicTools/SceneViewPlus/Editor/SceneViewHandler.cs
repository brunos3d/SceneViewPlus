// SceneViewPlus
// Version 1.0.0
// Bruno Silva
// bruno3dcontato@gmail.com

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;

namespace BasicTools.SceneViewPlus {
	internal class SceneViewHandler {

		[PreferenceItem("SceneViewPlus")]
		public static void CustomPreferencesGUI() {
			GUILayout.Label(new GUIContent("Settings"), (GUIStyle)"TL Selection H2", GUILayout.ExpandWidth(false));

			SceneViewPrefs.enabled = EditorGUILayout.Toggle(new GUIContent("Enable SceneViewPlus"), SceneViewPrefs.enabled);
			SceneViewPrefs.mouseFocus = EditorGUILayout.Toggle(new GUIContent("Mouse Focus", "Window takes focus when mouse is hover"), SceneViewPrefs.mouseFocus);
			if (GUILayout.Button("Reset Data")) {
				SceneViewPrefs.ResetData();
			}

			GUILayout.FlexibleSpace();
			SceneViewPrefs.devMode = EditorGUILayout.Toggle(new GUIContent("Development Mode"), SceneViewPrefs.devMode);

			GUILayout.BeginHorizontal();
			GUI.enabled = (SceneViewPrefs.enabled & SceneViewPrefs.mouseFocus & !SceneViewPrefs.devMode) == false;
			if (GUILayout.Button("Use Defaults", GUILayout.Width(120.0f))) {
				SceneViewPrefs.enabled = true;
			}
			GUI.enabled = true;
			GUILayout.FlexibleSpace();
			GUILayout.Label(new GUIContent("Version: " + SceneViewPrefs.versionInfo));
			GUILayout.EndHorizontal();
			GUILayout.Space(5.0f);

			if (GUI.changed) {
				EditorApplication.RepaintProjectWindow();
				EditorApplication.RepaintHierarchyWindow();
				GUI.changed = false;
			}
		}
	}
}