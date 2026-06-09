using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CastingEffect v4.4 — Palette VIOLET DOMINANTE
/// ✅ Détecte VirusAI ET SpywareAI
/// ✅ Appelle Freeze() sur les deux types
/// </summary>
public class CastingEffect : MonoBehaviour
{
    [Header("══ Références ══")]
    public Transform castPoint;
    public Animator  characterAnimator;
    [Tooltip("Nom du Trigger dans l'Animator")]
    public string    castAnimationTrigger = "Cast";
    [Tooltip("Nom du State dans l'Animator (pour détecter la fin)")]
    public string    castAnimationState   = "Casting";

    [Header("══ Durée de l'effet ══")]
    [Tooltip("0 = auto depuis la durée de l'animation détectée")]
    public float effectDuration    = 0f;
    [Tooltip("Durée minimale garantie même si l'animation est très courte")]
    public float minEffectDuration = 1.5f;

    [Header("══ Qualité (Low = mobile) ══")]
    public QualityLevel quality = QualityLevel.High;

    [Header("══ Beam ══")]
    public float beamLength    = 40f;
    public float beamGrowSpeed = 18f;
    [Range(12, 80)]
    public int   beamSegments  = 48;

    [Header("══ Beam Widths ══")]
    public float coreWidth = 0.12f;
    public float glowWidth = 1.2f;
    public float midWidth  = 0.45f;

    [Header("══ Beam Wave ══")]
    public float waveAmplitude = 0.06f;
    public float waveFrequency = 2.5f;
    public float waveSpeed     = 6f;

    [Header("══ Rings ══")]
    [Range(2, 8)]
    public int   ringCount     = 5;
    public float ringSpeed     = 8f;
    public float ringRadius    = 0.45f;
    public float ringLineWidth = 0.06f;
    [Range(8, 48)]
    public int   ringSegments  = 32;

    [Header("══ Binary Text ══")]
    public bool  enableBinaryText = true;
    [Range(4, 24)]
    public int   binaryLabelCount = 12;
    public float binaryTextSize   = 0.12f;
    public float binaryDriftSpeed = 0.5f;

    [Header("══ Colors VIOLET ══")]

    [ColorUsage(true, true)]
    public Color colorCoreViolet = new Color(0.85f, 0.30f, 1.00f, 1f);
    [ColorUsage(true, true)]
    public Color colorDeepPurple = new Color(0.35f, 0.00f, 0.80f, 1f);
    [ColorUsage(true, true)]
    public Color colorMagenta    = new Color(1.00f, 0.05f, 0.75f, 1f);
    [ColorUsage(true, true)]
    public Color colorLilac      = new Color(0.70f, 0.50f, 1.00f, 1f);
    [ColorUsage(true, true)]
    public Color colorWhiteVio   = new Color(0.95f, 0.88f, 1.00f, 1f);
    [ColorUsage(true, true)]
    public Color colorImpactVio  = new Color(0.80f, 0.10f, 1.00f, 1f);
    [ColorUsage(true, true)]
    public Color colorBinaryVio  = new Color(0.90f, 0.55f, 1.00f, 1f);

    [Range(1f, 20f)]
    public float emissionIntensity = 5f;

    [Header("══ Impact ══")]
    public float impactRadius = 0.6f;

    [Header("══ Audio ══")]
    public AudioClip scanLoopSound;
    [Range(0f, 1f)] public float volume = 0.6f;

    // ════════════════════════════════════════════════════════
    //  TYPES
    // ════════════════════════════════════════════════════════

    public enum QualityLevel { Low, Medium, High }

    private struct BinaryLabel
    {
        public TextMesh mesh;
        public float    tAlongBeam;
        public float    sideOffset;
        public float    vertOffset;
        public float    driftDir;
        public float    driftPhase;
        public float    blinkPhase;
    }

    // ════════════════════════════════════════════════════════
    //  PUBLIC READONLY
    // ════════════════════════════════════════════════════════

    public bool IsActive    => _active;
    public bool IsPlaying   => _castCoroutine != null;
    public int  QueuedCasts => _castQueue;

    // ════════════════════════════════════════════════════════
    //  PRIVATE
    // ════════════════════════════════════════════════════════

    private LineRenderer   _lrCore, _lrMid, _lrGlow, _lrGlow2;
    private LineRenderer[] _lrRings;

