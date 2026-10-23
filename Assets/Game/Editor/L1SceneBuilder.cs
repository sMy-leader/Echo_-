using System.Collections.Generic;
using Game.Bootstrap;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// L1 灰盒构建器：ProBuilder 负责房间结构，Primitive 负责机关/门/角色。
    /// 菜单：ECHO/Build L1 Scene
    ///
    /// 设计目标（方案 1）：
    /// - 守卫在中央巡逻，压力板在巡逻视野内；
    /// - 玩家可以先让残影踩板，再从西侧掩体绕到门口；
    /// - 门在压力板激活时升起，给后续激光门留出空间。
    /// </summary>
    public static class L1SceneBuilder
    {
        private const string LevelPath = "Assets/Scenes/Level_01.unity";
        private const string PlayerVisualPrefabPath = "Assets/Game/Art/Prefabs/Player_robot.prefab";
        private const string GuardVisualPrefabPath = "Assets/Game/Art/Prefabs/Enemy_robot1.prefab";
        private const string EchoVisualPrefabPath = "Assets/Game/Art/Prefabs/Echo_robot.prefab";
        private const string VfxMaterialPath = "Assets/Game/Art/VFX/M_Vfx_Additive.mat";
        private const string GreyboxMaterialPath = "Assets/Game/Art/Materials/M_L1_Greybox.mat";

        private static readonly string[] GeneratedObjectNames =
        {
            "L1_Floor",
            "L1_Wall_South",
            "L1_Wall_East",
            "L1_Wall_West",
            "L1_Wall_NorthLeft",
            "L1_Wall_NorthRight",
            "L1_Cover_West",
            "L1_Cover_Crate",
            "L1_Laser",
            "L1_Door",
            "L1_FillLight",
            "L1_PressurePlate",
            "L1_Checkpoint",
            "L1_Player",
            "L1_PlayerSpawn",
            "L1_Guard",
            "L1_Patrol_A",
            "L1_Patrol_B",
            "L1_Patrol_C",
            "L1_Patrol_D",
            "L1_Game"
        };

        [MenuItem("ECHO/Build L1 Scene")]
        public static void BuildL1Scene()
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

            for (int i = 0; i < GeneratedObjectNames.Length; i++)
            {
                GameObject existing = GameObject.Find(GeneratedObjectNames[i]);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }

            M0SceneBuilder.ConfigureCamera();
            EnsureDirectionalLight();
            M0SceneBuilder.ConfigureLight();
            EnsureFillLight();
            ConfigureAmbient();

            Material greybox = LoadOrCreateGreyboxMaterial();
            Material vfxMaterial = AssetDatabase.LoadAssetAtPath<Material>(VfxMaterialPath);

            BuildRoom(greybox);
            BuildMechanisms();
            BuildCharacters(vfxMaterial);
            BuildGameRoot(vfxMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuildSettings();

            Debug.Log("ECHO L1 scene built: " + LevelPath);
        }

        /// <summary>
        /// 房间尺寸 22x20，地面顶面 y=0；门洞在 z=11 的 x=-2..2。
        /// 用 ProBuilder 生成墙体/地面，后续可逐块替换成模块化 Kit。
        /// </summary>
        private static void BuildRoom(Material material)
        {
            CreateProBuilderBox(
                "L1_Floor",
                new Vector3(0f, -0.1f, 1f),
                new Vector3(22f, 0.2f, 20f),
                new Color(0.16f, 0.18f, 0.21f),
                material);

            CreateProBuilderBox(
                "L1_Wall_South",
                new Vector3(0f, 1.5f, -9f),
                new Vector3(22f, 3f, 0.4f),
                new Color(0.28f, 0.3f, 0.34f),
                material);

            CreateProBuilderBox(
                "L1_Wall_East",
                new Vector3(11f, 1.5f, 1f),
                new Vector3(0.4f, 3f, 20f),
                new Color(0.28f, 0.3f, 0.34f),
                material);

            CreateProBuilderBox(
                "L1_Wall_West",
                new Vector3(-11f, 1.5f, 1f),
                new Vector3(0.4f, 3f, 20f),
                new Color(0.28f, 0.3f, 0.34f),
                material);

            // 北墙留 x=-2..2 的门洞。
            CreateProBuilderBox(
                "L1_Wall_NorthLeft",
                new Vector3(-6.5f, 1.5f, 11f),
                new Vector3(9f, 3f, 0.4f),
                new Color(0.25f, 0.27f, 0.31f),
                material);

            CreateProBuilderBox(
                "L1_Wall_NorthRight",
                new Vector3(6.5f, 1.5f, 11f),
                new Vector3(9f, 3f, 0.4f),
                new Color(0.25f, 0.27f, 0.31f),
                material);

            // 西侧掩体：给“本体绕后”提供视线阻断；不是装饰。
            CreateProBuilderBox(
                "L1_Cover_West",
                new Vector3(-6f, 1f, 1f),
                new Vector3(1.2f, 2f, 4f),
                new Color(0.35f, 0.33f, 0.3f),
                material);

            CreateProBuilderBox(
                "L1_Cover_Crate",
                new Vector3(5f, 0.75f, 5f),
                new Vector3(2f, 1.5f, 2f),
                new Color(0.4f, 0.35f, 0.28f),
                material);
        }

        private static void BuildMechanisms()
        {
            GameObject door = M0SceneBuilder.CreateCube(
                "L1_Door",
                new Vector3(0f, 1.5f, 11f),
                new Vector3(4f, 3f, 0.3f),
                new Color(0.9f, 0.3f, 0.2f),
                keepCollider: true,
                addColorView: false);
            DoorView doorView = door.AddComponent<DoorView>();
            doorView.openOffset = new Vector3(0f, 3f, 0f);
            doorView.speed = 4f;

            GameObject plate = M0SceneBuilder.CreateCube(
                "L1_PressurePlate",
                new Vector3(4f, 0.02f, 2f),
                new Vector3(2f, 0.04f, 2f),
                new Color(0.9f, 0.75f, 0.2f),
                keepCollider: false,
                addColorView: false);
            PressurePlateView plateView = plate.AddComponent<PressurePlateView>();

            M0SceneBuilder.CreateCube(
                "L1_Checkpoint",
                new Vector3(0f, 0.02f, -7f),
                new Vector3(2f, 0.04f, 2f),
                new Color(0.2f, 0.55f, 1f),
                keepCollider: false);

            BuildLaserGate();
        }

        /// <summary>
        /// 周期激光横跨主房间 z=7，玩家必须等关闭窗口。
        /// 发射器只做视觉，不挂 Collider；命中由 LaserGateView 的线段距离判断。
        /// </summary>
        private static void BuildLaserGate()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(VfxMaterialPath);

            GameObject laser = new GameObject("L1_Laser");
            LineRenderer line = laser.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(-10.7f, 1.2f, 7f));
            line.SetPosition(1, new Vector3(10.7f, 1.2f, 7f));
            line.startWidth = 0.12f;
            line.endWidth = 0.12f;
            line.numCapVertices = 4;
            line.sharedMaterial = material;
            line.startColor = new Color(0.2f, 0.04f, 0.04f);
            line.endColor = new Color(0.2f, 0.04f, 0.04f);

            LaserGateView view = laser.AddComponent<LaserGateView>();
            view.beam = line;
            view.hitRadius = 0.25f;

            M0SceneBuilder.CreateCube(
                "L1_LaserEmitterLeft",
                new Vector3(-10.7f, 0.6f, 7f),
                new Vector3(0.25f, 1.2f, 0.25f),
                new Color(0.5f, 0.08f, 0.05f),
                keepCollider: false);

            M0SceneBuilder.CreateCube(
                "L1_LaserEmitterRight",
                new Vector3(10.7f, 0.6f, 7f),
                new Vector3(0.25f, 1.2f, 0.25f),
                new Color(0.5f, 0.08f, 0.05f),
                keepCollider: false);
        }

        private static void BuildCharacters(Material vfxMaterial)
        {
            GameObject player = new GameObject("L1_Player");
            player.transform.position = new Vector3(0f, 0.05f, -7f);
            PlayerView playerView = player.AddComponent<PlayerView>();

            GameObject playerVisual = M0SceneBuilder.InstantiateCharacterVisual(
                PlayerVisualPrefabPath,
                player.transform,
                0.85f);
            if (playerVisual == null)
            {
                playerVisual = M0SceneBuilder.CreateCapsuleVisual(
                    player.transform,
                    0.9f,
                    new Vector3(0.6f, 0.9f, 0.6f));
            }

            playerView.applyColorToVisual = false;
            playerView.visualRoot = playerVisual.transform;
            playerView.animator = playerVisual.GetComponentInChildren<Animator>();
            if (playerView.animator != null)
            {
                playerView.animator.applyRootMotion = false;
            }

            CharacterVfx playerVfx = player.AddComponent<CharacterVfx>();
            playerVfx.visualRoot = playerVisual.transform;
            playerVfx.particleMaterial = vfxMaterial;
            playerVfx.primaryColor = new Color(1f, 0.55f, 0.15f);
            playerVfx.coreHeight = 1f;
            playerView.vfx = playerVfx;

            GameObject spawn = new GameObject("L1_PlayerSpawn");
            spawn.transform.position = new Vector3(0f, 0.05f, -7f);

            GameObject guard = new GameObject("L1_Guard");
            guard.transform.position = new Vector3(0f, 0.05f, 1f);
            guard.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            GuardView guardView = guard.AddComponent<GuardView>();
            guardView.minApproachDistance = 1.1f;
            guardView.attackRange = 1.4f;
            guardView.attackHitDistanceBuffer = 0.3f;

            GameObject guardVisual = M0SceneBuilder.InstantiateCharacterVisual(
                GuardVisualPrefabPath,
                guard.transform,
                0.7f);
            if (guardVisual == null)
            {
                guardVisual = M0SceneBuilder.CreateCapsuleVisual(
                    guard.transform,
                    0.9f,
                    new Vector3(0.6f, 0.9f, 0.6f));
            }

            guardView.applyStateColorToVisual = false;
            guardView.visualRoot = guardVisual.transform;
            guardView.animator = guardVisual.GetComponentInChildren<Animator>();
            if (guardView.animator != null)
            {
                guardView.animator.applyRootMotion = false;
            }

            CharacterVfx guardVfx = guard.AddComponent<CharacterVfx>();
            guardVfx.visualRoot = guardVisual.transform;
            guardVfx.particleMaterial = vfxMaterial;
            guardVfx.primaryColor = new Color(1f, 0.15f, 0.1f);
            guardVfx.coreHeight = 1.2f;
            guardView.vfx = guardVfx;

            GameObject forward = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forward.name = "ForwardIndicator";
            forward.transform.SetParent(guard.transform, false);
            forward.transform.localPosition = new Vector3(0f, 1.9f, 1f);
            forward.transform.localScale = new Vector3(0.22f, 0.22f, 0.5f);
            Object.DestroyImmediate(forward.GetComponent<Collider>());
            guardView.forwardIndicator = forward.transform;

            GameObject patrolA = new GameObject("L1_Patrol_A");
            patrolA.transform.position = new Vector3(-6f, 0f, 4f);
            GameObject patrolB = new GameObject("L1_Patrol_B");
            patrolB.transform.position = new Vector3(6f, 0f, 4f);
            GameObject patrolC = new GameObject("L1_Patrol_C");
            patrolC.transform.position = new Vector3(6f, 0f, -2f);
            GameObject patrolD = new GameObject("L1_Patrol_D");
            patrolD.transform.position = new Vector3(-6f, 0f, -2f);

            guardView.patrolPoints = new[]
            {
                patrolA.transform,
                patrolB.transform,
                patrolC.transform,
                patrolD.transform
            };

            GameObject game = new GameObject("L1_Game");
            GameLoop loop = game.AddComponent<GameLoop>();
            GameHud hud = game.AddComponent<GameHud>();
            loop.playerSpawn = spawn.transform;
            loop.playerView = playerView;
            loop.cameraRig = Object.FindAnyObjectByType<CameraRig>();
            loop.pressurePlate = Object.FindAnyObjectByType<PressurePlateView>();
            loop.door = Object.FindAnyObjectByType<DoorView>();
            loop.guardView = guardView;
            loop.echoVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EchoVisualPrefabPath);
            loop.echoVfxMaterial = vfxMaterial;
            loop.laserGate = Object.FindAnyObjectByType<LaserGateView>();
            loop.hud = hud;
        }

        private static void BuildGameRoot(Material vfxMaterial)
        {
            // BuildCharacters 里已经创建 GameLoop；这里保留空方法是为了给后续
            // 多守卫/激光门/关卡配置留一个明确的挂载点。
        }

        /// <summary>
        /// ProBuilder 盒子：用 ShapeGenerator 生成 mesh，再补 MeshCollider。
        /// 视觉上是一整块 ProBuilder 几何；后续可以逐面替换成 Kit 模块。
        /// </summary>
        private static GameObject CreateProBuilderBox(
            string name,
            Vector3 position,
            Vector3 size,
            Color color,
            Material material)
        {
            ProBuilderMesh pb = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            pb.ToMesh();
            pb.Refresh();

            GameObject go = pb.gameObject;
            go.name = name;
            go.transform.position = position;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // 编辑模式下必须用 sharedMaterial；renderer.material 会实例化并泄漏材质。
                renderer.sharedMaterial = material;
            }

            // 运行时再按用途染色；避免在编辑模式下为每块 ProBuilder 几何实例化材质。
            SceneColorView colorView = go.AddComponent<SceneColorView>();
            colorView.color = color;

            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = go.AddComponent<MeshCollider>();
            }

            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter != null)
            {
                collider.sharedMesh = filter.sharedMesh;
            }

            return go;
        }

        private static Material LoadOrCreateGreyboxMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GreyboxMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader)
            {
                name = "M_L1_Greybox"
            };
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, GreyboxMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureDirectionalLight()
        {
            if (Object.FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        /// <summary>
        /// 室内补光：只用于灰盒可读性，不追求最终光照。
        /// 点光不投影，避免 20×20 房间里实时阴影开销。
        /// </summary>
        private static void EnsureFillLight()
        {
            GameObject existing = GameObject.Find("L1_FillLight");
            if (existing != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("L1_FillLight");
            lightObject.transform.position = new Vector3(0f, 5f, 1f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 26f;
            light.intensity = 2.2f;
            light.color = new Color(0.55f, 0.7f, 1f);
            light.shadows = LightShadows.None;
        }

        private static void ConfigureAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.08f, 0.1f, 0.14f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
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
