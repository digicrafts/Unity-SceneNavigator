#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Digicrafts.SceneNavigator {

	/// <summary>
	/// Action.
	/// </summary>
	/// 
	public enum Action {
		VIEW_FRONT,
		VIEW_BOTTOM,
		VIEW_TOP,
		VIEW_LEFT,
		VIEW_RIGHT,
		VIEW_BACK,
		VIEW_ROTATE_UP,
		VIEW_ROTATE_DOWN,
		VIEW_ROTATE_LEFT,
		VIEW_ROTATE_RIGHT,
		ZOOM_IN,
		ZOOM_OUT,
		TOGGLE_PERSPECTIVE,
		TOGGLE_2D,
		SAVE_CURRENT_VIEW,
		RESTORE_SAVED_VIEW,
		RESTORE_VIEW,
		FOCUS_OBJECT,
		CENTER,
		SHOW_ALL,
		RELATIVE_OBJECT
	}

	/// <summary>
	/// Shortcut.
	/// </summary>
	/// 
	[Serializable]
	public class Shortcut {

		public string name;
		public string label = "label";
		public bool command = false;
		public bool control = false;
		public bool alt = false;
		public bool shift = false;
		public KeyCode keyCode = KeyCode.None;
		public Action action;

		public Shortcut(string label, KeyCode key, bool command = false, bool control = false, bool alt = false, bool shift = false) {

			this.label =label;
			this.keyCode = key;
			this.command = command;
			this.control = control;
			this.alt = alt;
			this.shift = shift;

			name = Shortcut.GetName(key,command,control,alt,shift);
		}

		public string GetName(){
			
			return Shortcut.GetName(keyCode,command,control,alt,shift);
		}

		public void SetName(string shortcutName){
			///
			// Format KEY_x_x_x_x
			// convert to Shortcut
			char[] sp = {'_'};
			string[] k = shortcutName.Split(sp);

			if(k.Length == 5){
//				Debug.Log("SetName: " + k[0]);
				command=(k[1]=="1");
				control=(k[2]=="1");
				alt=(k[3]=="1");
				shift=(k[4]=="1");
				keyCode = SceneNavigatorSettings.KeyCodeMap[ k[0] ];
			}
			this.name=shortcutName;
//			Debug.Log("shortcutName :" + shortcutName + " Key code: " + keyCode);
		}
		public static string GetName(KeyCode key, bool command = false, bool control = false, bool alt = false, bool shift = false){
			//			string k = key.ToString()+"_"+command*1000+control*100+alt*10+1*shift;
			return (key.ToString()+"_"+((command?1:0)+"_"+(control?1:0)+"_"+(alt?1:0)+"_"+(shift?1:0)));
		}


	}

	[InitializeOnLoad]
	[Serializable]
	class SceneNavigator {

		public static string Version = "1.0.0";

		// Rig components
		private static GameObject _pivotGO, _cameraGO;
		private static Transform _pivot, _camera;
		private const string PivotName = "Scene camera pivot dummy";
		private const string CameraName = "Scene camera dummy";

		// Texture
		private static Texture _zoomInTexture;
		private static Texture _zoomOutTexture;
		private static Texture _centerTexture;
		// UI
		private static string _viewportName = "global";
		private static int _viewportNameIndex = -1;
		private static string[] _viewportNameString = new string[]{"Front","Back","Top","Bottom","Left","Right"};
		private static string _viewportMode = "perspective";
		private static string _viewportPerspective = "perspective";

		// Pirvate
		private static string _lastShortcutName = null;
		private static bool _hasSavedPosition = false;
		private static Vector3 _savedPivotPosition;
		private static Quaternion _savedPivotRotation;
		private static Vector3 _savedCameraPosition;
		private static Quaternion _savedCameraRotation;

		static SceneNavigator() {
			
			// Set up callbacks.
			EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
			SceneView.onSceneGUIDelegate += OnSceneGUI;

			// Setup Texture
			_zoomInTexture=(Texture)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Digicrafts/SceneNavigator/Editor/images/SceneNavigator_Zoom_In.png", typeof(Texture));
			_zoomOutTexture=(Texture)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Digicrafts/SceneNavigator/Editor/images/SceneNavigator_Zoom_Out.png", typeof(Texture));
			_centerTexture=(Texture)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Digicrafts/SceneNavigator/Editor/images/SceneNavigator_Center.png", typeof(Texture));			

			// Initialize.
			SceneNavigatorSettings.Read();
			InitCameraRig();
			StoreSelectionTransforms();

		}

		#region - Callbacks -
		private static void OnSceneGUI(SceneView sceneView){
				
			// Set the text
			_viewportMode = sceneView.in2DMode?"2D":"3D";
			_viewportPerspective = sceneView.orthographic?"Orthographic":"Perspective";

			// Selected Object
			if(SceneNavigatorSettings.AroundSelectedObject){
				if(Selection.gameObjects.Length>1){
					_viewportName = " < local >";
					//				GUILayout.Label("< multiple object >");
				} else if(Selection.activeGameObject){
//					_viewportName = " < "+Selection.activeGameObject.name+" >";
					_viewportName = " < local >";
					//				GUILayout.Label("< "+Selection.activeGameObject.name+" >");
				} else {
					_viewportName = " < global >";
					//				GUILayout.Label("< free >");
				};				
			} else {
				_viewportName = " < global >";
			}

			// Begin drawing UI
			Handles.BeginGUI();
			GUILayout.BeginVertical();
			GUILayout.FlexibleSpace();

			///-- Navigation UI

			if(SceneNavigatorSettings.ShowNavigationUI){
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				GUILayout.BeginHorizontal(GUILayout.Width(100));

				if(GUILayout.Button(_zoomInTexture,GUI.skin.label,GUILayout.Width(30),GUILayout.Height(30)) ){
					PerformAction(Action.ZOOM_IN,sceneView);
				}
				if(GUILayout.Button(_zoomOutTexture,  GUI.skin.label,GUILayout.Width(30),GUILayout.Height(30))){
					PerformAction(Action.ZOOM_OUT,sceneView);
				}
				if(GUILayout.Button(_centerTexture,GUI.skin.label,GUILayout.Width(30),GUILayout.Height(30)) ){
					Center(sceneView,Vector3.zero);
				}
				GUILayout.EndHorizontal();
				GUILayout.EndHorizontal();
			}
				
			/// View Mode Buttons

			GUILayout.BeginHorizontal();

			// View Mode
			//			GUILayout.Label(_viewportMode + " / " + _viewportName + " (" + _viewportPerspective + ")");
			GUILayout.Label(_viewportMode + " / " + _viewportPerspective + _viewportName);
			// space
			GUILayout.FlexibleSpace();
			///-- View Navigation UI
			if(SceneNavigatorSettings.ShowViewNavigationUI){
				int tempViewportNameIndex = GUILayout.Toolbar(_viewportNameIndex, _viewportNameString);

				if(_viewportNameIndex!=tempViewportNameIndex){					
					switch(tempViewportNameIndex){
					case 0:PerformAction(Action.VIEW_FRONT,sceneView);break;
					case 1:PerformAction(Action.VIEW_BACK,sceneView);break;						
					case 2:PerformAction(Action.VIEW_TOP,sceneView);break;
					case 3:PerformAction(Action.VIEW_BOTTOM,sceneView);break;
					case 4:PerformAction(Action.VIEW_LEFT,sceneView);break;
					case 5:PerformAction(Action.VIEW_RIGHT,sceneView);break;
					}
				}
			}

			/// Save/Restore View
			/// 
			if(GUILayout.Button("Save")){
				PerformAction(Action.SAVE_CURRENT_VIEW,sceneView);
			}
			GUI.enabled=_hasSavedPosition;
			if(GUILayout.Button("Restore")){
				PerformAction(Action.RESTORE_SAVED_VIEW,sceneView);
			}
			GUI.enabled=true;

			GUILayout.EndHorizontal();// end bottom panel 

			GUILayout.Space (25);
			GUILayout.EndVertical();
			Handles.EndGUI();


			// Process Events

			Event e = Event.current;
			if (e != null){
//				Debug.Log("Event: " + e.type);
				if(e.type==EventType.KeyDown && e.isKey){
					// Ignore invalid keycode
					if(e.keyCode == KeyCode.None){
						
						_lastShortcutName = null;

//						Debug.Log("Keycode none");
//						Debug.Log("Camera " + SceneView.lastActiveSceneView.camera.transform.position + " Rotation " + SceneView.lastActiveSceneView.camera.transform.rotation +
//							" Pivot " + SceneView.lastActiveSceneView.pivot + " Rotation " + SceneView.lastActiveSceneView.rotation);
						
					} else {
					
//						Debug.Log("Key " + e.keyCode);
						string shortcutName = Shortcut.GetName(e.keyCode,e.command,e.control,e.alt,e.shift);

						// Check if already pressed key
						if( shortcutName != _lastShortcutName){
							_lastShortcutName = shortcutName;
							if(SceneNavigatorSettings.Keymaps.ContainsKey(shortcutName)){
//								Debug.Log("Key pressed in editor: " + e.keyCode + " name: " + _lastShortcutName + " action: " + SceneNavigatorSettings.Keymaps[shortcutName]);
								PerformAction(SceneNavigatorSettings.Keymaps[shortcutName],sceneView);
							}
						}
					}  
				} else if(e.type==EventType.MouseMove && SceneNavigatorSettings.FocusOnMouseMove){
					sceneView.Focus();
				}

			} 			


		}

		private static void PlaymodeStateChanged() {
//			if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
//				Settings.Write();
		}

		public static void OnApplicationQuit() {
//			Settings.Write();
			DisposeCameraRig();
//			SpaceNavigator.Instance.Dispose();
		}
		#endregion - Callbacks -

		#region - Action -
		static void PerformAction(Action action, SceneView sceneView) {

			switch(action){
			// General
			case Action.TOGGLE_PERSPECTIVE:
				if(!sceneView.in2DMode)
				sceneView.orthographic=!sceneView.orthographic;
				break;
			case Action.TOGGLE_2D:
				sceneView.in2DMode=!sceneView.in2DMode;
				break;			
			case Action.CENTER:				
				Center(sceneView, Vector3.zero);
				break;
			// View
			case Action.VIEW_FRONT:				
				ChangeView(sceneView,0,new Vector3(0,0,-1),new Vector3(0,0,0));
				break;
			case Action.VIEW_BACK:
				ChangeView(sceneView,1,new Vector3(0,0,1),new Vector3(0,180,0));
				break;
			case Action.VIEW_TOP:
				ChangeView(sceneView,2,new Vector3(0,1,0),new Vector3(90,0,0));
				break;
			case Action.VIEW_BOTTOM:
				ChangeView(sceneView,3,new Vector3(0,-1,0),new Vector3(-90,0,0));
				break;
			case Action.VIEW_LEFT:
				ChangeView(sceneView,4,new Vector3(-1,0,0),new Vector3(0,-270,0));
				break;
			case Action.VIEW_RIGHT:
				ChangeView(sceneView,5,new Vector3(1,0,0),new Vector3(0,-90,0));
				break;
			// Rotate
			case Action.VIEW_ROTATE_UP:								
				RotateAround(sceneView,_pivot.position,_camera.right,SceneNavigatorSettings.RotationSnap);
				break;
			case Action.VIEW_ROTATE_DOWN:				
				RotateAround(sceneView,_pivot.position,_camera.right,-SceneNavigatorSettings.RotationSnap);
				break;
			case Action.VIEW_ROTATE_LEFT:				
				RotateAround(sceneView,_pivot.position,Vector3.up,SceneNavigatorSettings.RotationSnap);
				break;
			case Action.VIEW_ROTATE_RIGHT:				
				RotateAround(sceneView,_pivot.position,Vector3.up,-SceneNavigatorSettings.RotationSnap);
				break;
			// Zoom
			case Action.ZOOM_IN:
				SyncRigWithScene();
				if(sceneView.orthographic) {
					sceneView.size-=SceneNavigatorSettings.ZoomSnap;
				} else {					
					_camera.position=Vector3.MoveTowards(_camera.position,_pivot.position,SceneNavigatorSettings.ZoomSnap);				
				}
				RepaintView(sceneView);
				break;
			case Action.ZOOM_OUT:
				SyncRigWithScene();
				if(sceneView.orthographic) {
					sceneView.size+=SceneNavigatorSettings.ZoomSnap;
				} else {
					_camera.position=Vector3.MoveTowards(_camera.position,_pivot.position,-SceneNavigatorSettings.ZoomSnap);
				}
				RepaintView(sceneView);
				break;
			case Action.SAVE_CURRENT_VIEW:
				SyncRigWithScene();
				_savedCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;
				_savedCameraRotation = SceneView.lastActiveSceneView.camera.transform.rotation;
				_savedPivotPosition = SceneView.lastActiveSceneView.pivot;
				_savedPivotRotation = SceneView.lastActiveSceneView.rotation;
				_hasSavedPosition=true;
				break;
			case Action.RESTORE_SAVED_VIEW:
//				SyncRigWithScene();
//				Debug.Log("_savedCameraPosition " + _savedCameraPosition);
				_camera.position = _savedCameraPosition;
				_camera.rotation = _savedCameraRotation;
				_pivot.position = _savedPivotPosition;
				_pivot.rotation = _savedPivotRotation;
				RepaintView(sceneView);
				_lastShortcutName="";
				break;
			case Action.RELATIVE_OBJECT:
				SceneNavigatorSettings.AroundSelectedObject=!SceneNavigatorSettings.AroundSelectedObject;
//				Debug.Log("SceneNavigatorSettings.AroundSelectedObject: " + SceneNavigatorSettings.AroundSelectedObject);
				break;
			}
		}
		#endregion - Action -

		#region - Navigation -
		static void RotateAround(SceneView sceneView, Vector3 point, Vector3 axis, float angle) {
			if(!sceneView.in2DMode){
				_viewportName="global";
				SyncRigWithScene();
				if(SceneNavigatorSettings.AroundSelectedObject && Selection.gameObjects.Length > 0){					
					point = Tools.handlePosition;
					_camera.LookAt(point);
				}
				_camera.RotateAround(point,axis,angle);
				RepaintView(sceneView);
			}
		}
		static void ChangeView(SceneView sceneView, int index,Vector3 position, Vector3 rotation) {			
			_viewportNameIndex=index;
			_viewportName=_viewportNameString[index];
			SyncRigWithScene();
			float distance = Vector3.Distance(_pivot.position,_camera.position);
			if(SceneNavigatorSettings.AroundSelectedObject && Selection.gameObjects.Length > 0){				
				_camera.position = new Vector3(
					(position.x==0)?Tools.handlePosition.x:position.x*distance,
					(position.y==0)?Tools.handlePosition.y:position.y*distance,
					(position.z==0)?Tools.handlePosition.z:position.z*distance);
			} else {
				_camera.position = new Vector3(
					(position.x==0)?_pivot.position.x:position.x*distance,
					(position.y==0)?_pivot.position.y:position.y*distance,
					(position.z==0)?_pivot.position.z:position.z*distance);
			}
			_camera.rotation = Quaternion.Euler(rotation);
			RepaintView(sceneView);
		}
		static void Center(SceneView sceneView, Vector3 target) {
			SyncRigWithScene();
			_camera.position = _camera.position - (_pivot.position - target);
			RepaintView(sceneView);
		}

		public static void StraightenHorizon() {
			_camera.rotation = Quaternion.Euler(_camera.rotation.eulerAngles.x, _camera.rotation.eulerAngles.y, 0);

			// Update sceneview pivot and repaint view.
			SceneView.lastActiveSceneView.pivot = _pivot.position;
			SceneView.lastActiveSceneView.rotation = _pivot.rotation;
			SceneView.lastActiveSceneView.Repaint();
		}
		#endregion - Navigation -


		#region - Dummy Camera Rig -
		private static void InitCameraRig() {
			_cameraGO = GameObject.Find(CameraName);
			_pivotGO = GameObject.Find(PivotName);
			// Create camera rig if one is not already present.
			if (!_pivotGO) {
				_cameraGO = new GameObject(CameraName) { hideFlags = HideFlags.HideAndDontSave };
				_pivotGO = new GameObject(PivotName) { hideFlags = HideFlags.HideAndDontSave };
			}
			// Reassign these variables, they get destroyed when entering play mode.
			_camera = _cameraGO.transform;
			_pivot = _pivotGO.transform;
			_pivot.parent = _camera;

			SyncRigWithScene();
		}
		private static void SyncRigWithScene() {
			if (SceneView.lastActiveSceneView) {
				_camera.position = SceneView.lastActiveSceneView.camera.transform.position; // <- this value changes w.r.t. pivot !
				_camera.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
				_pivot.position = SceneView.lastActiveSceneView.pivot;
				_pivot.rotation = SceneView.lastActiveSceneView.rotation;
			}
		}
		static void RepaintView(SceneView sceneView){

			// Update sceneview pivot and repaint view.
//			if (sceneView) {
			sceneView.pivot = _pivot.position;
			sceneView.rotation = _pivot.rotation;
			sceneView.Repaint();
//			}

		}
		private static void DisposeCameraRig() {
			Object.DestroyImmediate(_cameraGO);
			Object.DestroyImmediate(_pivotGO);
		}
		#endregion - Dummy Camera Rig -


		#region - Snapping -
		public static void StoreSelectionTransforms() {
//			_unsnappedRotations.Clear();
//			_unsnappedTranslations.Clear();
//			foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
//				_unsnappedRotations.Add(transform, transform.rotation);
//				_unsnappedTranslations.Add(transform, transform.position);
//			}
		}
		private static Quaternion SnapRotation(Quaternion q, float snap) {
			Vector3 euler = q.eulerAngles;
			return Quaternion.Euler(
				Mathf.RoundToInt(euler.x / snap) * snap,
				Mathf.RoundToInt(euler.y / snap) * snap,
				Mathf.RoundToInt(euler.z / snap) * snap);
		}
		private static Vector3 SnapTranslation(Vector3 v, float snap) {
			return new Vector3(
				Mathf.RoundToInt(v.x / snap) * snap,
				Mathf.RoundToInt(v.y / snap) * snap,
				Mathf.RoundToInt(v.z / snap) * snap);
		}
		#endregion - Snapping -
	}
}
#endif