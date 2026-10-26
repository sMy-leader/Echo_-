using Game.Application;
using Game.Bootstrap;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// 流程骨架布线：Boot → MainMenu → Level_00/01/02 的传送门与大厅 UI。
    ///
    /// 这是临时 Editor 工具，不是运行时系统；场景内容稳定后可以保留为
    /// “重新布线”工具，接 YooAsset/存档时只替换 LevelFlow 内部实现。
    /// </summary>
    public static class FlowSceneBuilder
    {
        private const string BootPath = "Assets/Scenes/Boot.unity";
        private const string HubPath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("ECHO/Build Flow Wiring")]
        public static void BuildFlowWiring()
        {
            ConfigureBoot();
            ConfigureHub();
            ConfigurePortal("Assets/Scenes/Level_00.unity", "Level_01", new Vector3(0f, 0.1f, 9f), LevelIds.Level00);
            ConfigurePortal("Assets/Scenes/Level_01.unity", "Level_02_InteriorPrototype", new Vector3(0f, 0.1f, 10f), LevelIds.Level01);
            ConfigurePortal("Assets/Scenes/Level_02_InteriorPrototype.unity", "Level_03", new Vector3(0f, 0.1f, 10f), LevelIds.Level02);

            AssetDatabase.SaveAssets();
            Debug.Log("ECHO flow wiring built: Boot -> MainMenu -> Level_00 -> Level_01 -> Level_02");
        }

        private static void ConfigureBoot()
        {
            Scene scene = EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);
            Bootstrapper bootstrapper = Object.FindAnyObjectByType<Bootstrapper>();
            if (bootstrapper == null)
            {
                Debug.LogWarning("FlowSceneBuilder: Bootstrapper missing in Boot scene.");
                return;
            }

            SerializedObject serialized = new SerializedObject(bootstrapper);
            SerializedProperty firstScene = serialized.FindProperty("firstScene");
            if (firstScene != null)
            {
                firstScene.stringValue = "MainMenu";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureHub()
        {
            Scene scene = EditorSceneManager.OpenScene(HubPath, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("HubMenu");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject hub = new GameObject("HubMenu");
            hub.AddComponent<HubMenu>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePortal(string scenePath, string nextScene, Vector3 position, string completedLevelId)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("FlowPortal");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject portal = M0SceneBuilder.CreateCube(
                "FlowPortal",
                position,
                new Vector3(1.4f, 2.2f, 1.4f),
                new Color(0.1f, 0.9f, 1f),
                keepCollider: false,
                addColorView: false);

            LevelPortalView view = portal.AddComponent<LevelPortalView>();
            view.nextScene = nextScene;
            view.completedLevelId = completedLevelId;
            view.requirePlateActive = true;

            GameLoop loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop != null)
            {
                loop.levelPortal = view;
            }
            else
            {
                Debug.LogWarning("FlowSceneBuilder: GameLoop missing in " + scenePath);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
