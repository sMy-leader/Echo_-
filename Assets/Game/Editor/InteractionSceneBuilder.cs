using Game.Application;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// 给 L2 室内原型补拉杆布线。
    ///
    /// 不重建场景：L2 已经配置了 Cinemachine，重建会覆盖相机；
    /// 这里只创建/替换 L2_Lever，并把 DoorView / GameLoop 引用接上。
    /// </summary>
    public static class InteractionSceneBuilder
    {
        private const string LevelPath = "Assets/Scenes/Level_02_InteriorPrototype.unity";

        [MenuItem("ECHO/Build L2 Interaction Wiring")]
        public static void BuildL2Interaction()
        {
            Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("L2_Lever");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            DoorView door = null;
            GameObject doorObject = GameObject.Find("L1_Door");
            if (doorObject != null)
            {
                door = doorObject.GetComponent<DoorView>();
            }

            GameObject leverObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leverObject.name = "L2_Lever";
            leverObject.transform.position = new Vector3(-4f, 0.8f, 9f);
            leverObject.transform.localScale = new Vector3(0.3f, 1.6f, 0.3f);
            Object.DestroyImmediate(leverObject.GetComponent<Collider>());

            LeverView lever = leverObject.AddComponent<LeverView>();
            lever.targetDoor = door;

            LevelPortalView portal = null;
            GameObject portalObject = GameObject.Find("FlowPortal");
            if (portalObject != null)
            {
                portal = portalObject.GetComponent<LevelPortalView>();
                if (portal != null)
                {
                    // L2 教学：压力板供电 + 拉杆锁存，两者同时满足才可用。
                    portal.requirePlateActive = true;
                    portal.requiredLever = lever;
                    portal.completedLevelId = LevelIds.Level02;
                }
            }

            GameLoop loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop != null)
            {
                loop.lever = lever;
            }
            else
            {
                Debug.LogWarning("InteractionSceneBuilder: GameLoop missing in L2 scene.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("ECHO L2 interaction wiring built: L2_Lever -> L1_Door");
        }
    }
}
