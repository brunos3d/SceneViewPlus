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

	[InitializeOnLoad]
	public class SceneViewPlus {

		internal class Dialog {
			public Rect rect;
			public string title;
			public string message;
			public bool display;
			public System.Action abort;
			public System.Action confirm;
			public System.Action customDraw;

			public Dialog() { }

			public void Window(int windowID) {
				GUILayout.FlexibleSpace();

				if (string.IsNullOrEmpty(message) == false) {
					GUIStyle label = (GUIStyle)"label";
					label.wordWrap = true;
					GUILayout.Label(message, label);
				}

				if (customDraw != null) {
					customDraw.Invoke();
				}

				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				if (confirm != null) {
					if (GUILayout.Button("Ok")) {
						confirm.Invoke();
						Reset();
					}
				}
				if (abort != null) {
					if (GUILayout.Button("Cancel")) {
						abort.Invoke();
					}
				}
				GUILayout.EndHorizontal();
				GUI.DragWindow();
			}

			public void Draw() {
				Event current = Event.current;

				switch (current.type) {
					case (EventType.KeyDown):
					if (dialog.display) {
						if (current.keyCode == KeyCode.Return) {
							dialog.Confirm();
							dialog.Reset();
							current.Use();
						}
						if (current.keyCode == KeyCode.Escape) {
							dialog.Reset();
							current.Use();
						}
					}
					break;
				}

				rect = GUI.Window(0, rect, Window, new GUIContent(title), GUI.skin.window);
			}

			public void Confirm() {
				if (confirm != null) {
					confirm.Invoke();
				}
			}

			public void Reset() {
				rect = new Rect();
				display = false;
				abort = null;
				confirm = null;
				dialog.customDraw = null;
			}
		}

		[System.Serializable]
		public class ViewSlot {
			public string name;
			public float size;
			public Vector3 position;
			public Quaternion rotation;
			public List<int> objs = new List<int>();
			public bool orthographic,
				audio,
				in2DMode,
				lighting,
				wireframe,
				save_selection;

			public ViewSlot() { }

			public ViewSlot(string name, SceneView view, bool save_selection, List<int> objs) {
				this.name = name;
				this.size = view.size;
				this.objs = objs;
				this.audio = view.m_AudioPlay;
				this.in2DMode = view.in2DMode;
				this.lighting = view.m_SceneLighting;
				this.wireframe = view.renderMode == DrawCameraMode.Wireframe;
				this.position = view.pivot;
				this.rotation = view.rotation;
				this.orthographic = view.orthographic;
				this.save_selection = save_selection;
			}
		}

		[System.Serializable]
		public class GroupSlot {
			public int label;
			public string name;
			public List<int> ids = new List<int>();

			private List<GameObject> m_objs;

			public List<GameObject> objs {
				get {
					if (m_objs == null || m_objs.Count == 0) {
						m_objs = GetObjectsByIDs(ids).OfType<GameObject>().ToList();
					}
					return m_objs;
				}
			}

			public GroupSlot() { }

			public GroupSlot(string name, List<int> ids) {
				this.name = name;
				this.ids = ids;
			}
		}

		[System.Serializable]
		public class SVPData {
			public List<ViewSlot> slots = new List<ViewSlot>();
			public List<GroupSlot> groups = new List<GroupSlot>();
		}

		#region ReadMe variable
		private static string Read_me = @"SceneView Interact:

Key [F1] = Show hotkeys Info
Key [A] = Select/deselect all objects
Key [G] = Create group with selected objects
Key [H] = Hide all selected objects
Key [X] = Display Dialog to destroy objects
Key [Z] = Toggle Wireframe render mode
Key [Home] = Center the view for the selected objects

Ctrl/Cmd + Key [I] = Invert selection
Ctrl/Cmd + Key [G] = Select whole group
Ctrl/Cmd + Key [H] = Unhide (show) all selected objects

SceneView Move:

Numpad [0] = Align SceneView with the current camera;
Numpad [5] = Toggle orthographic;

Numpad [1] = Move SceneView to the back axis;
Numpad [3] = Move SceneView to the right axis;
Numpad [7] = Move SceneView to the top axis;

Ctrl/Cmd + Numpad [1] = Move SceneView to the front axis;
Ctrl/Cmd + Numpad [3] = Move SceneView to the left axis;
Ctrl/Cmd + Numpad [7] = Move SceneView to the bottom axis;

SceneView Orbit:

Numpad [4] = Left;
Numpad [6] = Right;
Numpad [8] = Up;
Numpad [2] = Down;

SceneView Roll:

Ctrl/Cmd + Numpad [4] = Left;
Ctrl/Cmd + Numpad [6] = Right;

SceneView Zoom:

Numpad [+] = Zoom in;
Numpad [-] = Zoom out;
";
		#endregion
		private static bool FPS;
		private static Vector2 scroll;

		private static SceneView scene_view;
		private static EditorWindow last_view;

		private static SVPData data = new SVPData();
		private static Dialog dialog = new Dialog();

		public static List<ViewSlot> slotsList {
			get {
				if (data == null) {
					OnEnable();
				}
				return data.slots;
			}
			set {
				if (data == null) {
					OnEnable();
				}
				data.slots = value;
			}
		}

		public static List<GroupSlot> groupsList {
			get {
				if (data == null) {
					OnEnable();
				}
				return data.groups;
			}
			set {
				if (data == null) {
					OnEnable();
				}
				data.groups = value;
			}
		}

		public static Rect position {
			get {
				return scene_view.position;
			}
			set {
				scene_view.position = value;
			}
		}

		public static Camera camera {
			get {
				return scene_view.camera;
			}
		}

		public static Transform transform {
			get {
				return scene_view.camera.transform;
			}
		}

		public static GameObject gameObject {
			get {
				return scene_view.camera.gameObject;
			}
		}

		public static int GetObjectID(Object unityObject) {
			PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

			SerializedObject serializedObject = new SerializedObject(unityObject);
			inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

			SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

			int localId = localIdProp.intValue;
			return localId;
		}

		public static Object GetObjectByID(int id) {
			GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
			foreach (Object obj in objs) {
				if (GetObjectID(obj) == id) {
					return obj;
				}
			}
			return null;
		}

		public static List<Object> GetObjectsByIDs(List<int> ids) {
			List<Object> objs = new List<Object>();
			foreach (int id in ids) {
				Object obj = GetObjectByID(id);
				if (obj != null) {
					objs.Add(obj);
				}
			}
			return objs;
		}

		static SceneViewPlus() {
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			OnEnable();
		}

		public static void OnEnable() {
			data = new SVPData();
			data.slots = new List<ViewSlot>();
			LoadData();
		}

		public static void OnSceneGUI(SceneView scene_view) {
			SceneViewPlus.scene_view = scene_view;
			if (SceneViewPrefs.enabled) {
				Handles.BeginGUI();
				SceneViewPlus.OnGUI();
				Handles.EndGUI();
			}
		}

		public static void OnGUI() {
			if (dialog.display) {
				dialog.Draw();
			}
			if (SceneViewPrefs.showInfo) {
				GUILayout.BeginArea(new Rect(5.0f, position.height - 275.0f, 405.0f, 250.0f));
				GUILayout.BeginVertical(GUI.skin.window);
				GUI.Label(new Rect(150.0f, 0.0f, 105.0f, 20.0f), "Info (F1 to close)");
				scroll = GUILayout.BeginScrollView(scroll);
				GUILayout.Label(Read_me);
				GUILayout.EndScrollView();
				GUILayout.EndVertical();
				GUILayout.EndArea();
			}
			InputGUI();
		}

		public static void InputGUI() {
			Event current = Event.current;
			Rect rect = new Rect(0.0f, 0.0f, position.width, position.height - 17.0f);

			if (EditorWindow.focusedWindow != scene_view) {
				last_view = EditorWindow.focusedWindow;
				if (SceneViewPrefs.devMode) {
					EditorGUI.DrawRect(rect, new Color(0.0f, 1.0f, 0.0f, 0.3f));
				}
			}

			if (SceneViewPrefs.mouseFocus) {
				if (EditorWindow.mouseOverWindow != null) {
					if (rect.Contains(current.mousePosition)) {
						if (EditorWindow.mouseOverWindow.Equals(scene_view)) {
							if (EditorWindow.focusedWindow != scene_view) {
								scene_view.Focus();
								Repaint();
							}
						}
						else {
							if (last_view != null && EditorWindow.focusedWindow != last_view) {
								last_view.Focus();
							}
						}
					}
					else {
						if (last_view != null && EditorWindow.focusedWindow != last_view) {
							last_view.Focus();
						}
					}
				}
			}

			if (dialog.display) {
				Repaint();
				if (dialog.rect.Contains(current.mousePosition) == false && Vector2.Distance(dialog.rect.center, current.mousePosition) > 200.0f) {
					dialog.Reset();
				}
			}

			switch (current.type) {
				case EventType.MouseDown:
				if (last_view != null) {
					last_view = null;
					if (SceneViewPrefs.devMode) {
						Notify("Focus Locked");
					}
				}
				if (dialog.display) {
					if (dialog.rect.Contains(current.mousePosition) == false) {
						dialog.Reset();
						current.Use();
					}
				}
				if (current.button == 1) {
					FPS = true;
				}
				Repaint();
				break;
				case EventType.MouseUp:
				FPS = false;
				Repaint();
				break;
				case EventType.MouseDrag:
				Repaint();
				break;
				case EventType.KeyDown:
				float current_x = scene_view.rotation.eulerAngles.x;
				float current_y = scene_view.rotation.eulerAngles.y;
				float current_z = scene_view.rotation.eulerAngles.z;

				if (current.keyCode == KeyCode.F1) {
					SceneViewPrefs.showInfo = !SceneViewPrefs.showInfo;
					current.Use();
				}

				// Focus on objs
				if (current.keyCode == KeyCode.Home) {
					GameObject[] objs = Selection.gameObjects;
					if (objs.Length > 0) {
						scene_view.FrameSelected(true);
						if (SceneViewPrefs.devMode) {
							Notify("FrameSelected");
						}
						current.Use();
					}
					else {
						objs = Object.FindObjectsOfType<GameObject>();
						if (objs.Length > 0) {
							Object[] selectionBKP = Selection.objects;
							Selection.objects = objs;
							scene_view.FrameSelected(true);
							if (SceneViewPrefs.devMode) {
								Notify("FrameSelected");
							}
							Selection.objects = selectionBKP;
							current.Use();
						}
					}
				}

				// select/deselect all
				if (current.Equals(Event.KeyboardEvent("A"))) {
					if (FPS == false) {
						GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>().Where((obj) => (obj.hideFlags == HideFlags.None || obj.hideFlags == HideFlags.HideInHierarchy || obj.hideFlags == HideFlags.NotEditable)).ToArray();
						if (Selection.objects.Length == 0) {
							Selection.objects = objs;
							if (SceneViewPrefs.devMode) {
								Notify("All Objects Selected");
							}
						}
						else {
							Selection.objects = new Object[0];
							if (SceneViewPrefs.devMode) {
								Notify("None Objects Selected");
							}
						}
						current.Use();
					}
				}
				// create group
				if (current.Equals(Event.KeyboardEvent("G"))) {
					SaveCurrentGroup();
					current.Use();
				}
				// select whole group
				else if(current.Equals(Event.KeyboardEvent("^G")) || current.Equals(Event.KeyboardEvent("%G"))) {
					foreach (GroupSlot group in groupsList) {
						if (group.objs.Contains(Selection.activeGameObject)) {
							Selection.objects = group.objs.ToArray();
							current.Use();
							break;
						}
					}
				}
				// hide all selected objects
				if (current.Equals(Event.KeyboardEvent("H"))) {
					if (Selection.objects.Length > 0) {
						int count = 0;
						Undo.RecordObjects(Selection.gameObjects, "Hide Objects");
						foreach (var obj in Selection.gameObjects) {
							obj.SetActive(false);
							count++;
						}
						if (SceneViewPrefs.devMode) {
							Notify("{0} Objects hided", count);
						}
					}
					current.Use();
				}
				// unhide all selected objects
				else if(current.Equals(Event.KeyboardEvent("^H")) || current.Equals(Event.KeyboardEvent("%H"))) {
					if (Selection.objects.Length > 0) {
						int count = 0;
						Undo.RecordObjects(Selection.gameObjects, "Unhide Objects");
						foreach (var obj in Selection.gameObjects) {
							obj.SetActive(true);
							count++;
						}
						if (SceneViewPrefs.devMode) {
							Notify("{0} Objects unhided", count);
						}
					}
					current.Use();
				}
				// unhide all selected objects
				if (current.Equals(Event.KeyboardEvent("^I")) || current.Equals(Event.KeyboardEvent("%I"))) {
					GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>().Where((obj) => Selection.gameObjects.Contains(obj) == false && (obj.hideFlags == HideFlags.None || obj.hideFlags == HideFlags.HideInHierarchy || obj.hideFlags == HideFlags.NotEditable)).ToArray();
					Selection.objects = objs;
					if (SceneViewPrefs.devMode) {
						Notify("Selection inverted");
					}
					current.Use();
				}
				// delete
				if (current.Equals(Event.KeyboardEvent("X"))) {
					if (Selection.gameObjects.Length > 0) {
						dialog.display = true;
						dialog.rect = new Rect(current.mousePosition, new Vector2(150.0f, 100.0f));
						dialog.title = "Destroy Objects...";
						dialog.message = "Do you want to destroy the selected objects?";
						dialog.confirm = DestroySelection;
						dialog.abort = dialog.Reset;
						current.Use();
					}
				}
				// toggle Wireframe
				if (current.Equals(Event.KeyboardEvent("Z"))) {
					if (scene_view.renderMode != (DrawCameraMode)1) {
						scene_view.renderMode = DrawCameraMode.Wireframe;
					}
					else {
						scene_view.renderMode = DrawCameraMode.Textured;
					}
					if (SceneViewPrefs.devMode) {
						Notify("Switch renderMode to {0}", scene_view.renderMode);
					}
					current.Use();
				}

				// zoom in
				if (current.Equals(Event.KeyboardEvent("[+]"))) {
					scene_view.size--;
					if (SceneViewPrefs.devMode) {
						Notify("SceneView zoom-in");
					}
					current.Use();
				}
				// zoom out
				else if(current.Equals(Event.KeyboardEvent("[-]"))) {
					scene_view.size++;
					if (SceneViewPrefs.devMode) {
						Notify("SceneView zoom-out");
					}
					current.Use();
				}

				// front
				if (current.Equals(Event.KeyboardEvent("[1]"))) {
					ApplyRotation(Quaternion.Euler(0.0f, 0.0f, 0.0f));
					if (SceneViewPrefs.devMode) {
						Notify("Move SceneView to the back axis");
					}
					current.Use();
				}
				// back
				else if (current.Equals(Event.KeyboardEvent("^[1]")) || current.Equals(Event.KeyboardEvent("%[1]"))) {
					ApplyRotation(Quaternion.Euler(0.0f, 180.0f, 0.0f));
					if (SceneViewPrefs.devMode) {
						Notify("Move SceneView to the front axis");
					}
					current.Use();
				}
				// right
				else if (current.Equals(Event.KeyboardEvent("[3]"))) {
					ApplyRotation(Quaternion.Euler(0.0f, -90.0f, 0.0f));
					if (SceneViewPrefs.devMode) {
						Notify("Move SceneView to the right axis");
					}
					current.Use();
				}
				// left
				else if (current.Equals(Event.KeyboardEvent("^[3]")) || current.Equals(Event.KeyboardEvent("%[3]"))) {
					ApplyRotation(Quaternion.Euler(0.0f, 90.0f, 0.0f));
					if (SceneViewPrefs.devMode) {
						Notify("Move SceneView to the left axis");
					}
					current.Use();
				}
				// top
				else if (current.Equals(Event.KeyboardEvent("[7]"))) {
					ApplyRotation(Quaternion.Euler(90.0f, 0.0f, 0.0f));
					if (SceneViewPrefs.devMode) {
						Notify("Move SceneView to the top axis");
					}
					current.Use();
				}
				// bottom
				else if (current.Equals(Event.KeyboardEvent("^[7]")) || current.Equals(Event.KeyboardEvent("%[7]"))) {
					ApplyRotation(Quaternion.Euler(-90.0f, 0.0f, 0.0f));
					if (SceneViewPrefs.devMode) {
						Notify("Move SceneView to the bottom axis");
					}
					current.Use();
				}
				// add rotation x
				else if (current.Equals(Event.KeyboardEvent("[8]"))) {
					ApplyRotation(Quaternion.Euler(current_x + 15.0f, current_y, current_z), false);
					if (SceneViewPrefs.devMode) {
						Notify("Orbit: Up");
					}
					current.Use();
				}
				// subtract rotation x
				else if (current.Equals(Event.KeyboardEvent("[2]"))) {
					ApplyRotation(Quaternion.Euler(current_x - 15.0f, current_y, current_z), false);
					if (SceneViewPrefs.devMode) {
						Notify("Orbit: Down");
					}
					current.Use();
				}
				// add rotation y
				else if (current.Equals(Event.KeyboardEvent("[4]"))) {
					ApplyRotation(Quaternion.Euler(current_x, current_y + 15.0f, current_z), false);
					if (SceneViewPrefs.devMode) {
						Notify("Orbit: Left");
					}
					current.Use();
				}
				// subtract rotation y
				else if (current.Equals(Event.KeyboardEvent("[6]"))) {
					ApplyRotation(Quaternion.Euler(current_x, current_y - 15.0f, current_z), false);
					if (SceneViewPrefs.devMode) {
						Notify("Orbit: Right");
					}
					current.Use();
				}
				// add rotation z
				else if (current.Equals(Event.KeyboardEvent("^[4]")) || current.Equals(Event.KeyboardEvent("%[4]"))) {
					ApplyRotation(Quaternion.Euler(current_x, current_y, current_z + 15.0f), false);
					if (SceneViewPrefs.devMode) {
						Notify("Roll: Left");
					}
					current.Use();
				}
				// subtract rotation z
				else if (current.Equals(Event.KeyboardEvent("^[6]")) || current.Equals(Event.KeyboardEvent("%[6]"))) {
					ApplyRotation(Quaternion.Euler(current_x, current_y, current_z - 15.0f), false);
					if (SceneViewPrefs.devMode) {
						Notify("Roll: Right");
					}
					current.Use();
				}
				// align with the current camera
				else if (current.Equals(Event.KeyboardEvent("[0]"))) {
					if (Camera.main != null) {
						if (scene_view.in2DMode) {
							scene_view.in2DMode = Camera.main.orthographic;
						}
						Transform target = Camera.main.transform;
						scene_view.AlignViewToObject(target);
						if (SceneViewPrefs.devMode) {
							Notify("AlignViewToObject({0})", target);
						}
						current.Use();
					}
					else if (Object.FindObjectsOfType<Camera>().Length > 0) {
						Camera camera = Object.FindObjectsOfType<Camera>()[Object.FindObjectsOfType<Camera>().Length - 1];
						Transform target = camera.transform;
						if (scene_view.in2DMode) {
							scene_view.in2DMode = camera.orthographic;
						}
						scene_view.AlignViewToObject(target);
						if (SceneViewPrefs.devMode) {
							Notify("AlignViewToObject({0})", target);
						}
						current.Use();
					}
					else {
						Notify("There is no camera on the scene!");
					}
				}
				// toggle orthographic
				if (current.Equals(Event.KeyboardEvent("[5]"))) {
					if (scene_view.in2DMode == false) {
						scene_view.orthographic = !scene_view.orthographic;
						if (SceneViewPrefs.devMode) {
							Notify("Toggle orthographic view {0}", scene_view.orthographic);
						}
						current.Use();
					}
					else {
						scene_view.ShowNotification(new GUIContent("2D mode enabled, disable to perform this command."));
					}
				}
				Repaint();
				break;
			}
		}

		public static void Repaint() {
			scene_view.Repaint();
		}

		public static void Notify(string message) {
			scene_view.ShowNotification(new GUIContent(message));
		}

		public static void Notify(string message, params object[] arguments) {
			Notify(string.Format(message, arguments));
		}

		public static void DestroySelection() {
			if (Selection.gameObjects.Length > 0) {
				int count = 0;
				Undo.RecordObjects(Selection.gameObjects, "Destroy Objects");
				foreach (GameObject obj in Selection.gameObjects) {
					Undo.DestroyObjectImmediate(obj);
					count++;
				}
				if (SceneViewPrefs.devMode) {
					Notify("Destroyed {0} objects", count);
				}
			}
		}

		private static void ApplyRotation(Quaternion rotation, bool smooth = true) {
			if (scene_view.in2DMode == false) {
				if (smooth == false) {
					scene_view.LookAtDirect(scene_view.pivot, rotation);
				}
				else {
					scene_view.LookAt(scene_view.pivot, rotation);
				}
			}
			else {
				scene_view.ShowNotification(new GUIContent("2D mode enabled, disable to perform this command."));
			}

			Repaint();
		}

		public static void SaveCurrentGroup(string name = null) {
			SaveGroup(name, Selection.gameObjects);
		}

		public static void SaveGroup(string name = null, params GameObject[] objs) {
			if (objs.Length > 0) {
				string key = string.IsNullOrEmpty(name) ? string.Format("New Group [{0}]", data.groups.Count) : name;
				List<int> ids = new List<int>();
				foreach (Object obj in objs) {
					if (obj != null) {
						ids.Add(GetObjectID(obj));
					}
				}
				data.groups.Add(new GroupSlot(key, ids));
				SaveData();
			}
		}

		public static void LoadGroup(string name) {
			for (int id = 0; id < data.groups.Count; id++) {
				if (data.groups[id].name == name) {
					LoadGroup(id);
					return;
				}
			}
		}

		public static void LoadGroup(int index) {
			Selection.objects = data.groups[index].objs.ToArray();
		}

		public static void SaveCurrentView(string name = null) {
			string key = string.IsNullOrEmpty(name) ? string.Format("New Slot [{0}]", data.slots.Count) : name;
			List<int> ids = new List<int>();
			foreach (Object obj in Selection.gameObjects) {
				if (obj != null) {
					ids.Add(GetObjectID(obj));
				}
			}
			data.slots.Add(new ViewSlot(key, scene_view, true, ids));
			SaveData();
		}

		public static void LoadView(string name) {
			for (int id = 0; id < data.slots.Count; id++) {
				if (data.slots[id].name == name) {
					LoadView(id);
					return;
				}
			}
		}

		public static void LoadView(int index) {
			scene_view.in2DMode = data.slots[index].in2DMode;
			scene_view.m_AudioPlay = data.slots[index].audio;
			scene_view.m_SceneLighting = data.slots[index].lighting;
			scene_view.renderMode = data.slots[index].wireframe ? DrawCameraMode.Wireframe : scene_view.renderMode;
			if (scene_view.in2DMode == false) {
				scene_view.orthographic = data.slots[index].orthographic;
				scene_view.LookAt(data.slots[index].position, data.slots[index].rotation, data.slots[index].size);
			}
			if (data.slots[index].save_selection) {
				Selection.objects = GetObjectsByIDs(data.slots[index].objs).ToArray();
			}
			if (SceneViewPrefs.devMode) {
				Notify("{0} || {1}", data.slots[index].position, data.slots[index].rotation);
			}
		}

		public static void SaveData() {
			SceneViewPrefs.data = JsonUtility.ToJson(data);
		}

		public static void LoadData() {
			var data = JsonUtility.FromJson<SVPData>(SceneViewPrefs.data);
			if (data == null) {
				SceneViewPlus.data = new SVPData();
			}
			else {
				SceneViewPlus.data = data;
			}
		}
	}
}
