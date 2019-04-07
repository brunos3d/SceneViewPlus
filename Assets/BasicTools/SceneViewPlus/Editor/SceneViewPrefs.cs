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

	internal class SceneViewPrefs {
		public static readonly string versionInfo = "1.0.0";

		private const string dataPref = "BasicToolsSceneViewPlusData";

		private const string enabledPref = "BasicToolsSceneViewPlusEnabled";
		private const string devModePref = "BasicToolsSceneViewPlusDevMode";
		private const string showInfoPref = "BasicToolsSceneViewPlusShowInfo";
		private const string mouseFocusPref = "BasicToolsSceneViewPlusMouseFocus";

		public static string data {
			get {
				return EditorPrefs.GetString(dataPref);
			}
			set {
				EditorPrefs.SetString(dataPref, value);
			}
		}

		public static void ResetData() {
			EditorPrefs.DeleteKey(dataPref);
			SceneViewPlus.SaveData();
		}

		public static bool enabled {
			get {
				return EditorPrefs.GetBool(enabledPref, true);
			}
			set {
				EditorPrefs.SetBool(enabledPref, value);
			}
		}

		public static bool devMode {
			get {
				return EditorPrefs.GetBool(devModePref, false);
			}
			set {
				EditorPrefs.SetBool(devModePref, value);
			}
		}

		public static bool showInfo {
			get {
				return EditorPrefs.GetBool(showInfoPref, false);
			}
			set {
				EditorPrefs.SetBool(showInfoPref, value);
			}
		}

		public static bool mouseFocus {
			get {
				return EditorPrefs.GetBool(mouseFocusPref, true);
			}
			set {
				EditorPrefs.SetBool(mouseFocusPref, value);
			}
		}
	}
}