using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Threading;

namespace Augmentix.Scripts.Editor
{
    public class BuildManager : EditorWindow
    {
        [SerializeField]
        private List<SceneAsset> _scenes;
        [SerializeField]
        private List<string> _scenePaths;
        [SerializeField]
        private string _buildPath = "";
        [SerializeField]
        private bool _run = false;
        [SerializeField]
        private bool _open = false;
        [SerializeField]
        private bool _scriptDebugging = false;

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _buildPath = EditorGUILayout.TextField("Build Path", _buildPath);
            if (GUILayout.Button("..."))
            {
                _buildPath = EditorUtility.SaveFolderPanel("Choose Path", "", "");
            }
            EditorGUILayout.EndHorizontal();

            ScriptableObject scriptableObj = this;
            SerializedObject serialObj = new SerializedObject(scriptableObj);

            SerializedProperty serialProp = serialObj.FindProperty("_scenes");
            EditorGUILayout.PropertyField(serialProp, true);
            serialObj.ApplyModifiedProperties();

            _scriptDebugging = EditorGUILayout.Toggle("Script Debugging", _scriptDebugging);

            _open = EditorGUILayout.Toggle("Open Folder after Build", _open);

            _run = EditorGUILayout.Toggle("Do Run after Build", _run);

        }

        protected void OnEnable()
        {
            // Here we retrieve the data if it exists or we save the default field initialisers we set above
            var data = EditorPrefs.GetString(GetType().Name, JsonUtility.ToJson(this, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, this);
            _scenes = _scenePaths.Select(s => (SceneAsset) AssetDatabase.LoadAssetAtPath(s,typeof(SceneAsset))).ToList();
        }

        protected void OnDisable()
        {
            // We get the Json data
            _scenePaths = _scenes.Select(asset => AssetDatabase.GetAssetPath(asset)).ToList();

            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString(GetType().Name, data);

        }

    
        public static void DoBuild(Type type, BuildTarget target)
        {

            BuildManager manager = GetManager();
            BuildManager newManager = GetManager(type.Name);

            string[] levels = newManager._scenePaths.ToArray();

            BuildOptions options = BuildOptions.None;

            if (newManager._scriptDebugging)
                options |= BuildOptions.Development | BuildOptions.AllowDebugging;

            if (newManager._run)
                options |= BuildOptions.AutoRunPlayer;

            if (newManager._open)
                options |= BuildOptions.ShowBuiltPlayer;

            var filename = type == typeof(VRBuildManager) ? Application.productName + ".exe" : Application.productName + ".apk";

            BuildTarget currentTarget = BuildTarget.NoTarget;
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Debug.LogError("Please Switch to Target before build!");
                return;
                currentTarget = EditorUserBuildSettings.activeBuildTarget;
                DoSwitch(type,target);
            }
            
            BuildPipeline.BuildPlayer(levels, Path.Combine(newManager._buildPath,filename), target, options);
            
            
            if (currentTarget != BuildTarget.NoTarget)
                DoSwitch(type == typeof(VRBuildManager) ? typeof(ARBuildManager) : typeof(VRBuildManager),currentTarget);
            
        }

        [MenuItem("BuildManager/Build All")]
        public static void Build()
        {
            Debug.LogError("Currently not supported");
            return;
            
            DoBuild(typeof(ARBuildManager), BuildTarget.Android);
            DoBuild(typeof(VRBuildManager), BuildTarget.StandaloneWindows64);
        }

        public static void DoSwitch(Type type, BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
                return;

            BuildManager manager = GetManager();
            BuildManager newManager = GetManager(type.Name);

            EditorUserBuildSettings.SwitchActiveBuildTarget(
                target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone, target);

            if (type == typeof(VRBuildManager))
            {
                PlayerSettings.vuforiaEnabled = false;
                PlayerSettings.virtualRealitySupported = true; // Does not work, yet
            }
            else
            {
                PlayerSettings.vuforiaEnabled = true;
                PlayerSettings.virtualRealitySupported = false; // Does not work, yet
            }


            EditorBuildSettings.scenes =
                newManager._scenePaths.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();

            if (newManager._scenePaths.Count > 0)
                EditorSceneManager.OpenScene(newManager._scenePaths[0]);
            else
                Debug.Log("No Scenes defined. See BuildManager->Setup");

        }


        public static BuildManager GetManager()
        { 
            string type = "";

            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                {
                    type = "ARBuildManager";
                    break;
                }

                case BuildTarget.StandaloneWindows64:
                {
                    type = "VRBuildManager";
                    break;
                }
            }
            return GetManager(type);
        }

        public static BuildManager GetManager(string type)
        {
            BuildManager manager = new BuildManager();
            // Here we retrieve the data if it exists or we save the default field initialisers we set above
            var data = EditorPrefs.GetString(type, JsonUtility.ToJson(manager, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, manager);
            return manager;
        }

        public class VRBuildManager : BuildManager
        {

            [MenuItem("BuildManager/Build/VR")]
            public new static void Build()
            {
                DoBuild(typeof(VRBuildManager),BuildTarget.StandaloneWindows64);
            }

            [MenuItem("BuildManager/Switch/VR")]
            public static void Switch()
            {
                DoSwitch(typeof(VRBuildManager),BuildTarget.StandaloneWindows64);
            }

            [MenuItem("BuildManager/Setup/VR")]
            public static void Setup()
            {
                GetWindow(typeof(VRBuildManager)).Show();
            }

        }

        public class ARBuildManager : BuildManager
        {
            [MenuItem("BuildManager/Build/AR")]
            public new static void Build()
            {
                DoBuild(typeof(ARBuildManager),BuildTarget.Android);
            }

            [MenuItem("BuildManager/Switch/AR")]
            public static void Switch()
            {
                DoSwitch(typeof(ARBuildManager), BuildTarget.Android);
            }

            [MenuItem("BuildManager/Setup/AR")]
            public static void Setup()
            {
                GetWindow(typeof(ARBuildManager)).Show();
            }

        
        }

    }
}
#endif