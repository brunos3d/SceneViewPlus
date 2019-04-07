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

	internal class SVPWindow : EditorWindow {

		private int tab;
		private Event current;
		private Vector2 scroll;
		private ReorderableList slots;
		private ReorderableList groups;
		private List<Texture2D> labels = new List<Texture2D>();

		[MenuItem("Tools/BasicTools/SceneViewPlus")]
		public static void Init() {
			var editor = GetWindow<SVPWindow>();
			editor.titleContent = new GUIContent("SceneViewPlus");
			editor.Show();
		}

		public void OnEnable() {
			if (labels == null || labels.Count == 0) {
				for (int id = 0; id < 8; id++) {
					labels.Add(EditorGUIUtility.FindTexture(string.Format("sv_icon_dot{0}_pix16_gizmo", id)));
				}
			}
		}

		public void OnFocus() {
			if (labels == null || labels.Count == 0) {
				for (int id = 0; id < 8; id++) {
					labels.Add(EditorGUIUtility.FindTexture(string.Format("sv_icon_dot{0}_pix16_gizmo", id)));
				}
			}
			SlotList();
			GroupList();
		}

		public void SlotList() {
			slots = new ReorderableList(SceneViewPlus.slotsList, typeof(List<SceneViewPlus.ViewSlot>));
			slots.drawHeaderCallback = (Rect rect) => {
				GUI.Label(rect, "SceneView");
			};
			slots.onChangedCallback = (ReorderableList list) => {
				SceneViewPlus.Repaint();
				SceneViewPlus.SaveData();
			};
			slots.onAddCallback = (ReorderableList list) => {
				SceneViewPlus.SaveCurrentView();
				GUI.FocusControl("SaveName");
				slots.index = slots.count - 1;
			};
			slots.onRemoveCallback = (ReorderableList list) => {
				ReorderableList.defaultBehaviours.DoRemoveButton(list);
				SceneViewPlus.SaveData();
				SceneViewPlus.Repaint();
			};
			slots.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				if (current.clickCount >= 2) {
					if (rect.Contains(current.mousePosition)) {
						SceneViewPlus.LoadView(index);
						current.Use();
					}
				}

				DrawStrip(rect, index);

				rect.y += 2;
				GUI.Label(new Rect(rect), SceneViewPlus.slotsList[index].name);
				// GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), SceneViewPlus.slotsList[index].name);
			};
		}

		public void GroupList() {
			groups = new ReorderableList(SceneViewPlus.groupsList, typeof(List<SceneViewPlus.GroupSlot>));
			groups.drawHeaderCallback = (Rect rect) => {
				GUI.Label(rect, "Object Group");
			};
			groups.onChangedCallback = (ReorderableList list) => {
				SceneViewPlus.Repaint();
				SceneViewPlus.SaveData();
			};
			groups.onAddCallback = (ReorderableList list) => {
				SceneViewPlus.SaveCurrentGroup();
				GUI.FocusControl("Group Name");
				groups.index = groups.count - 1;
			};
			groups.onRemoveCallback = (ReorderableList list) => {
				ReorderableList.defaultBehaviours.DoRemoveButton(list);
				SceneViewPlus.SaveData();
				SceneViewPlus.Repaint();
			};
			groups.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				if (current.clickCount >= 2) {
					if (rect.Contains(current.mousePosition)) {
						SceneViewPlus.LoadGroup(index);
						EditorApplication.RepaintHierarchyWindow();
						current.Use();
					}
				}

				DrawStrip(rect, index);

				rect.y += 2.0f;
				GUI.Label(rect, SceneViewPlus.groupsList[index].name);
				Rect button = new Rect(rect) { position = new Vector2(position.width - 45.0f, rect.y), size = new Vector2(18.0f, 18.0f) };
				SceneViewPlus.groupsList[index].label %= 8;
				Texture2D icon = labels[SceneViewPlus.groupsList[index].label];
				if (GUI.Button(button, new GUIContent(icon, "Group Icon"), GUI.skin.label)) {
					SceneViewPlus.groupsList[index].label++;
					SceneViewPlus.SaveData();
				}
			};
		}

		public void DrawStrip(Rect rect, int index) {
			Rect strip = new Rect(rect);
			strip.xMin = 0.0f;
			strip.xMax += 4.0f;
			Color color = new Color(1.0f, 1.0f, 1.0f, 0.20f);

			if (EditorGUIUtility.isProSkin) {
				color = new Color(0.0f, 0.0f, 0.0f, 0.10f);
			}

			if (index % 2 == 0) {
				EditorGUI.DrawRect(strip, color);
			}
		}
		
		public void OnGUI() {
			current = Event.current;
			tab = GUILayout.Toolbar(tab, new string[] { "SceneView", "SceneViewFX", "Object Group", "Preferences" });
			switch (tab) {
				case 0:
				DrawSlotList();
				break;
				case 1:
				DrawEditorFX();
				break;
				case 2:
				DrawGroupList();
				break;
				case 3:
				DrawPrefs();
				break;
			}
		}

		private void DrawSlotList() {
			GUILayout.BeginArea(new Rect(5.0f, 25.0f, position.width - 10.0f, position.height - 25.0f));
			//  20.0f + (slots.count * 20.0f)
			GUILayout.Label("Load with double click");
			scroll = GUILayout.BeginScrollView(scroll);
			slots.DoLayoutList();
			GUILayout.EndScrollView();
			if (slots.index >= 0 && slots.index < SceneViewPlus.slotsList.Count) {
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical((GUIStyle)"HelpBox");
				GUI.SetNextControlName("SaveName");
				SceneViewPlus.slotsList[slots.index].name = EditorGUILayout.TextField(new GUIContent("Name"), SceneViewPlus.slotsList[slots.index].name);
				SceneViewPlus.slotsList[slots.index].in2DMode = EditorGUILayout.Toggle(new GUIContent("In 2D Mode"), SceneViewPlus.slotsList[slots.index].in2DMode);
				SceneViewPlus.slotsList[slots.index].audio = EditorGUILayout.Toggle(new GUIContent("Audio"), SceneViewPlus.slotsList[slots.index].audio);
				SceneViewPlus.slotsList[slots.index].lighting = EditorGUILayout.Toggle(new GUIContent("Lighting"), SceneViewPlus.slotsList[slots.index].lighting);
				SceneViewPlus.slotsList[slots.index].wireframe = EditorGUILayout.Toggle(new GUIContent("Wireframe"), SceneViewPlus.slotsList[slots.index].wireframe);
				SceneViewPlus.slotsList[slots.index].orthographic = EditorGUILayout.Toggle(new GUIContent("Orthographic"), SceneViewPlus.slotsList[slots.index].orthographic);
				SceneViewPlus.slotsList[slots.index].save_selection = EditorGUILayout.Toggle(new GUIContent("Save Selection"), SceneViewPlus.slotsList[slots.index].save_selection);
				if (GUI.changed) {
					SceneViewPlus.SaveData();
				}
				GUILayout.EndVertical();
				GUILayout.Space(5.0f);
			}
			GUILayout.EndArea();
		}

		private void DrawGroupList() {
			GUILayout.BeginArea(new Rect(5.0f, 25.0f, position.width - 10.0f, position.height - 25.0f));
			GUILayout.Label("Select whole group with double click");
			scroll = GUILayout.BeginScrollView(scroll);
			groups.DoLayoutList();
			GUILayout.EndScrollView();
			if (groups.index >= 0 && groups.index < SceneViewPlus.groupsList.Count) {
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical((GUIStyle)"HelpBox");
				GUI.SetNextControlName("Group Name");
				SceneViewPlus.groupsList[groups.index].name = EditorGUILayout.TextField(new GUIContent("Name"), SceneViewPlus.groupsList[groups.index].name);
				if (GUI.changed) {
					SceneViewPlus.SaveData();
				}
				GUILayout.EndVertical();
				GUILayout.Space(5.0f);
			}
			GUILayout.EndArea();
		}

		private void DrawPrefs() {
			GUILayout.BeginArea(new Rect(5.0f, 25.0f, position.width - 10.0f, position.height - 25.0f));
			scroll = GUILayout.BeginScrollView(scroll);
			SceneViewHandler.CustomPreferencesGUI();
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawEditorFX() {
			if (GUILayout.Button("Select")) {
				Selection.activeGameObject = SceneViewPlus.gameObject;
			}
			if (GUILayout.Button("Edit")) {
				SceneViewPlus.gameObject.hideFlags = HideFlags.DontSave;
			}
			if (GUILayout.Button("Non Edit")) {
				SceneViewPlus.gameObject.hideFlags = HideFlags.HideAndDontSave;
			}
		}
	}
}