using Game.Bootstrap;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// 可重复构建 M0 灰盒场景，避免手工拖拽引用。
    /// 菜单：ECHO/Build M0 Scenes
    /// </summary>
    public static class M0SceneBuilder
    {
        private const string LevelPath = "Assets/Scenes/Level_00.unity";
        private const string BootPath = "Assets/Scenes/Boot.unity";
        private const string PlayerVisualPrefabPath = "Assets/Game/Art/Prefabs/Player_robot.prefab";
        private const string GuardVisualPrefabPath = "Assets/Game/Art/Prefabs/Enemy_robot1.prefab";
        private const string EchoVisualPrefabPath = "Assets/Game/Art/Prefabs/Echo_robot.prefab";
        private const string VfxMaterialPath = "Assets/Game/Art/VFX/M_Vfx_Additive.mat";

        // 由本工具“拥有”的对象名。重建时先删再建，保证场景可重复生成。
        private static readonly string[] GeneratedObjectNames =
        {
            "Floor",
            "Wall_North",
            "Wall_South",
            "Wall_East",
            "Wall_West",
            "Wall_DoorLeft",
            "Wall_DoorRight",
            "Door",
            "PressurePlate",
            "Checkpoint",
            "Player",
            "PlayerSpawn",
            "Guard",
            "GuardPatrol_A",
            "GuardPatrol_B",
            "GuardPatrol_C",
            "GuardPatrol_D",
            "Game"
        };

        /// <summary>
        /// 一键重建 M0 场景与构建配置。幂等：重复执行不会留下重复对象。
        /// 注意：会覆盖 Level_00 / Boot 场景中由本工具生成的对象，手工改动需先移出这些名字。
        /// </summary>
        [MenuItem("ECHO/Build M0 Scenes")]
        public static void BuildM0Scenes()
        {
            BuildLevel();
            BuildBoot();
            ConfigureBuildSettings();

            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.productName = "ECHO-残响";

            AssetDatabase.SaveAssets();
            Debug.Log("ECHO M0 scenes built. Open Assets/Scenes/Boot.unity and press Play.");
        }

        /// <summary>
        /// 灰盒布局（单位：米）：
        /// - 房间 20x20，地面顶面 y=0；
        /// - 玩家出生/检查点 z=-6；
        /// - 压力板 z=4；
        /// - 门框与门 z=8，门洞 x=-2..2；
        /// - 相机从 -Z 方向斜俯视，保证出生点、压力板、门同屏。
        /// </summary>
        private static void BuildLevel()
        {
            Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);

            for (int i = 0; i < GeneratedObjectNames.Length; i++)
            {
                GameObject existing = GameObject.Find(GeneratedObjectNames[i]);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }

            ConfigureCamera();
            ConfigureLight();

            Material vfxMaterial = AssetDatabase.LoadAssetAtPath<Material>(VfxMaterialPath);

            CreateCube(
                "Floor",
                new Vector3(0f, -0.1f, 0f),
                new Vector3(20f, 0.2f, 20f),
                new Color(0.18f, 0.19f, 0.21f));

            CreateCube(
                "Wall_North",
                new Vector3(0f, 1.5f, 10f),
                new Vector3(20f, 3f, 0.5f),
                new Color(0.32f, 0.33f, 0.35f));

            CreateCube(
                "Wall_South",
                new Vector3(0f, 1.5f, -10f),
                new Vector3(20f, 3f, 0.5f),
                new Color(0.32f, 0.33f, 0.35f));

            CreateCube(
                "Wall_East",
                new Vector3(10f, 1.5f, 0f),
                new Vector3(0.5f, 3f, 20f),
                new Color(0.32f, 0.33f, 0.35f));

            CreateCube(
                "Wall_West",
                new Vector3(-10f, 1.5f, 0f),
                new Vector3(0.5f, 3f, 20f),
                new Color(0.32f, 0.33f, 0.35f));

            // 门框墙：只在 x=-2..2 留出门洞，防止玩家绕过门。
            CreateCube(
                "Wall_DoorLeft",
                new Vector3(-6f, 1.5f, 8f),
                new Vector3(8f, 3f, 0.5f),
                new Color(0.28f, 0.29f, 0.31f));

            CreateCube(
                "Wall_DoorRight",
                new Vector3(6f, 1.5f, 8f),
                new Vector3(8f, 3f, 0.5f),
                new Color(0.28f, 0.29f, 0.31f));

            GameObject door = CreateCube(
                "Door",
                new Vector3(0f, 1.5f, 8f),
                new Vector3(4f, 3f, 0.3f),
                new Color(0.9f, 0.3f, 0.2f),
                keepCollider: true,
                addColorView: false);
            DoorView doorView = door.AddComponent<DoorView>();
            doorView.openOffset = new Vector3(0f, 3f, 0f);
            doorView.speed = 4f;

            GameObject plate = CreateCube(
                "PressurePlate",
                new Vector3(0f, 0.02f, 4f),
                new Vector3(2f, 0.04f, 2f),
                new Color(0.9f, 0.75f, 0.2f),
                keepCollider: false,
                addColorView: false);
            PressurePlateView plateView = plate.AddComponent<PressurePlateView>();

            CreateCube(
                "Checkpoint",
                new Vector3(0f, 0.02f, -6f),
                new Vector3(2f, 0.04f, 2f),
                new Color(0.2f, 0.55f, 1f),
                keepCollider: false);

            // 玩家根节点在脚底；胶囊/模型放在 visualRoot 子节点。
            // 这样未来换模型时只换 visualRoot，逻辑胶囊和 PlayerMotor 不动。
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, -6f);
            PlayerView playerView = player.AddComponent<PlayerView>();

            GameObject playerVisual = InstantiateCharacterVisual(
                PlayerVisualPrefabPath,
                player.transform,
                0.85f);

            if (playerVisual != null)
            {
                // 正式模型已经带材质，不再用 PlayerView 的 fallback 橙色覆盖。
                playerView.applyColorToVisual = false;
            }
            else
            {
                playerVisual = CreateCapsuleVisual(
                    player.transform,
                    0.9f,
                    new Vector3(0.6f, 0.9f, 0.6f));
            }

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
            playerVfx.coreHeight = 1.0f;
            playerView.vfx = playerVfx;

            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = new Vector3(0f, 0.05f, -6f);

            // 守卫根节点在脚底；胶囊/模型放在 visualRoot，前向指示器放在 forwardIndicator。
            // 胶囊是对称的，forwardIndicator 让顶部视角能看出守卫面朝哪边。
            GameObject guard = new GameObject("Guard");
            guard.transform.position = new Vector3(0f, 0.05f, 0f);
            guard.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            GuardView guardView = guard.AddComponent<GuardView>();

            GameObject guardVisual = InstantiateCharacterVisual(
                GuardVisualPrefabPath,
                guard.transform,
                0.7f);

            if (guardVisual != null)
            {
                // 模型已经有自己的材质/发光条，状态颜色主要靠 ForwardIndicator 表达。
                guardView.applyStateColorToVisual = false;
            }
            else
            {
                guardVisual = CreateCapsuleVisual(
                    guard.transform,
                    0.9f,
                    new Vector3(0.6f, 0.9f, 0.6f));
            }

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

            GameObject guardForward = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guardForward.name = "ForwardIndicator";
            guardForward.transform.SetParent(guard.transform, false);
            guardForward.transform.localPosition = new Vector3(0f, 1.9f, 1.0f);
            guardForward.transform.localScale = new Vector3(0.22f, 0.22f, 0.5f);
            Object.DestroyImmediate(guardForward.GetComponent<Collider>());
            guardView.forwardIndicator = guardForward.transform;

            GameObject patrolA = new GameObject("GuardPatrol_A");
            patrolA.transform.position = new Vector3(0f, 0f, 0f);
            GameObject patrolB = new GameObject("GuardPatrol_B");
            patrolB.transform.position = new Vector3(5f, 0f, 0f);
            GameObject patrolC = new GameObject("GuardPatrol_C");
            patrolC.transform.position = new Vector3(5f, 0f, 3f);
            GameObject patrolD = new GameObject("GuardPatrol_D");
            patrolD.transform.position = new Vector3(-5f, 0f, 3f);

            guardView.patrolPoints = new[]
            {
                patrolA.transform,
                patrolB.transform,
                patrolC.transform,
                patrolD.transform
            };

            GameObject game = new GameObject("Game");
            GameLoop loop = game.AddComponent<GameLoop>();
            GameHud hud = game.AddComponent<GameHud>();
            loop.playerSpawn = spawn.transform;
            loop.playerView = playerView;
            loop.cameraRig = Object.FindAnyObjectByType<CameraRig>();
            loop.pressurePlate = plateView;
            loop.door = doorView;
            loop.guardView = guardView;
            loop.echoVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EchoVisualPrefabPath);
            loop.echoVfxMaterial = vfxMaterial;
            loop.hud = hud;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Boot 场景只放一个启动器；真正玩法场景由 Bootstrapper 在运行时加载。
        /// </summary>
        private static void BuildBoot()
        {
            Scene scene = EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("Bootstrap");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<Bootstrapper>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 固定透视相机：俯角 50°、FOV 45°、相对玩家偏移 (0,16,-12)。
        /// 这组值让出生点、压力板、门同时可见；后续可按构图微调。
        /// </summary>
        internal static void ConfigureCamera()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 16f, -18f);
            camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            camera.fieldOfView = 45f;
            camera.farClipPlane = 100f;

            CameraRig rig = camera.GetComponent<CameraRig>();
            if (rig == null)
            {
                rig = camera.gameObject.AddComponent<CameraRig>();
            }

            rig.distance = 16f;
            rig.pitch = 50f;
            rig.yaw = 0f;
            rig.followSpeed = 3.5f;
        }

        /// <summary>
        /// 主光只做方向光，灰盒不依赖烘焙光照。
        /// </summary>
        internal static void ConfigureLight()
        {
            Light light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        /// <summary>
        /// 创建一个灰盒 Cube。
        /// keepCollider=false 用于压力板/检查点这类“视觉地板”，避免挡住玩家胶囊。
        /// addColorView=false 用于门/压力板：它们由各自的 View 组件控制颜色，避免被覆盖。
        /// </summary>
        internal static GameObject CreateCube(
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool keepCollider = true,
            bool addColorView = true)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;

            if (!keepCollider)
            {
                Collider collider = cube.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }
            }

            if (addColorView)
            {
                SceneColorView colorView = cube.AddComponent<SceneColorView>();
                colorView.color = color;
            }

            return cube;
        }

        /// <summary>
        /// 实例化正式角色 Prefab，作为角色的 visualRoot 子节点。
        /// Prefab 的 pivot 约定在脚底；找不到 Prefab 时返回 null，由调用方回退胶囊。
        /// </summary>
        internal static GameObject InstantiateCharacterVisual(
            string prefabPath,
            Transform parent,
            float uniformScale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * uniformScale;

            RemoveColliders(instance);
            return instance;
        }

        /// <summary>
        /// 灰盒 fallback：没有正式模型时使用的胶囊视觉体。
        /// </summary>
        internal static GameObject CreateCapsuleVisual(
            Transform parent,
            float localY,
            Vector3 scale)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(0f, localY, 0f);
            visual.transform.localScale = scale;
            RemoveColliders(visual);
            return visual;
        }

        /// <summary>
        /// 视觉体不参与物理；模型自带 Collider 时也一并移除。
        /// </summary>
        internal static void RemoveColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }
        }

        /// <summary>
        /// 固定构建顺序：Boot 必须是 0，否则打包后不会先进入启动器。
        /// </summary>
        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene(LevelPath, true)
            };
        }
    }
}
