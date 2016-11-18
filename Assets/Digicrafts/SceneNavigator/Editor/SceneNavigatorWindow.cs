#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace Digicrafts.SceneNavigator {

	[Serializable]
	public class SceneNavigatorWindow : EditorWindow, IDisposable {

		/// <summary>
		/// Initializes the window.
		/// </summary>
		[MenuItem("Window/Digicrafts/Scene Navigator")]
		public static void Init() {
			SceneNavigatorWindow window = GetWindow(typeof(SceneNavigatorWindow)) as SceneNavigatorWindow;
			if (window) {
				window.Show();
			}
		}

		public void OnDisable() {
//			Debug.Log("On disable");
			SceneNavigatorSettings.Write();
		}

		public void OnDestroy() {
//			Debug.Log("On destroy");
			SceneNavigatorSettings.Write();
		}

		// This does not get called, unfortunately...
		public void OnApplicationQuit() {
			SceneNavigator.OnApplicationQuit();
		}

		public void OnSelectionChange() {
//			ViewportController.StoreSelectionTransforms();
		}

		public void OnGUI() {
			SceneNavigatorSettings.OnGUI();
		}

		public void Dispose() {
			SceneNavigatorSettings.Write();
		}
	}
}
#endif