    private ParticleSystem _psSource, _psImpact, _psImpactGlow, _psSparks;

    private Camera        _mainCam;
    private BinaryLabel[] _binaryLabels;
    private AudioSource   _audioLoop;

    private Vector3[]            _pts;
    private Vector3[][]          _ringPtsCache;
    private Gradient[]           _ringGradCache;
    private GradientColorKey[][] _ringColorKeys;
    private GradientAlphaKey[][] _ringAlphaKeys;

    private float[]  _ringT;
    private bool     _active;
    private float    _length;
    private float    _time;
    private float    _pulseTimer;
    private float    _pulseVal;

    // ── Caches ennemis — VirusAI ET SpywareAI ──
    private VirusAI       _lastFrozenVirus   = null;
    private SpywareAI     _lastFrozenSpyware = null;
    private List<VirusAI>   _virusCacheList   = new List<VirusAI>();
    private List<SpywareAI> _spywareCacheList = new List<SpywareAI>();
    private float           _enemyCacheTimer  = 0f;
    private const float     ENEMY_CACHE_INTERVAL = 0.5f;

    private int       _castQueue     = 0;
    private Coroutine _castCoroutine = null;

    private float _detectedAnimDuration = 0f;

    private Color _ringColorA, _ringColorB, _ringColorC;

    private Vector3 BeamDir    => transform.forward;
    private Vector3 BeamOrigin => castPoint != null ? castPoint.position : transform.position;

    private static readonly string[] BinaryWords = {
        "1010","1001","0110","1100","0101",
        "100","010","110","011","101",
        "1010 1001","0110 1100","10110",
        "0xFF","0x1A","ROOT","SYS","0x00","ERR"
    };

    // ════════════════════════════════════════════════════════
    //  INIT
    // ════════════════════════════════════════════════════════

    private void Awake()
    {
        _mainCam = Camera.main;
        ApplyQualityPreset();

        _ringColorA = colorMagenta;
        _ringColorB = colorCoreViolet;
        _ringColorC = colorDeepPurple;

        _pts = new Vector3[beamSegments];

        CreateLineRenderers();
        CreateRings();
        PreallocRingCaches();
        CreateParticleSystems();
        CreateBinaryLabels();
        CreateAudio();
        DetectAnimationDuration();
    }

    private void ApplyQualityPreset()
    {
        switch (quality)
        {
            case QualityLevel.Low:
                beamSegments     = 16;
                ringCount        = 3;
                ringSegments     = 12;
                binaryLabelCount = 4;
                enableBinaryText = false;
                break;
            case QualityLevel.Medium:
                beamSegments     = 28;
                ringCount        = 4;
                ringSegments     = 20;
                binaryLabelCount = 8;
                break;
        }
    }

    private void DetectAnimationDuration()
    {
        if (characterAnimator == null) return;
        RuntimeAnimatorController rac = characterAnimator.runtimeAnimatorController;
        if (rac == null) return;

        foreach (AnimationClip clip in rac.animationClips)
        {
            string n = clip.name.ToLower();
            if (n.Contains(castAnimationState.ToLower()) || n.Contains("cast"))
            {
                _detectedAnimDuration = clip.length;
                Debug.Log($"[CastingEffect] Animation '{clip.name}' = {_detectedAnimDuration:F2}s");
                return;
            }
        }
        Debug.LogWarning("[CastingEffect] Animation non trouvée → durée = minEffectDuration");
    }

    private float GetEffectDuration()
    {
        if (effectDuration > 0f)        return Mathf.Max(effectDuration,        minEffectDuration);
        if (_detectedAnimDuration > 0f) return Mathf.Max(_detectedAnimDuration, minEffectDuration);
        return minEffectDuration;
    }

    // ════════════════════════════════════════════════════════
    //  MATERIALS & RENDERERS
    // ════════════════════════════════════════════════════════

    private Shader FindShader(bool additive)
    {
        Shader sh = null;
        if (additive)
        {
            sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Mobile/Particles/Additive");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sh == null) sh = Shader.Find("Particles/Additive");
            if (sh == null) sh = Shader.Find("Sprites/Default");
        }
        else
        {
            sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Mobile/Unlit (Supports Lightmap)");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
        }
        if (sh == null) sh = Shader.Find("Standard");
        return sh;
    }

