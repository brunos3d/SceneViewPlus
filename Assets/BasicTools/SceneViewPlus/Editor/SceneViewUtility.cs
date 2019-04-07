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

	internal class SceneViewUtility {
		public static void ShowIconSelector(Object target, Rect activatorRect, bool showLabelIcons) {
			var type = typeof(Editor).Assembly.GetType("UnityEditor.IconSelector");
			var instance = ScriptableObject.CreateInstance(type);
			var parameters = new object[3];

			parameters[0] = target;
			parameters[1] = activatorRect;
			parameters[2] = showLabelIcons;

			type.InvokeMember("Init", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, instance, parameters);
		}
	}
}