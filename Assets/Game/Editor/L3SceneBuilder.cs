using System.Collections.Generic;
using Game.Application;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// L3 综合关灰盒：中心大厅 + 三条支线入口。
    ///
    /// 这一版只做空间/流程骨架，不导入 Kit、不添加新的机关类型；
    /// 复用 L2 的压力板/门/激光/守卫，先验证从 L2 进入 L3、再回 Hub 的链路。
    /// </summary>
    public static class L3SceneBuilder
    {
        private const string LevelPath = "Assets/Scenes/Level_03.unity";
        private const string VfxMaterialPath = "Assets/Game/Art/VFX/M_Vfx_Additive.mat";

        private static readonly string[] L3ObjectNames =
        {
            "L3_Floor",
            "L3_Wall_South",
            "L3_Wall_EastA",
            "L3_Wall_EastB",
            "L3_Wall_WestA",
            "L3_Wall_WestB",
            "L3_Wall_NorthLeft",
            "L3_Wall_NorthRight",
            "L3_Branch_West",
            "L3_Branch_East",
            "L3_Branch_North",
            "L3_Plate_West",
            "L3_Plate_East",
            "L3_Plate_North",
            "L3_Core",
            "L3_FlowPortal"
        };

        [MenuItem("ECHO/Build L3 Greybox")]
        public static void BuildL3Greybox()
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
            EnsureLight();
            M0SceneBuilder.ConfigureLight();
            ConfigureAmbient();

            Material greybox = L1SceneBuilder.LoadOrCreateGreyboxMaterial();
            BuildRoom(greybox);

            // 复用 L1 的机关/角色/激光/守卫；L1_ 坐标在中心大厅内。
            L1SceneBuilder.BuildMechanisms();
            Material vfx = AssetDatabase.LoadAssetAtPath<Material>(VfxMaterialPath);
            L1SceneBuilder.BuildCharacters(vfx, false);
            BuildPortal();

            CameraRig rig = Object.FindAnyObjectByType<CameraRig>();
            if (rig != null)
            {
                rig.distance = 24f;
                rig.minDistance = 14f;
                rig.maxDistance = 30f;
                rig.pitch = 50f;
            }

            // L3 正式综合关使用第三人称，重建后也保持统一相机。
            ThirdPersonCameraBuilder.EnsureAndApplyToActiveScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuildSettings();

            Debug.Log("ECHO L3 greybox built: " + LevelPath);
        }

        private static void ClearGenerated()
        {
            for (int i = 0; i < L1SceneBuilder.GeneratedObjectNames.Length; i++)
            {
                DestroyIfExists(L1SceneBuilder.GeneratedObjectNames[i]);
            }

            for (int i = 0; i < L3ObjectNames.Length; i++)
            {
                DestroyIfExists(L3ObjectNames[i]);
            }
        }

        private static void DestroyIfExists(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        /// <summary>
        /// 中心大厅 30x26，东西各留 4m 支线入口，北侧留主门洞。
        /// 支线地板先做灰盒，机关/Kit 后续再填。
        /// </summary>
        private static void BuildRoom(Material material)
        {
            L1SceneBuilder.CreateProBuilderBox(
                "L3_Floor",
                new Vector3(0f, -0.1f, 2f),
                new Vector3(30f, 0.2f, 26f),
                new Color(0.14f, 0.16f, 0.2f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "L3_Wall_South",
                new Vector3(0f, 2f, -11f),
                new Vector3(30f, 4f, 0.5f),
                new Color(0.28f, 0.3f, 0.35f),
                material);

            // 东西墙留 z=-2..2 的支线入口。
            L1SceneBuilder.CreateProBuilderBox(
                "L3_Wall_EastA",
                new Vector3(15f, 2f, -6.5f),
                new Vector3(0.5f, 4f, 9f),
                new Color(0.26f, 0.28f, 0.33f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "L3_Wall_EastB",
                new Vector3(15f, 2f, 6.5f),
                new Vector3(0.5f, 4f, 9f),
                new Color(0.26f, 0.28f, 0.33f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "L3_Wall_WestA",
                new Vector3(-15f, 2f, -6.5f),
                new Vector3(0.5f, 4f, 9f),
                new Color(0.26f, 0.28f, 0.33f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "L3_Wall_WestB",
                new Vector3(-15f, 2f, 6.5f),
                new Vector3(0.5f, 4f, 9f),
                new Color(0.26f, 0.28f, 0.33f),
                material);

            // 北墙留主门洞；门/传送门仍在 L1 的 z=11 附近。
            L1SceneBuilder.CreateProBuilderBox(
                "L3_Wall_NorthLeft",
                new Vector3(-8f, 2f, 11f),
                new Vector3(12f, 4f, 0.5f),
                new Color(0.24f, 0.26f, 0.31f),
                material);

            L1SceneBuilder.CreateProBuilderBox(
                "L3_Wall_NorthRight",
                new Vector3(8f, 2f, 11f),
                new Vector3(12f, 4f, 0.5f),
                new Color(0.24f, 0.26f, 0.31f),
                material);

            BuildBranch("L3_Branch_West", new Vector3(-20f, 0f, 0f), material);
            BuildBranch("L3_Branch_East", new Vector3(20f, 0f, 0f), material);
            BuildBranch("L3_Branch_North", new Vector3(0f, 0f, 18f), material);
            BuildTimeLockPlates(material);

            L1SceneBuilder.CreateProBuilderBox(
                "L3_Core",
                new Vector3(0f, 1.5f, 4f),
                new Vector3(1.2f, 3f, 1.2f),
                new Color(0.15f, 0.85f, 1f),
                material);
        }

        /// <summary>
        /// 三条支线各一块压力板；中央时间锁要求三块同时激活。
        /// 板本身不挂 Collider，避免玩家胶囊扫掠被薄板挡住。
        /// </summary>
        private static void BuildTimeLockPlates(Material material)
        {
            // 三块板全部放在中央门之前，避免“门没开先要踩门后板”的死循环。
            CreateTimeLockPlate("L3_Plate_West", new Vector3(-8f, 0.05f, 0f), material);
            CreateTimeLockPlate("L3_Plate_East", new Vector3(8f, 0.05f, 0f), material);
            CreateTimeLockPlate("L3_Plate_North", new Vector3(0f, 0.05f, 8f), material);
        }

        private static void CreateTimeLockPlate(string name, Vector3 position, Material material)
        {
            GameObject plate = L1SceneBuilder.CreateProBuilderBox(
                name,
                position,
                new Vector3(2f, 0.1f, 2f),
                new Color(0.95f, 0.75f, 0.2f),
                material);

            Collider collider = plate.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            PressurePlateView view = plate.AddComponent<PressurePlateView>();
            view.halfExtents = new Vector3(1f, 0.5f, 1f);
        }

        private static void BuildBranch(string name, Vector3 center, Material material)
        {
            L1SceneBuilder.CreateProBuilderBox(
                name,
                center + new Vector3(0f, -0.1f, 0f),
                new Vector3(8f, 0.2f, 12f),
                new Color(0.18f, 0.2f, 0.24f),
                material);
        }

        private static void BuildPortal()
        {
            GameObject portal = M0SceneBuilder.CreateCube(
                "L3_FlowPortal",
                new Vector3(0f, 0.1f, 12.5f),
                new Vector3(1.4f, 2.2f, 1.4f),
                new Color(0.1f, 0.9f, 1f),
                keepCollider: false,
                addColorView: false);

            LevelPortalView view = portal.AddComponent<LevelPortalView>();
            view.nextScene = "MainMenu";
            view.completedLevelId = LevelIds.Level03;
            view.requirePlateActive = false;

            GameLoop loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop != null)
            {
                loop.levelPortal = view;
            }

            ConfigureTimeLock(loop);
        }

        private static void ConfigureTimeLock(GameLoop loop)
        {
            if (loop == null)
            {
                return;
            }

            GameObject doorObject = GameObject.Find("L1_Door");
            DoorView timeLockDoor = doorObject != null ? doorObject.GetComponent<DoorView>() : null;
            PressurePlateView[] plates = new PressurePlateView[3];
            string[] names = { "L3_Plate_West", "L3_Plate_East", "L3_Plate_North" };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject plateObject = GameObject.Find(names[i]);
                plates[i] = plateObject != null ? plateObject.GetComponent<PressurePlateView>() : null;
            }

            // L3 不再使用 L1 的单板/单门逻辑，改由三源 TimeLock 驱动同一扇门。
            // 同时清掉 L1 遗留压力板，避免场景里出现第四块“踩了没反应”的板。
            GameObject legacyPlate = GameObject.Find("L1_Plate");
            if (legacyPlate != null)
            {
                Object.DestroyImmediate(legacyPlate);
            }

            loop.pressurePlate = null;
            loop.door = null;
            loop.timeLockPlates = plates;
            loop.timeLockDoor = timeLockDoor;
        }

        private static void EnsureLight()
        {
            if (Object.FindAnyObjectByType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            }
        }

        private static void ConfigureAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.23f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.08f, 0.1f, 0.14f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;
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
