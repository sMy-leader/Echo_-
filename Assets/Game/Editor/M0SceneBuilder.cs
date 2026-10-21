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

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.95f, -6f);
            player.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            Object.DestroyImmediate(player.GetComponent<Collider>());
            PlayerView playerView = player.AddComponent<PlayerView>();

            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = new Vector3(0f, 0.05f, -6f);

            GameObject game = new GameObject("Game");
            GameLoop loop = game.AddComponent<GameLoop>();
            GameHud hud = game.AddComponent<GameHud>();
            loop.playerSpawn = spawn.transform;
            loop.playerView = playerView;
            loop.cameraRig = Object.FindAnyObjectByType<CameraRig>();
            loop.pressurePlate = plateView;
            loop.door = doorView;
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
        private static void ConfigureCamera()
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

            rig.offset = new Vector3(0f, 16f, -12f);
            rig.pitch = 50f;
            rig.yaw = 0f;
            rig.followSpeed = 3.5f;
        }

        /// <summary>
        /// 主光只做方向光，灰盒不依赖烘焙光照。
        /// </summary>
        private static void ConfigureLight()
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
        private static GameObject CreateCube(
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