    private Material MakeMat(Color col, float alpha = 1f, bool additive = true)
    {
        Material m = new Material(FindShader(additive));
        Color c    = col * emissionIntensity;
        c.a        = alpha;
        if (m.HasProperty("_BaseColor"))     m.SetColor("_BaseColor",     c);
        if (m.HasProperty("_Color"))         m.SetColor("_Color",         c);
        if (m.HasProperty("_TintColor"))     m.SetColor("_TintColor",
            new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, alpha));
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
        if (additive)
        {
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_ZWrite",   0);
            m.renderQueue = 3000;
        }
        return m;
    }

    private LineRenderer MakeBeamLR(string goName, float width, Color colA, Color colB,
                                    float alphaCenter, float alphaEdge)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform);
        LineRenderer lr      = go.AddComponent<LineRenderer>();
        lr.positionCount     = beamSegments;
        lr.useWorldSpace     = true;
        lr.alignment         = LineAlignment.View;
        lr.textureMode       = LineTextureMode.Tile;
        lr.numCapVertices    = 6;
        lr.numCornerVertices = 3;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.material          = MakeMat(colA);
        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f,    width * 0.5f),
            new Keyframe(0.04f, width),
            new Keyframe(0.5f,  width * 1.15f),
            new Keyframe(0.96f, width),
            new Keyframe(1f,    width * 0.25f));

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(colA, 0f),
                new GradientColorKey(colB, 0.5f),
                new GradientColorKey(colA, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(alphaEdge,   0f),
                new GradientAlphaKey(alphaCenter, 0.08f),
                new GradientAlphaKey(alphaCenter, 0.92f),
                new GradientAlphaKey(alphaEdge,   1f)
            });
        lr.colorGradient = g;
        lr.enabled       = false;
        return lr;
    }

    private void CreateLineRenderers()
    {
        _lrGlow  = MakeBeamLR("AV_Glow",  glowWidth,         colorDeepPurple, colorDeepPurple, 0.45f, 0f);
        _lrGlow2 = MakeBeamLR("AV_Glow2", glowWidth * 0.55f, colorCoreViolet, colorLilac,      0.55f, 0f);
        _lrMid   = MakeBeamLR("AV_Mid",   midWidth,          colorLilac,      colorWhiteVio,   0.85f, 0f);
        _lrCore  = MakeBeamLR("AV_Core",  coreWidth,         colorWhiteVio,   colorCoreViolet, 1f,    0.4f);
    }

    private void CreateRings()
    {
        _lrRings = new LineRenderer[ringCount];
        _ringT   = new float[ringCount];
        for (int i = 0; i < ringCount; i++)
        {
            GameObject go = new GameObject("AV_Ring_" + i);
            go.transform.SetParent(transform);
            LineRenderer lr      = go.AddComponent<LineRenderer>();
            lr.positionCount     = ringSegments + 1;
            lr.useWorldSpace     = true;
            lr.loop              = false;
            lr.alignment         = LineAlignment.View;
            lr.numCapVertices    = 4;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            Color rc      = RingColor(i);
            lr.material   = MakeMat(rc);
            lr.startWidth = ringLineWidth;
            lr.endWidth   = ringLineWidth;
            lr.enabled    = false;
            _lrRings[i]   = lr;
            _ringT[i]     = (float)i / ringCount;
        }
    }

    private Color RingColor(int i)
    {
        return i % 3 == 0 ? _ringColorA :
               i % 3 == 1 ? _ringColorB : _ringColorC;
    }

    private void PreallocRingCaches()
    {
        _ringPtsCache  = new Vector3[ringCount][];
        _ringGradCache = new Gradient[ringCount];
        _ringColorKeys = new GradientColorKey[ringCount][];
        _ringAlphaKeys = new GradientAlphaKey[ringCount][];
        for (int i = 0; i < ringCount; i++)
        {
            _ringPtsCache[i]  = new Vector3[ringSegments + 1];
            _ringGradCache[i] = new Gradient();
            Color rc = RingColor(i);
            _ringColorKeys[i] = new GradientColorKey[] {
                new GradientColorKey(rc,              0f),
                new GradientColorKey(colorCoreViolet, 0.5f),
                new GradientColorKey(rc,              1f)
            };
            _ringAlphaKeys[i] = new GradientAlphaKey[] {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.8f, 0.88f),
                new GradientAlphaKey(0f,   1f)
            };
        }
    }

    // ════════════════════════════════════════════════════════
    //  PARTICLE SYSTEMS
    // ════════════════════════════════════════════════════════

    private void CreateParticleSystems()
    {
        _psSource     = MakeSourcePS();
        _psImpact     = MakeImpactPS();
        _psImpactGlow = MakeImpactGlowPS();
        _psSparks     = (quality != QualityLevel.Low) ? MakeSparksPS() : null;
    }

    private ParticleSystem MakeBasePS(string n)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        ParticleSystem ps    = go.AddComponent<ParticleSystem>();
        var main             = ps.main;
        main.loop            = true;
        main.playOnAwake     = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private ParticleSystem MakeSourcePS()
    {
        var ps = MakeBasePS("AV_PS_Source");
        var m  = ps.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        m.startSpeed    = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
        m.startSize     = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        m.startColor    = new ParticleSystem.MinMaxGradient(colorCoreViolet, colorWhiteVio);
        m.maxParticles  = quality == QualityLevel.High ? 100 : 40;
        var e = ps.emission; e.rateOverTime = quality == QualityLevel.High ? 70f : 30f;
        var s = ps.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Circle; s.radius = 0.05f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(colorWhiteVio,   0f),
                new GradientColorKey(colorCoreViolet, 0.3f),
                new GradientColorKey(colorDeepPurple, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f,   1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material   = MakeMat(colorCoreViolet);
        return ps;
    }

    private ParticleSystem MakeImpactPS()
    {
        var ps = MakeBasePS("AV_PS_Impact");
        var m  = ps.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        m.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 8f);
        m.startSize     = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
        m.startColor    = new ParticleSystem.MinMaxGradient(colorImpactVio, colorWhiteVio);
        m.maxParticles  = quality == QualityLevel.High ? 150 : 50;
        var e = ps.emission; e.rateOverTime = quality == QualityLevel.High ? 80f : 30f;
        var s = ps.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Sphere; s.radius = impactRadius * 0.25f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(colorWhiteVio,   0f),
                new GradientColorKey(colorImpactVio,  0.15f),
                new GradientColorKey(colorMagenta,    0.6f),
                new GradientColorKey(colorDeepPurple, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.9f, 0.4f),
                new GradientAlphaKey(0f,   1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode    = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.3f;
        r.lengthScale   = 4f;
        r.material      = MakeMat(colorImpactVio);
        return ps;
    }

    private ParticleSystem MakeImpactGlowPS()
    {
        var ps = MakeBasePS("AV_PS_ImpactGlow");
        var m  = ps.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        m.startSpeed    = 0f;
        m.startSize     = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        m.startColor    = new ParticleSystem.MinMaxGradient(colorImpactVio, colorLilac);
        m.maxParticles  = quality == QualityLevel.High ? 25 : 10;
        var e = ps.emission; e.rateOverTime = quality == QualityLevel.High ? 30f : 12f;
        var s = ps.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Sphere; s.radius = 0.04f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(colorWhiteVio,  0f),
                new GradientColorKey(colorImpactVio, 0.4f),
                new GradientColorKey(colorMagenta,   1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.4f, 0.5f),
                new GradientAlphaKey(0f,   1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material   = MakeMat(colorImpactVio, 0.6f);
        return ps;
    }

    private ParticleSystem MakeSparksPS()
    {
        var ps = MakeBasePS("AV_PS_Sparks");
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 3f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        m.startColor      = new ParticleSystem.MinMaxGradient(colorMagenta, colorCoreViolet);
        m.maxParticles    = quality == QualityLevel.High ? 60 : 25;
        m.gravityModifier = 0.05f;
        var e = ps.emission; e.rateOverTime = quality == QualityLevel.High ? 40f : 15f;
        var s = ps.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Box;
        s.scale = new Vector3(0.1f, 0.1f, beamLength * 0.5f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(colorCoreViolet, 0f),
                new GradientColorKey(colorMagenta,    1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material   = MakeMat(colorMagenta);
        return ps;
    }

    private void CreateBinaryLabels()
    {
        if (!enableBinaryText || quality == QualityLevel.Low) return;
        _binaryLabels = new BinaryLabel[binaryLabelCount];
        for (int i = 0; i < binaryLabelCount; i++)
        {
            GameObject go = new GameObject("AV_Binary_" + i);
            go.transform.SetParent(transform);
            TextMesh tm      = go.AddComponent<TextMesh>();
            tm.text          = BinaryWords[Random.Range(0, BinaryWords.Length)];
            tm.fontSize      = 28;
            tm.characterSize = binaryTextSize * 0.1f;
            tm.anchor        = TextAnchor.MiddleCenter;
            tm.alignment     = TextAlignment.Center;
            tm.color         = colorBinaryVio * emissionIntensity;
            tm.fontStyle     = FontStyle.Bold;
            go.SetActive(false);
            _binaryLabels[i] = new BinaryLabel {
                mesh       = tm,
                tAlongBeam = Random.Range(0.05f, 0.92f),
                sideOffset = Random.Range(-ringRadius * 1.5f, ringRadius * 1.5f),
                vertOffset = Random.Range(-ringRadius * 1.0f, ringRadius * 1.0f),
                driftDir   = Random.value > 0.5f ? 1f : -1f,
                driftPhase = Random.Range(0f, Mathf.PI * 2f),
                blinkPhase = Random.Range(0f, Mathf.PI * 2f)
            };
        }
    }

    private void CreateAudio()
    {
        _audioLoop              = gameObject.AddComponent<AudioSource>();
        _audioLoop.loop         = true;
        _audioLoop.spatialBlend = 1f;
        _audioLoop.volume       = volume;
        if (scanLoopSound != null) _audioLoop.clip = scanLoopSound;
    }

    // ════════════════════════════════════════════════════════
    //  UPDATE
    // ════════════════════════════════════════════════════════

    private void Update()
    {
        if (!_active || castPoint == null) return;

        _length      = Mathf.MoveTowards(_length, beamLength, beamGrowSpeed * Time.deltaTime);
        _time       += Time.deltaTime;
        _pulseTimer += Time.deltaTime;
        _pulseVal    = 1f + Mathf.Sin(_pulseTimer * 6f) * 0.10f;

        ComputeBeamPoints();
        ApplyBeamLRs();
        PulseBeam();
        UpdateRings();
        UpdateImpact();
        UpdateBinaryText();
        UpdateSourcePS();
        UpdateSparksPS();
    }

    private void ComputeBeamPoints()
    {
        Vector3 origin = BeamOrigin;
        Vector3 fwd    = BeamDir;
        Vector3 up     = Vector3.up;
        Vector3 right  = Vector3.Cross(fwd, up).normalized;
        up             = Vector3.Cross(right, fwd).normalized;

        for (int i = 0; i < beamSegments; i++)
        {
            float t    = (float)i / (beamSegments - 1);
            float dist = t * _length;
            float w1   = Mathf.Sin(t * waveFrequency * Mathf.PI * 2f - _time * waveSpeed)
                         * waveAmplitude * Mathf.Sin(t * Mathf.PI);
            float w2   = Mathf.Sin(t * waveFrequency * 1.7f * Mathf.PI * 2f - _time * waveSpeed * 1.3f)
                         * waveAmplitude * 0.4f;
            _pts[i] = origin + fwd * dist + up * (w1 + w2);
        }
    }

    private void ApplyBeamLRs()
    {
        _lrGlow.SetPositions(_pts);
        _lrGlow2.SetPositions(_pts);
        _lrMid.SetPositions(_pts);
        _lrCore.SetPositions(_pts);
    }

    private void PulseBeam()
    {
        if (_lrGlow  != null) _lrGlow.widthMultiplier  = _pulseVal * 1.1f;
        if (_lrGlow2 != null) _lrGlow2.widthMultiplier = _pulseVal * 0.95f;
        if (_lrMid   != null) _lrMid.widthMultiplier   = _pulseVal;
        if (_lrCore  != null) _lrCore.widthMultiplier  = _pulseVal * 1.05f;
    }

    private void UpdateRings()
    {
        Vector3 origin = BeamOrigin;
        Vector3 fwd    = BeamDir;
        Vector3 up     = Vector3.up;
        Vector3 right  = Vector3.Cross(fwd, up).normalized;
        up             = Vector3.Cross(right, fwd).normalized;

        for (int i = 0; i < ringCount; i++)
        {
            _ringT[i] += (ringSpeed * Time.deltaTime) / Mathf.Max(_length, 0.1f);
            if (_ringT[i] > 1f) _ringT[i] -= 1f;

            float t     = _ringT[i];
            float dist  = t * _length;
            Vector3 ctr = origin + fwd * dist;
            float pulse = 1f + Mathf.Sin(_time * 10f + i * 1.2f) * 0.14f;
            float alpha = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 3f);

            _ringAlphaKeys[i][0].alpha = alpha;
            _ringAlphaKeys[i][1].alpha = alpha * 0.8f;
            _ringAlphaKeys[i][2].alpha = 0f;
            _ringGradCache[i].SetKeys(_ringColorKeys[i], _ringAlphaKeys[i]);

            _lrRings[i].colorGradient   = _ringGradCache[i];
            _lrRings[i].widthMultiplier = pulse;

            float r = ringRadius * pulse;
            for (int s = 0; s <= ringSegments; s++)
            {
                float a = (float)s / ringSegments * Mathf.PI * 2f;
                _ringPtsCache[i][s] = ctr + right * Mathf.Cos(a) * r + up * Mathf.Sin(a) * r;
            }
            _lrRings[i].SetPositions(_ringPtsCache[i]);
        }
    }

    // ════════════════════════════════════════════════════════
    //  UPDATE IMPACT — détecte VirusAI ET SpywareAI
    // ════════════════════════════════════════════════════════

    private void UpdateImpact()
    {
        Vector3 origin = BeamOrigin;
        Vector3 fwd    = BeamDir;
        Vector3 tip    = origin + fwd * _length;

        // ── Refresh cache ennemis ──
        _enemyCacheTimer -= Time.deltaTime;
        if (_enemyCacheTimer <= 0f)
        {
            _enemyCacheTimer = ENEMY_CACHE_INTERVAL;
            _virusCacheList.Clear();
            _spywareCacheList.Clear();
            _virusCacheList.AddRange(FindObjectsOfType<VirusAI>());
            _spywareCacheList.AddRange(FindObjectsOfType<SpywareAI>());
        }

        VirusAI   detVirus   = null;
        SpywareAI detSpyware = null;
        float closest = Mathf.Infinity;

        // ── Raycast principal ──
        RaycastHit[] hits = Physics.RaycastAll(origin, fwd, _length,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        foreach (RaycastHit h in hits)
        {
            if (h.distance >= closest) continue;

            VirusAI v = h.collider.GetComponent<VirusAI>()
                     ?? h.collider.GetComponentInParent<VirusAI>();
            if (v != null) { closest = h.distance; detVirus = v; detSpyware = null; tip = h.point; continue; }

            SpywareAI sp = h.collider.GetComponent<SpywareAI>()
                        ?? h.collider.GetComponentInParent<SpywareAI>();
            if (sp != null) { closest = h.distance; detSpyware = sp; detVirus = null; tip = h.point; }
        }

        // ── OverlapSphere fallback ──
        if (detVirus == null && detSpyware == null)
        {
            Collider[] cols = Physics.OverlapSphere(origin + fwd * _length, impactRadius * 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            foreach (var c in cols)
            {
                VirusAI v = c.GetComponent<VirusAI>() ?? c.GetComponentInParent<VirusAI>();
                if (v != null) { detVirus = v; break; }

                SpywareAI sp = c.GetComponent<SpywareAI>() ?? c.GetComponentInParent<SpywareAI>();
                if (sp != null) { detSpyware = sp; break; }
            }
        }

        // ── Distance-along-beam fallback ──
        if (detVirus == null && detSpyware == null)
        {
            foreach (VirusAI v in _virusCacheList)
            {
                if (v == null) continue;
                float dl = Vector3.Cross(fwd, v.transform.position - origin).magnitude;
                float da = Vector3.Dot(v.transform.position - origin, fwd);
                if (da > 0f && da < _length && dl < 1.2f) { detVirus = v; break; }
            }
            if (detVirus == null)
            {
                foreach (SpywareAI sp in _spywareCacheList)
                {
                    if (sp == null) continue;
                    float dl = Vector3.Cross(fwd, sp.transform.position - origin).magnitude;
                    float da = Vector3.Dot(sp.transform.position - origin, fwd);
                    if (da > 0f && da < _length && dl < 1.2f) { detSpyware = sp; break; }
                }
            }
        }

        // ── Appliquer Freeze ──
        if (detVirus != null && detVirus != _lastFrozenVirus)
        {
            _lastFrozenVirus = detVirus;
            _lastFrozenSpyware = null;
            detVirus.Freeze(detVirus.freezeDuration);
            Debug.Log($"[BEAM] FREEZE VirusAI → {detVirus.gameObject.name}");
        }
        else if (detSpyware != null && detSpyware != _lastFrozenSpyware)
        {
            _lastFrozenSpyware = detSpyware;
            _lastFrozenVirus   = null;
            detSpyware.Freeze(detSpyware.freezeDuration);
            Debug.Log($"[BEAM] FREEZE SpywareAI → {detSpyware.gameObject.name}");
        }
        else if (detVirus == null && detSpyware == null)
        {
            _lastFrozenVirus   = null;
            _lastFrozenSpyware = null;
        }

        // ── Positionner particules d'impact ──
        Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);
        if (_psImpact     != null) { _psImpact.transform.position     = tip; _psImpact.transform.rotation     = rot; }
        if (_psImpactGlow != null) { _psImpactGlow.transform.position = tip; _psImpactGlow.transform.rotation = rot; }
    }

    private void UpdateBinaryText()
    {
        if (!enableBinaryText || _binaryLabels == null) return;
        Vector3 origin = BeamOrigin;
        Vector3 fwd    = BeamDir;
        Vector3 up     = Vector3.up;
        Vector3 right  = Vector3.Cross(fwd, up).normalized;
        up             = Vector3.Cross(right, fwd).normalized;
        if (_mainCam == null) _mainCam = Camera.main;

        for (int i = 0; i < _binaryLabels.Length; i++)
        {
            BinaryLabel lb = _binaryLabels[i];
            if (lb.mesh == null) continue;

            float t    = lb.tAlongBeam;
            float dist = t * _length;
            if (dist < 0.2f) continue;

            float drift = Mathf.Sin(_time * binaryDriftSpeed + lb.driftPhase) * ringRadius * 0.9f;
            Vector3 pos = origin + fwd * dist
                        + right * (lb.sideOffset + drift * lb.driftDir)
                        + up * lb.vertOffset;

            lb.mesh.transform.position = pos;
            if (_mainCam != null) lb.mesh.transform.LookAt(_mainCam.transform.position);

            float blink = Mathf.Clamp01(Mathf.Sin(_time * 3f + lb.blinkPhase) * 2f);
            float alpha = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 2f) * (0.6f + blink * 0.4f);
            Color c     = colorBinaryVio * emissionIntensity;
            c.a         = alpha;
            lb.mesh.color    = c;
            _binaryLabels[i] = lb;
        }
    }

    private void UpdateSourcePS()
    {
        if (_psSource == null) return;
        _psSource.transform.position = BeamOrigin;
        _psSource.transform.rotation = Quaternion.LookRotation(BeamDir, Vector3.up);
    }

    private void UpdateSparksPS()
    {
        if (_psSparks == null) return;
        _psSparks.transform.position = BeamOrigin + BeamDir * (_length * 0.5f);
        _psSparks.transform.rotation = Quaternion.LookRotation(BeamDir, Vector3.up);
    }

    // ════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════

    public void ForceStartEffect()
    {
        if (GameManager.Instance != null && GameManager.anti < 0)
        {
            Debug.LogWarning("[CastingEffect] ForceStartEffect bloqué — anti < 0 !");
            return;
        }
        if (_castCoroutine != null) return;
        _castCoroutine = StartCoroutine(ForceLoop());
    }

    private IEnumerator ForceLoop()
    {
        StartEffectInternal();
        while (_active) yield return null;
        _castCoroutine = null;
    }

    public void ForceStopEffect()
    {
        StopEffectInternal();
    }

    public void TryCast()
    {
        if (GameManager.Instance == null || GameManager.anti <= 0)
        {
            Debug.Log("[CastingEffect] Aucun antivirus disponible !");
            return;
        }
        if (castPoint == null)
        {
            Debug.LogWarning("[CastingEffect] castPoint non assigné !");
            return;
        }
        GameManager.anti--;
        Debug.Log($"[CastingEffect] Antivirus consommé. Restants : {GameManager.anti}");
        if (_castCoroutine == null)
            _castCoroutine = StartCoroutine(CastLoop());
        else
        {
            _castQueue++;
            Debug.Log($"[CastingEffect] En attente ({_castQueue} en file)");
        }
    }

    private IEnumerator CastLoop()
    {
        do
        {
            yield return StartCoroutine(RunOneCast());
            if (_castQueue > 0) { _castQueue--; yield return new WaitForSeconds(0.15f); }
            else break;
        } while (true);
        _castCoroutine = null;
    }

    private IEnumerator RunOneCast()
    {
        float duration = GetEffectDuration();
        if (characterAnimator != null) characterAnimator.SetTrigger(castAnimationTrigger);
        StartEffectInternal();

        float elapsed  = 0f;
        bool  animDone = false;

        while (elapsed < duration || !animDone)
        {
            elapsed += Time.deltaTime;
            if (elapsed > 0.25f && characterAnimator != null && !animDone)
            {
                AnimatorStateInfo info = characterAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(castAnimationState) && info.normalizedTime >= 0.95f) animDone = true;
            }
            bool timeUp = elapsed >= duration;
            bool noAnim = characterAnimator == null;
            if (timeUp && (animDone || noAnim)) break;
            if (elapsed >= duration * 2f) break;
            yield return null;
        }

        StopEffectInternal();
        yield return new WaitForSeconds(0.4f);
    }

    private void StartEffectInternal()
    {
        if (_active) return;
        if (_mainCam == null) _mainCam = Camera.main;

        _active          = true;
        _length          = 0f;
        _time            = 0f;
        _pulseTimer      = 0f;
        _enemyCacheTimer = 0f;

        _lrCore.enabled  = true;
        _lrMid.enabled   = true;
        _lrGlow.enabled  = true;
        _lrGlow2.enabled = true;

        for (int i = 0; i < ringCount; i++)
        {
            _lrRings[i].enabled = true;
            _ringT[i]           = (float)i / ringCount;
        }

        if (_psSource     != null) _psSource.Play();
        if (_psImpact     != null) _psImpact.Play();
        if (_psImpactGlow != null) _psImpactGlow.Play();
        if (_psSparks     != null) _psSparks.Play();

        if (_binaryLabels != null)
            foreach (var lb in _binaryLabels)
                if (lb.mesh != null) lb.mesh.gameObject.SetActive(true);

        if (scanLoopSound != null && !_audioLoop.isPlaying) _audioLoop.Play();
    }

    private void StopEffectInternal()
    {
        if (!_active) return;
        StartCoroutine(Retract());
    }

    private IEnumerator Retract()
    {
        _active = false;
        while (_length > 0.05f)
        {
            _length = Mathf.MoveTowards(_length, 0f, beamGrowSpeed * 5f * Time.deltaTime);
            ComputeBeamPoints();
            ApplyBeamLRs();
            UpdateRings();
            yield return null;
        }

        _lrCore.enabled  = false;
        _lrMid.enabled   = false;
        _lrGlow.enabled  = false;
        _lrGlow2.enabled = false;
        for (int i = 0; i < ringCount; i++) _lrRings[i].enabled = false;

        if (_psSource     != null) _psSource.Stop();
        if (_psImpact     != null) _psImpact.Stop();
        if (_psImpactGlow != null) _psImpactGlow.Stop();
        if (_psSparks     != null) _psSparks.Stop();

        if (_binaryLabels != null)
            foreach (var lb in _binaryLabels)
                if (lb.mesh != null) lb.mesh.gameObject.SetActive(false);

        if (_audioLoop != null && _audioLoop.isPlaying) _audioLoop.Stop();

        _lastFrozenVirus   = null;
        _lastFrozenSpyware = null;
        _virusCacheList.Clear();
        _spywareCacheList.Clear();
        Debug.Log("[CastingEffect] ■ Effet violet terminé");
    }

    // ════════════════════════════════════════════════════════
    //  GIZMOS
    // ════════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        if (castPoint == null) return;
        Vector3 o = castPoint.position, d = transform.forward;
        Gizmos.color = new Color(0.7f, 0.2f, 1f, 1f);
        Gizmos.DrawRay(o, d * beamLength);
        Gizmos.DrawWireSphere(o, 0.06f);
        Gizmos.color = new Color(1f, 0.1f, 0.8f, 0.5f);
        Gizmos.DrawWireSphere(o + d * beamLength, impactRadius);
    }
}