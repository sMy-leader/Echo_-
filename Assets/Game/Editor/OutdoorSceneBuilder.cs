using System.Collections.Generic;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// L1 室外灰盒：复用 L1SceneBuilder 的机关/角色/激光，只替换房间为室外庭院。
    ///
    /// 为什么单独建场景：
    /// - Level_01 保留为室内第三人称原型；
    /// - 室外 L0/L1 需要更大的构图、不同相机距离和掩体布局；
    /// - 共用 Gameplay 构建方法，避免两套机关逻辑分叉。
    ///
    /// 菜单：ECHO/Build L1 Outdoor
    /// </summary>
    public static class OutdoorSceneBuilder
    {
        private const string LevelPath = "Assets/Scenes/Level_01.unity";
        private const string VfxMaterialPath = "Assets/Game/Art/VFX/M_Vfx_Additive.mat";

        private static readonly string[] OutdoorObjectNames =
        {
            "Outdoor_Floor",
            "Outdoor_Wall_South",
            "Outdoor_Wall_NorthLeft",
            "Outdoor_Wall_NorthRight",
            "Outdoor_Wall_East",
            "Outdoor_Wall_West",
            "Outdoor_Cover_West",
            "Outdoor_Cover_Crate",
            "Outdoor_Cover_Rock"
        };

        [MenuItem("ECHO/Build L1 Outdoor")]
        public static void BuildL1Outdoor()
        {
            Scene scene;
            if (System.IO.File.Exists(LevelPath))
            {
                scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, LevelPath);
            }

            ClearGenerated();
            M0SceneBuilder.ConfigureCamera();

            EnsureOutdoorLight();
            M0SceneBuilder.ConfigureLight();

            ConfigureOutdoorAmbient();
            Material greybox = L1SceneBuilder.LoadOrCreateGreyboxMaterial();
            BuildOutdoorRoom(greybox);

            // 复用现有机关/角色/激光；坐标仍沿用 L1：
            // 玩家 -7、压力板 (4,2)、激光 z=7、门 z=11、守卫 z=1。
            L1SceneBuilder.BuildMechanisms();
            Material vfx = AssetDatabase.LoadAssetAtPath<Material>(VfxMaterialPath);
            L1SceneBuilder.BuildCharacters(vfx, false);

            CameraRig rig = Object.FindAnyObjectByType<CameraRig>();
            if (rig != null)
            {
                rig.distance = 20f;
                rig.minDistance = 12f;
                rig.maxDistance = 26f;
                rig.pitch = 50f;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuildSettings();

            Debug.Log("ECHO L1 outdoor scene built: " + LevelPath);
        }

        private static void ClearGenerated()
        {
            for (int i = 0; i < L1SceneBuilder.GeneratedObjectNames.Length; i++)
            {
                DestroyIfExists(L1SceneBuilder.GeneratedObjectNames[i]);
            }

            for (int i = 0; i < OutdoorObjectNames.Length; i++)
            {
                DestroyIfExists(OutdoorObjectNames[i]);
            }
        }

        private static void DestroyIfExists(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        /// <summary>
        /// 30x23 室外庭院：南墙、北墙（留 4m 门洞）、东西围墙 + 西侧掩体。
        /// 地面顶面仍为 y=0，玩家/守卫/机关坐标与 L1 保持一致。
        /// </summary>
        private static void BuildOutdoorRoom(Material material)
        {
            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Floor",
                new Vector3(0f, -0.1f, -0.5f),
                new Vector3(30f, 0.2f, 23f),
                new Color(0.16f, 0.18f, 0.21f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Wall_South",
                new Vector3(0f, 2f, -12f),
                new Vector3(30f, 4f, 0.5f),
                new Color(0.28f, 0.3f, 0.34f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Wall_East",
                new Vector3(15f, 2f, -0.5f),
                new Vector3(0.5f, 4f, 23f),
                new Color(0.28f, 0.3f, 0.34f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Wall_West",
                new Vector3(-15f, 2f, -0.5f),
                new Vector3(0.5f, 4f, 23f),
                new Color(0.28f, 0.3f, 0.34f),
                material);

            // 北墙留 x=-2..2 门洞。
            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Wall_NorthLeft",
                new Vector3(-8.5f, 2f, 11f),
                new Vector3(13f, 4f, 0.5f),
                new Color(0.25f, 0.27f, 0.31f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Wall_NorthRight",
                new Vector3(8.5f, 2f, 11f),
                new Vector3(13f, 4f, 0.5f),
                new Color(0.25f, 0.27f, 0.31f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Cover_West",
                new Vector3(-7f, 1f, 1f),
                new Vector3(1.2f, 2f, 4f),
                new Color(0.35f, 0.33f, 0.3f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Cover_Crate",
                new Vector3(7f, 0.75f, 5f),
                new Vector3(2f, 1.5f, 2f),
                new Color(0.4f, 0.35f, 0.28f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "Outdoor_Cover_Rock",
                new Vector3(-9f, 1.2f, -4f),
                new Vector3(3f, 2.4f, 2.5f),
                new Color(0.3f, 0.31f, 0.3f),
                material);
        }

        private static void EnsureOutdoorLight()
        {
            if (Object.FindAnyObjectByType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.96f, 0.88f);
                light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }
        }

        private static void ConfigureOutdoorAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.42f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.2f, 0.24f, 0.3f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.004f;
        }

        private static void AddToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == LevelPath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(LevelPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
