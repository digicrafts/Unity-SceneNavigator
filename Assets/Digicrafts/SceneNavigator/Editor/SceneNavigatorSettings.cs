#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Digicrafts.SceneNavigator {


	[Serializable]
	public static class SceneNavigatorSettings {

		public const string PREFIX = "DC.SceneNavigator.";

		[SerializeField]
		public static bool ShowNavigationUI = false;
		[SerializeField]
		public static bool ShowViewNavigationUI = true;
		[SerializeField]
		public static bool FocusOnMouseMove = true;
		[SerializeField]
		public static int RotationSnap = 10;
		[SerializeField]
		public static int ZoomSnap = 1;
		[SerializeField]
		public static bool AroundSelectedObject = false;
		[SerializeField]
		public static Dictionary<Action, Shortcut> Shortcuts;
		[SerializeField]
		public static Dictionary<string,Action> Keymaps;

		public static Dictionary<string,KeyCode> KeyCodeMap;

		// Private
		private static Vector2 _scrollPos;
		private static Shortcut _lastChangedShortcut;
//		private static GUIStyle _titleStyle;

		// Texture
		private static Texture _headerTexture;
		private static Texture _logoTexture;

		// Styles
		private static GUIStyle _smallTextStyle;
		private static GUIStyle _titleTextStyle;

		static SceneNavigatorSettings() {

			_headerTexture=(Texture)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Digicrafts/SceneNavigator/Editor/images/SceneNavigator_Header.png", typeof(Texture));
			_logoTexture=(Texture)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Digicrafts/SceneNavigator/Editor/images/SceneNavigator_Logo.png", typeof(Texture));

		}

		public static void OnGUI() {


			// Setup Styles
			_smallTextStyle = new GUIStyle(GUI.skin.label);
			_smallTextStyle.fontSize=9;
			_smallTextStyle.margin=new RectOffset(0,0,0,0);
			_smallTextStyle.padding=new RectOffset(0,5,0,0);
			_smallTextStyle.alignment=TextAnchor.UpperRight;
			_smallTextStyle.active.background=Texture2D.blackTexture;//new Texture2D(8,8);

			_titleTextStyle = new GUIStyle(GUI.skin.box);
			_titleTextStyle.alignment=TextAnchor.MiddleCenter;
			_titleTextStyle.fontSize=14;
			_titleTextStyle.fontStyle=FontStyle.Bold;

			GUIStyle logoStyle= new GUIStyle(GUI.skin.box);
			logoStyle.margin=new RectOffset(0,0,0,0);
			logoStyle.padding=new RectOffset(0,0,0,0);

			// Check if last shortcut changed
			if(_lastChangedShortcut != null){
				KeyCode currentKey=Event.current.keyCode;
//				Debug.Log("key code " + currentKey);
				_lastChangedShortcut.keyCode=currentKey;
				_lastChangedShortcut=null;
			}
			_scrollPos = GUILayout.BeginScrollView(_scrollPos);
			GUILayout.BeginVertical("box");

			/// Title
			/// 
			Color savedColor = GUI.backgroundColor;
			GUI.backgroundColor=Color.black;
			GUILayout.Box(_logoTexture,logoStyle,GUILayout.ExpandWidth(true),GUILayout.Height(70));
			GUILayout.Label("\nversion " + SceneNavigator.Version + "\n",_smallTextStyle,GUILayout.ExpandWidth(true));
			GUI.backgroundColor=savedColor;

			/// -- Start of settings UI
			GUILayout.Box("General Settings",_titleTextStyle,GUILayout.ExpandWidth(true));
			GUILayout.Space(10);
			ShowNavigationUI = GUILayout.Toggle(ShowNavigationUI,"Show Navigation UI");
			ShowViewNavigationUI = GUILayout.Toggle(ShowViewNavigationUI,"Show View Navigation Toolbar");
			AroundSelectedObject = GUILayout.Toggle(AroundSelectedObject,"Local Transform Mode");
			FocusOnMouseMove = GUILayout.Toggle(FocusOnMouseMove,"Focus Keyboard on Mousemove");
			GUILayout.Label("Rotation Steps: " + RotationSnap.ToString());
			RotationSnap = (int)GUILayout.HorizontalSlider((int)RotationSnap,1,90);
			GUILayout.Label("Zoom Steps: " + ZoomSnap.ToString());
			ZoomSnap = (int)GUILayout.HorizontalSlider((int)ZoomSnap,1,5);
//			GUILayout.EndVertical();

			#region - Shortcuts -
			GUILayout.Space(10);
			GUILayout.Box("Keyboard Shortcuts",_titleTextStyle,GUILayout.ExpandWidth(true));
			// header
			GUILayout.BeginHorizontal();
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.margin=new RectOffset(3,10,10,3);
			style.fontStyle=FontStyle.Bold;
			GUILayout.Label("Shortcut",style,GUILayout.Height(29));
			GUILayout.Box(_headerTexture,GUI.skin.label,GUILayout.Width(66),GUILayout.Height(29));
			GUILayout.Label("Key",style,GUILayout.Width(70));
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			foreach(KeyValuePair<Action, Shortcut> item in Shortcuts){
				string tempKeyString;
				Shortcut sc = item.Value;
				GUILayout.BeginHorizontal();
				GUILayout.Label(sc.label);
				sc.command=GUILayout.Toggle(sc.command,"",GUILayout.Width(12));
				sc.alt=GUILayout.Toggle(sc.alt,"",GUILayout.Width(12));
				sc.control=GUILayout.Toggle(sc.control,"",GUILayout.Width(12));
				sc.shift=GUILayout.Toggle(sc.shift,"",GUILayout.Width(12));
				tempKeyString=GUILayout.TextField(sc.keyCode.ToString(),GUILayout.Width(80));
				GUILayout.EndHorizontal();
				if(tempKeyString!=sc.keyCode.ToString()){	
					if( tempKeyString==KeyCode.Escape.ToString() ||
						tempKeyString==KeyCode.Tab.ToString() ||
						tempKeyString==KeyCode.Space.ToString() ||
						tempKeyString==KeyCode.RightShift.ToString() ||
						tempKeyString==KeyCode.LeftShift.ToString() ||
						tempKeyString==KeyCode.RightAlt.ToString() ||
						tempKeyString==KeyCode.LeftAlt.ToString() ||
						tempKeyString==KeyCode.Backspace.ToString() ||
						tempKeyString==KeyCode.Delete.ToString()){
						// Do Nothing
					} else {
						sc.keyCode=Event.current.keyCode;
						_lastChangedShortcut=sc;
//					Debug.Log("key code G"  + " new " + Event.current.keyCode);
					}
				}
			}
			if(GUILayout.Button("Reset Default")){
				ResetShortcuts();
			}
			#endregion - Shortcuts -

			GUILayout.EndVertical();
			GUILayout.EndScrollView();

		}			

		/// <summary>
		/// Write settings to EditorPrefs.
		/// </summary>
		public static void Write() {


//			Debug.Log("Write");

			// Save to editor Prefs
			EditorPrefs.SetBool(Prefix("ShowNavigationUI"),ShowNavigationUI);
			EditorPrefs.SetBool(Prefix("ShowViewNavigationUI"),ShowViewNavigationUI);
			EditorPrefs.SetBool(Prefix("FocusOnMouseMove"),FocusOnMouseMove);
			EditorPrefs.SetInt(Prefix("RotationSnap"),RotationSnap);
			EditorPrefs.SetInt(Prefix("ZoomSnap"),ZoomSnap);
			EditorPrefs.SetBool(Prefix("AroundSelectedObject"),AroundSelectedObject);

			// Loop the shortcut dictionary and save
			foreach(KeyValuePair<Action, Shortcut> item in Shortcuts)
			{				
				Shortcut sc = item.Value;
				EditorPrefs.SetString(Prefix(sc.action.ToString()),sc.name);
			}

			RebuildKeymaps();
		}

		/// <summary>
		/// Read settings from EditorPrefs.
		/// </summary>
		public static void Read() {

//			Debug.Log("Read");

			ShowNavigationUI = EditorPrefs.HasKey(Prefix("ShowNavigationUI"))?EditorPrefs.GetBool(Prefix("ShowNavigationUI")):true;
			ShowViewNavigationUI = EditorPrefs.HasKey(Prefix("ShowViewNavigationUI"))?EditorPrefs.GetBool(Prefix("ShowViewNavigationUI")):true;
			FocusOnMouseMove = EditorPrefs.HasKey(Prefix("FocusOnMouseMove"))?EditorPrefs.GetBool(Prefix("FocusOnMouseMove")):true;
			RotationSnap = EditorPrefs.HasKey(Prefix("RotationSnap"))?EditorPrefs.GetInt(Prefix("RotationSnap")):10;
			ZoomSnap = EditorPrefs.HasKey(Prefix("ZoomSnap"))?EditorPrefs.GetInt(Prefix("ZoomSnap")):1;
			AroundSelectedObject = EditorPrefs.HasKey(Prefix("AroundSelectedObject"))?EditorPrefs.GetBool(Prefix("AroundSelectedObject")):false;

			// Reset Shortcuts
			ResetShortcuts();

			// Build Keycode map
			if(KeyCodeMap==null){
				KeyCodeMap = new Dictionary<string, KeyCode>();
				foreach (KeyCode enumValue in Enum.GetValues(typeof(KeyCode)))
				{
					string key = enumValue.ToString();
					//				Debug.Log(enumValue.ToString());
					if(!KeyCodeMap.ContainsKey(key))
						KeyCodeMap.Add(key,enumValue); 
				}
			}

			// Loop the shortcut dictionary and build Keymaps
			Keymaps = new Dictionary<string, Action>();
			foreach(KeyValuePair<Action, Shortcut> item in Shortcuts)
			{				
				Shortcut sc = item.Value;
				sc.action=item.Key;
				string key = Prefix(sc.action.ToString());
				if(EditorPrefs.HasKey(key)){
					string name = EditorPrefs.GetString(key);
//					Debug.Log("name: " +name);
					sc.SetName(name);
					if(!Keymaps.ContainsKey(sc.name))
						Keymaps.Add(sc.name,item.Key);
				}					
			}
				
		}


		/// <summary>
		/// Prefix the specified name.
		/// </summary>
		/// <param name="name">Name.</param>

		private static string Prefix(string name) {
			return PREFIX + name;
		}

		/// <summary>
		/// Rebuilds the keymaps.
		/// </summary>
		private static void RebuildKeymaps(){
			
			// Loop the shortcut dictionary and build Keymaps
			Keymaps = new Dictionary<string, Action>();
			foreach(KeyValuePair<Action, Shortcut> item in Shortcuts)
			{
				Shortcut sc = item.Value;
				sc.name=sc.GetName();
				sc.action=item.Key;
//				Debug.Log("Shortcut name: " + sc.name);
				if(!Keymaps.ContainsKey(sc.name))
					Keymaps.Add(sc.name,item.Key);
			}

		}

		/// <summary>
		/// Resets the shortcuts.
		/// </summary>
		private static void ResetShortcuts() {
			// Build the shortcust dictionary
			Shortcuts = new Dictionary<Action,Shortcut>(){
				{Action.VIEW_FRONT,new Shortcut("Front View",KeyCode.Keypad5)},
				{Action.VIEW_BOTTOM,new Shortcut("Bottom View",KeyCode.Keypad3)},
				{Action.VIEW_TOP,new Shortcut("Top View",KeyCode.Keypad7)},
				{Action.VIEW_LEFT,new Shortcut("Left View",KeyCode.Keypad1)},
				{Action.VIEW_RIGHT,new Shortcut("Right View",KeyCode.Keypad9)},
				{Action.VIEW_BACK,new Shortcut("Back View",KeyCode.Keypad5,false,false,true)},
				{Action.VIEW_ROTATE_UP,new Shortcut("Rotate up",KeyCode.Keypad8)},
				{Action.VIEW_ROTATE_DOWN,new Shortcut("Rotate down",KeyCode.Keypad2)},
				{Action.VIEW_ROTATE_RIGHT,new Shortcut("Rotate right",KeyCode.Keypad6)},
				{Action.VIEW_ROTATE_LEFT,new Shortcut("Rotate left",KeyCode.Keypad4)},
				{Action.ZOOM_IN,new Shortcut("Zoom in",KeyCode.KeypadPlus)},
				{Action.ZOOM_OUT,new Shortcut("Zoom out",KeyCode.KeypadMinus)},
				{Action.TOGGLE_PERSPECTIVE,new Shortcut("Perspective",KeyCode.KeypadDivide)},
				{Action.TOGGLE_2D,new Shortcut("2D/3D Mode",KeyCode.KeypadMultiply)},
				{Action.SAVE_CURRENT_VIEW,new Shortcut("Save View",KeyCode.PageUp)},
				{Action.RESTORE_SAVED_VIEW,new Shortcut("Restore View",KeyCode.PageDown)},
				//				{Action.RESTORE_VIEW,new Shortcut("Resotre",KeyCode.A)},
				//				{Action.FOCUS_OBJECT,new Shortcut("Focus",KeyCode.C)},
				{Action.CENTER,new Shortcut("Center View",KeyCode.Keypad0)},
				{Action.RELATIVE_OBJECT,new Shortcut("Transform Mode",KeyCode.KeypadPeriod)}
				//				{Action.SHOW_ALL,new Shortcut("Show All",KeyCode.F)}
				//
			};		
		}

//		/// <summary>
//		/// Utility function for retrieving axis locking settings at runtime.
//		/// </summary>
//		/// <param name="doF"></param>
//		/// <param name="axis"></param>
//		/// <returns></returns>
//		public static bool GetLock(DoF doF, Axis axis) {
//			Locks translationLocks = Mode == OperationMode.Fly || Mode == OperationMode.Orbit ? NavTranslationLock : ManipulateTranslationLock;
//			Locks rotationLocks = Mode == OperationMode.Fly || Mode == OperationMode.Orbit ? NavRotationLock : ManipulateRotationLock;
//			Locks locks = doF == DoF.Translation ? translationLocks : rotationLocks;
//
//			switch (axis) {
//				case Axis.X:
//					return (locks.X || locks.All) && !Application.isPlaying;
//				case Axis.Y:
//					return (locks.Y || locks.All) && !Application.isPlaying;
//				case Axis.Z:
//					return (locks.Z || locks.All) && !Application.isPlaying;
//				default:
//					throw new ArgumentOutOfRangeException("axis");
//			}
//		}
	}
}

#endif //UNITY_EDITOR