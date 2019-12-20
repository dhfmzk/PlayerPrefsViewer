using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OrcaAssist {
    public class PlayerPrefsViewerWindow : EditorWindow {

        [MenuItem("OrcaAssist/PlayerPrefs Viewer")]
        private static void Init() {

            var editor = GetWindow<PlayerPrefsViewerWindow>(false, "Prefs Editor");

            Vector2 minSize     = editor.minSize;
            minSize.x           = 230;
            editor.minSize      = minSize;
        }

        bool isPlayerPrefs = true;

        private void OnEnable() {
        }

        private void OnGUI() {
            DrawTopSection();

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh")) {
                LoadPrefsData();
            }

            DrawResult();
            DrawEditTool();
        }

        private void DrawTopSection() {
            EditorGUILayout.Space();

            // Prefs Type Select
            var oldIndex = isPlayerPrefs ? 0 : 1;
            var newIndex = GUILayout.Toolbar(oldIndex, new string[] { "PlayerPrefs", "EditorPrefs" });
            isPlayerPrefs = newIndex == 0 ? true : false;

            // Type filter tap section
            if (isPlayerPrefs) {
                DrawPlayerPrefsTab();
            }
            else {
                DrawEditorPrefsTab();
            }
        }

        private int drawPlayerTabIndex = 0;
        private string[] playerTapArray = new string[] { "All", "String", "Float", "Int" };
        private void DrawPlayerPrefsTab() {
            drawPlayerTabIndex = GUILayout.Toolbar(drawPlayerTabIndex, playerTapArray);
        }

        private int drawEditorTabIndex = 0;
        private string[] editorTapArray = new string[] { "All", "String", "Float", "Int", "Bool" };
        private void DrawEditorPrefsTab() {
            drawEditorTabIndex = GUILayout.Toolbar(drawEditorTabIndex, editorTapArray);
        }

        private int CurrentFilterIndex => isPlayerPrefs ? drawPlayerTabIndex : drawEditorTabIndex;

        Vector2 scrollPosition = new Vector2();
        List<string[]> filterScheme = new List<string[]>() { 
            new string[] {"String", "Float", "Int", "Bool"},
            new string[] {"String"},
            new string[] {"Float"},
            new string[] {"Int"},
            new string[] {"Bool"},
        };
        private void DrawResult() {
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (scrollPosition.y < 0) { scrollPosition.y = 0; }

            targetDataList
                ?.Where(e => filterScheme[CurrentFilterIndex].Contains(GetType(e.Value)))
                ?.ToList()
                ?.ForEach(e => {
                    var data = e;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.TextField(GetType(data.Value), GUILayout.Width(37));
                    EditorGUILayout.TextField(data.Key);
                    if (GetType(data.Value).Equals("String")) {
                        EditorGUILayout.TextField(data.Value.ToString());
                    }
                    else if (GetType(data.Value).Equals("Float")) {
                        EditorGUILayout.FloatField(System.Convert.ToSingle(data.Value));
                    }
                    else if (GetType(data.Value).Equals("Int")) {
                        EditorGUILayout.IntField(System.Convert.ToInt32(data.Value));
                    }
                    else if (GetType(data.Value).Equals("Bool")) {
                        EditorGUILayout.Toggle(System.Convert.ToBoolean(data.Value));
                    }

                    if (GUILayout.Button("Edit")) {
                        targetKey   = data.Key;
                        targetValue = data.Value;
                        CurrentTargetTypeIndex = GetTypeIndex(GetType(data.Value));
                    }
                    if (GUILayout.Button("Del")) {
                        DeletePrefs(data.Key);
                        LoadPrefsData();
                        Repaint();
                    }
                    EditorGUILayout.EndHorizontal();
                });

            EditorGUILayout.EndScrollView();
        }

        private void DeletePrefs(string _key) {
            if (isPlayerPrefs)  { PlayerPrefs.DeleteKey(_key); PlayerPrefs.Save(); }
            else                { EditorPrefs.DeleteKey(_key); }

            Repaint();
        }

        List<KeyValuePair<string, object>> targetDataList;
        private void LoadPrefsData() {
            var playerPrefsPath = string.Empty;
            var fileName        = string.Empty;

            if (isPlayerPrefs) {
                fileName = $"unity.{PlayerSettings.companyName}.{PlayerSettings.productName}.plist";
            }
            else {
                fileName = $"com.unity3d.UnityEditor{Application.unityVersion.Split('.')[0]}.x.plist";
            }

            playerPrefsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences", fileName);

            if (File.Exists(playerPrefsPath)) {
                targetDataList = (OrcaAssist.Plist.readPlist(playerPrefsPath) as Dictionary<string, object>).ToList();
            }
            else {
                Debug.LogError($"[SYSTEM ERROR] No Exist File ({playerPrefsPath})");
            }
        }

        private string GetType(object _target) {
            var ret = string.Empty;
            if (_target.GetType() == typeof(int)) {
                ret = "Int";
            }
            else if (_target.GetType() == typeof(string)) {
                ret = "String";
            }
            else if (_target.GetType() == typeof(bool)) {
                ret = "Bool";
            }
            else if (_target.GetType() == typeof(float)) {
                ret = "Float";
            }
            else if (_target.GetType() == typeof(double)) {
                ret = "Float";
            }

            return ret;
        }

        string targetKey;
        object targetValue = "0";
        private int editToolPlayerTabIndex = 0;
        private int editToolEditorTabIndex = 0;
        private int CurrentTargetTypeIndex {
            get {
                return isPlayerPrefs ? editToolPlayerTabIndex : editToolEditorTabIndex;
            }
            set {
                if (isPlayerPrefs)  { editToolPlayerTabIndex = value; }
                else                { editToolEditorTabIndex = value; }
            }
        }
        private string[] editToolPlayerTabArray = new string[] { "String", "Float", "Int" };
        private string[] editToolEditTabArray = new string[] { "String", "Float", "Int", "Bool" };
        private string[] editToolTabArray => isPlayerPrefs ? editToolPlayerTabArray : editToolEditTabArray;
        private int GetTypeIndex(string _type) => editToolTabArray.ToList().IndexOf(_type);
        private void DrawEditTool() {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(isPlayerPrefs ? "PlayerPrefs Edit Tool" : "EditorPrefs Edit Tool", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Type", EditorStyles.boldLabel, GUILayout.Width(37));
            CurrentTargetTypeIndex = GUILayout.Toolbar(CurrentTargetTypeIndex, editToolTabArray);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Key", EditorStyles.boldLabel);
            targetKey = EditorGUILayout.TextField(targetKey);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Value", EditorStyles.boldLabel);
            if (editToolTabArray[CurrentTargetTypeIndex].Equals("String")) {
                targetValue = EditorGUILayout.TextField(targetValue.ToString());
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Float")) {
                targetValue = EditorGUILayout.FloatField(System.Convert.ToSingle(targetValue));
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Int")) {
                targetValue = EditorGUILayout.IntField(System.Convert.ToInt32(targetValue));
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Bool")) {
                targetValue = EditorGUILayout.Toggle(System.Convert.ToBoolean(targetValue));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("", EditorStyles.boldLabel);
            if (GUILayout.Button("Add")) { SavePrefs(); }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
        private void SavePrefs() { 
            if (isPlayerPrefs)  { SavePlayerPrefs(); }
            else                { SaveEditorPrefs(); }
        }

        private void SavePlayerPrefs() {
            if (editToolTabArray[CurrentTargetTypeIndex].Equals("String")) {
                PlayerPrefs.SetString(targetKey, targetValue.ToString());
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Float")) {
                PlayerPrefs.SetFloat(targetKey, System.Convert.ToSingle(targetValue));
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Int")) {
                PlayerPrefs.SetInt(targetKey, System.Convert.ToInt32(targetValue));
            }

            PlayerPrefs.Save();

            LoadPrefsData();
            Repaint();
        }

        private void SaveEditorPrefs() {
            if (editToolTabArray[CurrentTargetTypeIndex].Equals("String")) {
                EditorPrefs.SetString(targetKey, targetValue.ToString());
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Float")) {
                EditorPrefs.SetFloat(targetKey, System.Convert.ToSingle(targetValue));
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Int")) {
                EditorPrefs.SetInt(targetKey, System.Convert.ToInt32(targetValue));
            }
            else if (editToolTabArray[CurrentTargetTypeIndex].Equals("Bool")) {
                EditorPrefs.SetBool(targetKey, System.Convert.ToBoolean(targetValue));
            }

            LoadPrefsData();
            Repaint();
        }
    }

}