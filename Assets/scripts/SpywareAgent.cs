using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class SpywareAI : MonoBehaviour
{
    [Header("── Déplacement & Ciblage ──")]
    public float  detectionRadius = 30f;
    public float  attackRange     = 1.5f;
    public string targetTag       = "DataTarget";

    [Header("── Vol de Données ──")]
    public float stealDelay = 0.6f;

    [Header("── Animation ──")]
    public string walkParamName  = "walk";
    public string tombeParamName = "tombe";

    [Header("── Freeze ──")]
    public float freezeDuration = 3f;

    [Header("── Invisibilité ──")]
    public bool startInvisible = true;

    [Header("── Audio ──")]
    public AudioClip tombeSound;
    public AudioClip glitchSound;
    [Range(0f, 1f)] public float tombeSoundVolume  = 0.8f;
    [Range(0f, 1f)] public float glitchSoundVolume = 0.5f;

    [Header("── Effets visuels ──")]
    public float flashIntensity  = 2.5f;
    public Color freezeGlowColor = new Color(0.6f, 0.1f, 1f, 1f);
    public Color shockwaveColor  = new Color(0.8f, 0.2f, 1f, 0.8f);

    // ── Composants ──
    private NavMeshAgent agent;
    private Animator     anim;
    private AudioSource  audioMain;
    private AudioSource  audioGlitch;

    // ── État ──
    private List<DataTarget> allTargets           = new List<DataTarget>();
    private DataTarget       currentTarget        = null;
    private bool             isBusy               = false;
    private bool             isFrozen             = false;
    private bool             isVisible            = false;
    private bool             hasBeenRevealed      = false;
    private Coroutine        freezeCoroutine      = null;
    private bool             _questFreezeNotified = false; // Q3 notifié une seule fois

    public bool IsVisible => isVisible;

    // ── Effets ──
    private ParticleSystem psGlitch;
    private ParticleSystem psSparks;
    private ParticleSystem psSmoke;
    private LineRenderer[] shockRings;
    private GameObject     haloGlow;
    private GameObject     floatingText;
    private Renderer[]     bodyRenderers;
    private Color[]        originalColors;
    private Light          freezeLight;

    // ── Minimap ──
    private RawImage minimapIcon;

    private const int RING_COUNT    = 3;
    private const int RING_SEGMENTS = 32;

    // ════════════════════════════════════════════════════════
    //  INIT
    // ════════════════════════════════════════════════════════

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();

        audioMain              = gameObject.AddComponent<AudioSource>();
        audioMain.spatialBlend = 1f;
        audioMain.volume       = tombeSoundVolume;
        audioMain.playOnAwake  = false;

        audioGlitch              = gameObject.AddComponent<AudioSource>();
        audioGlitch.spatialBlend = 1f;
        audioGlitch.volume       = glitchSoundVolume;
        audioGlitch.playOnAwake  = false;

        agent.stoppingDistance = attackRange * 0.8f;

        bodyRenderers  = GetBodyRenderers();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
            originalColors[i] = bodyRenderers[i].material.color;

        minimapIcon = GetComponentInChildren<RawImage>(true);
        if (minimapIcon == null)
            Debug.LogWarning("[SpywareAI] Aucun RawImage trouvé dans les enfants pour la minimap.");

        BuildParticleSystems();
        BuildShockRings();
        BuildHaloGlow();
        BuildFreezeLight();
        BuildFloatingText();

        RefreshTargets();
        InvokeRepeating("UpdateDetection", 0f, 0.5f);

        if (startInvisible)
            MakeInvisible();
        else
            isVisible = true;
    }

    private Renderer[] GetBodyRenderers()
    {
        List<Renderer> result = new List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                result.Add(r);
        }
        return result.ToArray();
    }

    // ════════════════════════════════════════════════════════
    //  INVISIBILITÉ & RÉVÉLATION
    // ════════════════════════════════════════════════════════

    void MakeInvisible()
    {
        if (hasBeenRevealed) return;
        isVisible = false;
        bodyRenderers  = GetBodyRenderers();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            originalColors[i]        = bodyRenderers[i].material.color;
            bodyRenderers[i].enabled = false;
        }
        if (minimapIcon != null) minimapIcon.enabled = false;
        if (psGlitch    != null) psGlitch.gameObject.SetActive(false);
        if (psSparks    != null) psSparks.gameObject.SetActive(false);
        if (psSmoke     != null) psSmoke .gameObject.SetActive(false);
        if (haloGlow    != null) haloGlow.SetActive(false);
        if (freezeLight != null) freezeLight.intensity = 0f;
        Debug.Log($"[SpywareAI] {gameObject.name} est INVISIBLE.");
    }

    public void Reveal()
    {
        if (isVisible) return;
        isVisible       = true;
        hasBeenRevealed = true;
        bodyRenderers  = GetBodyRenderers();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            originalColors[i]        = bodyRenderers[i].material.color;
            bodyRenderers[i].enabled = true;
        }
        if (minimapIcon != null) minimapIcon.enabled = true;
        StartCoroutine(RevealFlash());
        Debug.Log($"[SpywareAI] {gameObject.name} révélé DÉFINITIVEMENT !");
    }

    private IEnumerator RevealFlash()
    {
        Color flashCol = new Color(0.9f, 0.3f, 1f, 1f);
        float t = 0f, duration = 0.8f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.PingPong(t * 12f, 1f);
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].material.color = Color.Lerp(originalColors[i], flashCol, p);
            yield return null;
        }
        for (int i = 0; i < bodyRenderers.Length; i++)
            if (bodyRenderers[i] != null)
                bodyRenderers[i].material.color = originalColors[i];
        if (psGlitch != null)
        {
            psGlitch.gameObject.SetActive(true);
            psGlitch.transform.position = transform.position + Vector3.up * 0.8f;
            psGlitch.Play();
        }
    }

    // ════════════════════════════════════════════════════════
    //  CONSTRUCTION DES EFFETS
    // ════════════════════════════════════════════════════════

    Material MakeAdditiveMat(Color col, float alpha = 1f)
    {
        Shader sh = Shader.Find("Particles/Additive")
                 ?? Shader.Find("Legacy Shaders/Particles/Additive")
                 ?? Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        Color c = col; c.a = alpha;
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor",
            new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, alpha));
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = 3000;
        return m;
    }

    ParticleSystem MakeBasePS(string n)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        ParticleSystem ps    = go.AddComponent<ParticleSystem>();
        var main             = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    void BuildParticleSystems()
    {
        psGlitch = MakeBasePS("FX_Glitch");
        var mg = psGlitch.main;
        mg.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
        mg.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 4f);
        mg.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.2f);
        mg.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.8f, 0.1f, 1f, 1f), new Color(0.3f, 0.9f, 1f, 1f));
        mg.maxParticles    = 150;
        mg.gravityModifier = 0.4f;
        var eg = psGlitch.emission;
        eg.rateOverTime = 0f;
        eg.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 70),
            new ParticleSystem.Burst(0.1f, 40)
        });
        var sg = psGlitch.shape;
        sg.enabled = true; sg.shapeType = ParticleSystemShapeType.Sphere; sg.radius = 0.4f;
        var colg = psGlitch.colorOverLifetime; colg.enabled = true;
        Gradient gg = new Gradient();
        gg.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.6f, 1f), 0f),
                new GradientColorKey(new Color(0.4f, 0.1f, 0.8f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colg.color = new ParticleSystem.MinMaxGradient(gg);
        psGlitch.GetComponent<ParticleSystemRenderer>().material = MakeAdditiveMat(new Color(0.9f, 0.3f, 1f));
        psGlitch.gameObject.SetActive(false);

        psSparks = MakeBasePS("FX_Sparks");
        var ms = psSparks.main;
        ms.loop = true;
        ms.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
        ms.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 5f);
        ms.startSize       = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        ms.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 1f, 1f), new Color(0.7f, 0.1f, 1f, 1f));
        ms.maxParticles    = 200;
        ms.gravityModifier = 0.08f;
        var es = psSparks.emission; es.rateOverTime = 45f;
        var ss = psSparks.shape;
        ss.enabled = true; ss.shapeType = ParticleSystemShapeType.Box; ss.scale = new Vector3(0.6f, 1.4f, 0.6f);
        var cols = psSparks.colorOverLifetime; cols.enabled = true;
        Gradient gs = new Gradient();
        gs.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.8f, 0.2f, 1f), 0.4f),
                new GradientColorKey(new Color(0.3f, 0f, 0.6f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        cols.color = new ParticleSystem.MinMaxGradient(gs);
        var rs = psSparks.GetComponent<ParticleSystemRenderer>();
        rs.renderMode = ParticleSystemRenderMode.Stretch;
        rs.velocityScale = 0.15f; rs.lengthScale = 3f;
        rs.material = MakeAdditiveMat(new Color(0.9f, 0.5f, 1f));
        psSparks.gameObject.SetActive(false);

        psSmoke = MakeBasePS("FX_Smoke");
        var mf = psSmoke.main;
        mf.loop = true;
        mf.startLifetime   = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
        mf.startSpeed      = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        mf.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        mf.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        mf.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.5f, 0.1f, 0.7f, 0.5f), new Color(0.2f, 0f, 0.4f, 0.25f));
        mf.maxParticles    = 30;
        mf.gravityModifier = -0.06f;
        var ef = psSmoke.emission; ef.rateOverTime = 8f;
        var sf = psSmoke.shape;
        sf.enabled = true; sf.shapeType = ParticleSystemShapeType.Circle; sf.radius = 0.35f;
        psSmoke.GetComponent<ParticleSystemRenderer>().material = MakeAdditiveMat(new Color(0.5f, 0.1f, 0.7f), 0.3f);
        psSmoke.gameObject.SetActive(false);
    }

    void BuildShockRings()
    {
        shockRings = new LineRenderer[RING_COUNT];
        for (int i = 0; i < RING_COUNT; i++)
        {
            GameObject go = new GameObject("FX_ShockRing_" + i);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            LineRenderer lr  = go.AddComponent<LineRenderer>();
            lr.positionCount = RING_SEGMENTS + 1;
            lr.useWorldSpace = true;
            lr.loop          = false;
            lr.alignment     = LineAlignment.View;
            lr.startWidth    = 0.08f;
            lr.endWidth      = 0f;
            lr.material      = MakeAdditiveMat(shockwaveColor);
            lr.enabled       = false;
            shockRings[i]    = lr;
        }
    }

    void BuildHaloGlow()
    {
        haloGlow = new GameObject("FX_HaloGlow");
        haloGlow.transform.SetParent(transform);
        haloGlow.transform.localPosition = Vector3.up * 0.05f;
        haloGlow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LineRenderer lr = haloGlow.AddComponent<LineRenderer>();
        lr.positionCount = 33; lr.loop = false; lr.useWorldSpace = false;
        lr.alignment = LineAlignment.TransformZ;
        lr.startWidth = 0.12f; lr.endWidth = 0.12f;
        lr.material = MakeAdditiveMat(freezeGlowColor, 0.7f);
        for (int i = 0; i <= 32; i++)
        {
            float a = (float)i / 32f * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.75f, Mathf.Sin(a) * 0.75f, 0f));
        }
        Gradient hg = new Gradient();
        hg.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.5f, 1f), 0f),
                new GradientColorKey(freezeGlowColor, 0.5f),
                new GradientColorKey(new Color(1f, 0.5f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0.9f, 1f)
            });
        lr.colorGradient = hg;
        haloGlow.SetActive(false);
    }

    void BuildFreezeLight()
    {
        GameObject go = new GameObject("FX_FreezeLight");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 1f;
        freezeLight           = go.AddComponent<Light>();
        freezeLight.type      = LightType.Point;
        freezeLight.color     = new Color(0.7f, 0.1f, 1f);
        freezeLight.intensity = 0f;
        freezeLight.range     = 4f;
        freezeLight.shadows   = LightShadows.None;
    }

    void BuildFloatingText()
    {
        floatingText = new GameObject("FX_FloatingText");
        floatingText.transform.SetParent(transform);
        floatingText.transform.localPosition = Vector3.up * 2.2f;
        TextMesh tm      = floatingText.AddComponent<TextMesh>();
        tm.text          = "NEUTRALIZED";
        tm.fontSize      = 24;
        tm.characterSize = 0.08f;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.fontStyle     = FontStyle.Bold;
        tm.color         = new Color(0.9f, 0.4f, 1f, 0f);
        floatingText.SetActive(false);
    }

    // ════════════════════════════════════════════════════════
    //  UPDATE
    // ════════════════════════════════════════════════════════

    void Update()
    {
        if (isBusy || isFrozen) return;

        if (currentTarget != null && !currentTarget.isCompromised)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.transform.position);

            bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
            if (isVisible) anim.SetBool(walkParamName, isMoving);

            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (dist <= attackRange && !agent.pathPending)
                StartCoroutine(StealRoutine());
        }
        else
        {
            if (isVisible) anim.SetBool(walkParamName, false);
        }
    }

    // ════════════════════════════════════════════════════════
    //  FREEZE
    // ════════════════════════════════════════════════════════

    public void Freeze(float duration)
    {
        if (!isVisible)
        {
            Debug.LogWarning("[SpywareAI] Impossible de freeze un spyware invisible ! Scannez d'abord.");
            return;
        }

        if (isFrozen && freezeCoroutine != null)
            StopCoroutine(freezeCoroutine);
        freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        isFrozen        = true;
        isBusy          = false;
        agent.isStopped = true;
        agent.velocity  = Vector3.zero;

        anim.SetBool(walkParamName,  false);
        anim.SetBool(tombeParamName, true);

        // ── ★ FIX : Objectif Q3 "Stop Spyware with a spell" — une seule fois ★ ──
        if (!_questFreezeNotified)
        {
            _questFreezeNotified = true;
            QuestManager2.Instance?.OnSpywareFrozen();
        }

        StartCoroutine(ImpactEffects());
        StartCoroutine(FreezeLoopEffects(duration));

        yield return new WaitForSeconds(duration);

        StartCoroutine(RecoveryEffects());

        anim.SetBool(tombeParamName, false);
        anim.SetBool(walkParamName,  false);
        isFrozen        = false;
        agent.isStopped = false;
        currentTarget   = GetClosestTarget();
        freezeCoroutine = null;
        Debug.Log($"[SpywareAI] {gameObject.name} reprend.");
    }

    // ════════════════════════════════════════════════════════
    //  EFFETS FREEZE
    // ════════════════════════════════════════════════════════

    private IEnumerator ImpactEffects()
    {
        if (tombeSound  != null) { audioMain.clip   = tombeSound;  audioMain.Play(); }
        if (glitchSound != null) { audioGlitch.clip = glitchSound; audioGlitch.PlayDelayed(0.05f); }

        yield return StartCoroutine(MaterialFlash(0.3f));

        psGlitch.gameObject.SetActive(true);
        psGlitch.transform.position = transform.position + Vector3.up * 0.8f;
        psGlitch.Play();

        for (int i = 0; i < RING_COUNT; i++)
            StartCoroutine(ShockwaveRing(shockRings[i], i * 0.13f));

        StartCoroutine(LightFlash(1.2f, 0.35f));
        StartCoroutine(FloatText());
    }

    private IEnumerator MaterialFlash(float duration)
    {
        Color flashCol  = new Color(0.9f, 0.5f, 1f, 1f) * flashIntensity;
        Color frozenCol = new Color(0.55f, 0.15f, 0.85f, 1f);
        float t         = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.PingPong(t * 10f, 1f);
            foreach (var r in bodyRenderers)
                if (r != null) r.material.color = Color.Lerp(r.material.color, flashCol, p * 0.85f);
            yield return null;
        }
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].material.color = Color.Lerp(
                        bodyRenderers[i].material.color, frozenCol, t / 0.4f);
            yield return null;
        }
    }

    private IEnumerator ShockwaveRing(LineRenderer lr, float delay)
    {
        yield return new WaitForSeconds(delay);
        lr.enabled = true;
        float dur = 0.55f, maxRad = 2.8f, t = 0f;
        Vector3 ctr   = transform.position + Vector3.up * 0.05f;
        Vector3 fwd   = transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ratio  = t / dur;
            float radius = Mathf.Lerp(0f, maxRad, ratio);
            float alpha  = Mathf.Lerp(1f, 0f, ratio * ratio);
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(shockwaveColor, 0f),
                    new GradientColorKey(shockwaveColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(alpha, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            lr.colorGradient = g;
            lr.startWidth    = Mathf.Lerp(0.1f, 0.01f, ratio);
            for (int i = 0; i <= RING_SEGMENTS; i++)
            {
                float a = (float)i / RING_SEGMENTS * Mathf.PI * 2f;
                lr.SetPosition(i, ctr + right * Mathf.Cos(a) * radius + fwd * Mathf.Sin(a) * radius);
            }
            yield return null;
        }
        lr.enabled = false;
    }

    private IEnumerator LightFlash(float peak, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            freezeLight.intensity = Mathf.Lerp(peak, 0f, (t / duration) * (t / duration));
            yield return null;
        }
        freezeLight.intensity = 0f;
    }

    private IEnumerator FloatText()
    {
        if (floatingText == null) yield break;
        floatingText.SetActive(true);
        TextMesh tm       = floatingText.GetComponent<TextMesh>();
        Vector3  startPos = Vector3.up * 2.2f;
        Vector3  endPos   = Vector3.up * 3.4f;
        float    t = 0f, dur = 2f;
        Camera   cam = Camera.main;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ratio = t / dur;
            float alpha = ratio < 0.2f
                ? Mathf.Lerp(0f, 1f, ratio / 0.2f)
                : Mathf.Lerp(1f, 0f, (ratio - 0.2f) / 0.8f);
            floatingText.transform.localPosition = Vector3.Lerp(startPos, endPos, ratio);
            if (cam != null)
                floatingText.transform.LookAt(floatingText.transform.position + cam.transform.forward);
            float blink = Mathf.Abs(Mathf.Sin(t * 20f));
            tm.color = new Color(0.95f, 0.45f + blink * 0.55f, 1f, alpha);
            yield return null;
        }
        floatingText.SetActive(false);
    }

    private IEnumerator FreezeLoopEffects(float duration)
    {
        haloGlow.SetActive(true);
        LineRenderer haloLR = haloGlow.GetComponent<LineRenderer>();
        psSparks.gameObject.SetActive(true);
        psSparks.transform.position = transform.position + Vector3.up * 0.7f;
        psSparks.Play();
        psSmoke.gameObject.SetActive(true);
        psSmoke.transform.position = transform.position + Vector3.up * 0.1f;
        psSmoke.Play();
        float t = 0f;
        while (t < duration && isFrozen)
        {
            t += Time.deltaTime;
            float pulse = 0.75f + Mathf.Sin(t * 4.5f) * 0.25f;
            haloGlow.transform.localScale = Vector3.one * pulse;
            haloLR.startWidth     = 0.06f + Mathf.Sin(t * 9f) * 0.05f;
            haloLR.endWidth       = haloLR.startWidth;
            freezeLight.intensity = 0.25f + Mathf.Sin(t * 6f) * 0.18f;
            if (glitchSound != null && Random.value < 0.007f && !audioGlitch.isPlaying)
            {
                audioGlitch.pitch = Random.Range(0.7f, 1.5f);
                audioGlitch.Play();
            }
            yield return null;
        }
        psSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        psSmoke .Stop(true, ParticleSystemStopBehavior.StopEmitting);
        haloGlow.SetActive(false);
        freezeLight.intensity = 0f;
    }

    private IEnumerator RecoveryEffects()
    {
        psGlitch.transform.position = transform.position + Vector3.up * 0.8f;
        psGlitch.Play();
        StartCoroutine(LightFlash(0.6f, 0.3f));
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null)
                    bodyRenderers[i].material.color = Color.Lerp(
                        bodyRenderers[i].material.color, originalColors[i], t / 0.6f);
            yield return null;
        }
    }

    // ════════════════════════════════════════════════════════
    //  LOGIQUE NORMALE
    // ════════════════════════════════════════════════════════

    void UpdateDetection()
    {
        if (isBusy || isFrozen) return;
        allTargets.RemoveAll(t => t == null || t.isCompromised);
        if (currentTarget == null || currentTarget.isCompromised)
            currentTarget = GetClosestTarget();
    }

    IEnumerator StealRoutine()
    {
        isBusy          = true;
        agent.isStopped = true;
        if (isVisible) anim.SetBool(walkParamName, false);
        Vector3 dir = (currentTarget.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        yield return new WaitForSeconds(stealDelay);
        if (!isFrozen && currentTarget != null)
            currentTarget.StealData();
        currentTarget = null;
        isBusy        = false;
    }

    DataTarget GetClosestTarget()
    {
        DataTarget best = null;
        float minD = Mathf.Infinity;
        foreach (var t in allTargets)
        {
            if (t == null || t.isCompromised) continue;
            float d = Vector3.Distance(transform.position, t.transform.position);
            if (d < detectionRadius && d < minD) { minD = d; best = t; }
        }
        return best;
    }

    public void RefreshTargets()
    {
        allTargets.Clear();
        GameObject[] gos = GameObject.FindGameObjectsWithTag(targetTag);
        foreach (GameObject go in gos)
        {
            DataTarget dt = go.GetComponent<DataTarget>();
            if (dt != null) allTargets.Add(dt);
        }
    }
}