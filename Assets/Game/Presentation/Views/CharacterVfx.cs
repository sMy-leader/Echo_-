using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 角色通用 VFX。只做表现，不参与玩法。
    ///
    /// 组成：
    /// - Hover：脚底悬停光圈（粒子环）。
    /// - Core：胸口能量核心（粒子 + 发光色）。
    /// - Trail：移动拖尾（TrailRenderer）。
    /// - AlertPulse：守卫进入 Alert 时的红色扩散环。
    /// - Burst：残影生成/消散的爆发粒子。
    ///
    /// 所有对象都在运行时创建，不依赖外部 ParticleSystem 资产；
    /// 正式美术可以后续替换成 VFX Graph / 专用 Prefab。
    /// </summary>
    public sealed class CharacterVfx : MonoBehaviour
    {
        [Header("References")]
        public Transform visualRoot;
        public Material particleMaterial;

        [Header("Style")]
        public Color primaryColor = new Color(0.2f, 0.9f, 1f);
        public float hoverRadius = 0.55f;
        public float hoverY = 0.03f;
        public float coreHeight = 1.0f;
        public float trailTime = 0.3f;
        public float trailWidth = 0.18f;

        [Header("Toggles")]
        public bool enableHover = true;
        public bool enableCore = true;
        public bool enableTrail = true;
        public bool enableAlertPulse = true;

        private ParticleSystem _hover;
        private ParticleSystem _core;
        private TrailRenderer _trail;
        private ParticleSystem _alertPulse;
        private ParticleSystem _burst;
        private Material _runtimeMaterial;
        private bool _isMoving;
        private bool _isAlert;

        private void Awake()
        {
            Build();
        }

        /// <summary>
        /// 运行时创建残影时使用。GameLoop 会在 GameObject 激活前调用，
        /// 因此 Awake 中的 Build 能拿到最终的颜色/材质/visualRoot。
        /// </summary>
        public void Initialize(Transform visualRoot, Material material, Color color, float coreHeight)
        {
            this.visualRoot = visualRoot;
            particleMaterial = material;
            primaryColor = color;
            this.coreHeight = coreHeight;
        }

        public void SetMoving(bool moving)
        {
            _isMoving = moving;
            if (_trail != null)
            {
                _trail.emitting = moving;
            }
        }

        /// <summary>
        /// 只在进入 Alert 的那一次触发脉冲，避免每帧重复 Emit。
        /// </summary>
        public void SetAlert(bool alert)
        {
            if (alert && !_isAlert && _alertPulse != null)
            {
                _alertPulse.Emit(24);
            }

            _isAlert = alert;
        }

        public void PlaySpawnBurst()
        {
            if (_burst != null)
            {
                _burst.Emit(20);
            }
        }

        public void PlayDespawnBurst()
        {
            if (_burst != null)
            {
                _burst.Emit(28);
            }
        }

        private void Build()
        {
            if (_hover != null || _core != null || _trail != null)
            {
                return;
            }

            Material material = ResolveMaterial();

            if (enableHover)
            {
                _hover = CreateHover(material);
            }

            if (enableCore)
            {
                _core = CreateCore(material);
            }

            if (enableTrail)
            {
                _trail = CreateTrail(material);
            }

            if (enableAlertPulse)
            {
                _alertPulse = CreateAlertPulse(material);
            }

            _burst = CreateBurst(material);
            SetMoving(_isMoving);
        }

        private Material ResolveMaterial()
        {
            if (particleMaterial != null)
            {
                return particleMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            _runtimeMaterial = new Material(shader)
            {
                name = "VFX_Runtime_Additive",
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
            };

            // 尽量把 URP Particles/Unlit 切到 Additive；非 URP 材质会忽略这些属性。
            _runtimeMaterial.SetFloat("_Surface", 1f);
            _runtimeMaterial.SetFloat("_Blend", 2f);
            _runtimeMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _runtimeMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _runtimeMaterial.SetFloat("_ZWrite", 0f);
            _runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return _runtimeMaterial;
        }

        private ParticleSystem CreateHover(Material material)
        {
            ParticleSystem ps = CreateParticleObject("VFX_Hover", transform, material);
            ps.transform.localPosition = new Vector3(0f, hoverY, 0f);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.8f;
            main.startSpeed = 0f;
            main.startSize = 0.13f;
            main.startColor = primaryColor;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 30f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = hoverRadius;
            shape.radiusThickness = 0.1f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            ApplyFadeOverLifetime(ps);

            ps.Play();
            return ps;
        }

        private ParticleSystem CreateCore(Material material)
        {
            Transform parent = visualRoot != null ? visualRoot : transform;
            ParticleSystem ps = CreateParticleObject("VFX_Core", parent, material);
            ps.transform.localPosition = new Vector3(0f, coreHeight, 0f);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.05f;
            main.startSize = 0.09f;
            main.startColor = primaryColor;
            main.maxParticles = 32;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 16f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.07f;

            ApplyFadeOverLifetime(ps);

            ps.Play();
            return ps;
        }

        private TrailRenderer CreateTrail(Material material)
        {
            GameObject go = new GameObject("VFX_Trail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, coreHeight, 0f);

            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = trailWidth;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.material = material;
            trail.startColor = primaryColor;
            trail.endColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0f);
            trail.emitting = false;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            return trail;
        }

        private ParticleSystem CreateAlertPulse(Material material)
        {
            ParticleSystem ps = CreateParticleObject("VFX_AlertPulse", transform, material);
            ps.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.5f;
            main.startSpeed = 1.6f;
            main.startSize = 0.14f;
            main.startColor = new Color(1f, 0.15f, 0.1f);
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            shape.radiusThickness = 0f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        private ParticleSystem CreateBurst(Material material)
        {
            ParticleSystem ps = CreateParticleObject("VFX_Burst", transform, material);
            ps.transform.localPosition = new Vector3(0f, 1f, 0f);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.5f;
            main.startSpeed = 1.8f;
            main.startSize = 0.1f;
            main.startColor = primaryColor;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        private static ParticleSystem CreateParticleObject(string name, Transform parent, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = material;
            renderer.sortingFudge = 10f;

            return ps;
        }

        private void ApplyFadeOverLifetime(ParticleSystem ps)
        {
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(primaryColor, 0f),
                    new GradientColorKey(primaryColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });

            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
            }
        }
    }
}
