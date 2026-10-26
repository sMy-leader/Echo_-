using Game.Presentation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// 可复用的第三人称相机 Prefab + 场景应用工具。
    ///
    /// 参数基线来自 L2（用户已调好的手感）：
    /// Radius 6、FOV 40、Yaw 0.06 / Pitch 0.04、自由鼠标、
    /// Deoccluder 0.5m。L2/L3 都实例化同一个 Prefab，避免场景各调一套。
    /// </summary>
    public static class ThirdPersonCameraBuilder
    {
        private const string PrefabPath = "Assets/Game/Presentation/Camera/ThirdPersonCameraRig.prefab";
        private const string Level02Path = "Assets/Scenes/Level_02_InteriorPrototype.unity";
        private const string Level03Path = "Assets/Scenes/Level_03.unity";

        [MenuItem("ECHO/Build ThirdPerson Camera Prefab")]
        public static void BuildPrefab()
        {
            GameObject root = new GameObject("ThirdPersonCameraRig");

            GameObject targetObject = new GameObject("CameraTarget");
            targetObject.transform.SetParent(root.transform, false);
            CameraTargetFollow targetFollow = targetObject.AddComponent<CameraTargetFollow>();
            targetFollow.offset = new Vector3(0f, 1.2f, 0f);

            GameObject vcamObject = new GameObject("VCam_ThirdPerson");
            vcamObject.transform.SetParent(root.transform, false);

            CinemachineCamera vcam = vcamObject.AddComponent<CinemachineCamera>();
            vcam.Follow = targetObject.transform;
            vcam.LookAt = targetObject.transform;
            LensSettings lens = vcam.Lens;
            lens.FieldOfView = 40f;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 100f;
            vcam.Lens = lens;

            CinemachineOrbitalFollow orbit = vcamObject.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = 6f;

            InputAxis horizontal = orbit.HorizontalAxis;
            horizontal.Value = 0f;
            horizontal.Range = new Vector2(-180f, 180f);
            horizontal.Center = 0f;
            horizontal.Wrap = true;
            horizontal.Recentering.Enabled = false;

            InputAxis vertical = orbit.VerticalAxis;
            vertical.Value = 17.5f;
            vertical.Range = new Vector2(-25f, 60f);
            vertical.Center = 15f;
            vertical.Wrap = false;
            vertical.Recentering.Enabled = false;

            orbit.HorizontalAxis = horizontal;
            orbit.VerticalAxis = vertical;

            vcamObject.AddComponent<CinemachineRotationComposer>();

            CinemachineDeoccluder deoccluder = vcamObject.AddComponent<CinemachineDeoccluder>();
            deoccluder.MinimumDistanceFromTarget = 0.5f;
            CinemachineDeoccluder.ObstacleAvoidance avoidance = deoccluder.AvoidObstacles;
            avoidance.Enabled = true;
            avoidance.CameraRadius = 0.2f;
            avoidance.Damping = 0.2f;
            avoidance.DampingWhenOccluded = 0.1f;
            deoccluder.AvoidObstacles = avoidance;

            ThirdPersonOrbitInput orbitInput = vcamObject.AddComponent<ThirdPersonOrbitInput>();
            orbitInput.orbit = orbit;
            orbitInput.yawSensitivity = 0.06f;
            orbitInput.pitchSensitivity = 0.04f;
            orbitInput.requireRightMouse = false;
            orbitInput.minPitch = -25f;
            orbitInput.maxPitch = 60f;
            orbitInput.startPitch = 15f;
            orbitInput.radius = 6f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ECHO ThirdPersonCameraRig prefab built: " + PrefabPath);
        }

        [MenuItem("ECHO/Apply ThirdPerson Camera to L2/L3")]
        public static void ApplyToL2AndL3()
        {
            ApplyToScene(Level02Path);
            ApplyToScene(Level03Path);
        }

        [MenuItem("ECHO/Apply ThirdPerson Camera to Active Scene")]
        public static void ApplyToActiveScene()
        {
            ApplyToScene(EditorSceneManager.GetActiveScene().path);
        }

        /// <summary>
        /// 场景构建器调用：Prefab 不存在时先创建，再应用到当前场景。
        /// 这样重建 L1/L3 后不会退回自定义俯视 CameraRig。
        /// </summary>
        public static void EnsureAndApplyToActiveScene()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                BuildPrefab();
            }

            ApplyToActiveScene();
        }

        private static void ApplyToScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("ThirdPersonCameraBuilder: prefab missing. Run Build ThirdPerson Camera Prefab first.");
                return;
            }

            // 关键：场景构建器正在当前场景里刚生成对象时，不能再 OpenScene 同一个路径，
            // 否则会从磁盘重新加载，丢掉尚未保存的板子/门/机关。
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            DestroyIfExists("ThirdPersonCameraRig");
            DestroyIfExists("ThirdPersonCameraRig(Clone)");
            DestroyIfExists("VCam_IndoorThirdPerson");
            DestroyIfExists("VCam_L3ThirdPerson");
            DestroyIfExists("CameraTarget");

            GameObject player = GameObject.Find("L1_Player");
            GameLoop loop = Object.FindAnyObjectByType<GameLoop>();
            if (player == null && loop != null && loop.playerView != null)
            {
                player = loop.playerView.gameObject;
            }

            if (player == null)
            {
                Debug.LogError("ThirdPersonCameraBuilder: player missing in " + scenePath);
                return;
            }

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.name = "ThirdPersonCameraRig";

            Transform target = rig.transform.Find("CameraTarget");
            CameraTargetFollow targetFollow = target != null
                ? target.GetComponent<CameraTargetFollow>()
                : null;
            if (targetFollow != null)
            {
                targetFollow.target = player.transform;
                targetFollow.offset = new Vector3(0f, 1.2f, 0f);
                target.position = player.transform.position + targetFollow.offset;
            }

            CinemachineCamera vcam = rig.GetComponentInChildren<CinemachineCamera>();
            if (vcam != null && target != null)
            {
                vcam.Follow = target;
                vcam.LookAt = target;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindAnyObjectByType<Camera>();
            }

            if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
            {
                mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            CameraRig oldRig = Object.FindAnyObjectByType<CameraRig>();
            if (oldRig != null)
            {
                oldRig.enabled = false;
            }

            if (loop != null)
            {
                loop.cameraRig = null;
                loop.cameraRelativeMovement = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("ECHO third-person camera applied: " + scenePath);
        }

        private static void DestroyIfExists(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }
}
