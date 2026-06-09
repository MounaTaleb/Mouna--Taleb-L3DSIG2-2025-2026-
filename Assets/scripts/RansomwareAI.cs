using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RansomwareAI : MonoBehaviour
{
    [Header("Déplacement")]
    public float detectionRadius = 20f;
    public float attackRange     = 1.5f;

    [Header("Animation")]
    public string walkParamName   = "isWalking";
    public string attackParamName = "isAttacking";

    [Header("Attaque")]
    public int   hitsToEncrypt   = 3;
    public float timeBetweenHits = 0.4f;
    public float encryptDelay    = 0.2f;

    [Header("Dégâts Système")]
    public float damagePerHit = 7f;

    [Header("Tag des fichiers cibles")]
    public string fileTag = "RansomFile";

    [Header("Prefab fichier crypté")]
    public GameObject encryptedFilePrefab;
    public Vector3    prefabSpawnOffset = Vector3.zero;

    [Header("Couleurs VFX")]
    [ColorUsage(true, true)] public Color dangerRed   = new Color(1f,    0.05f, 0.05f, 1f);
    [ColorUsage(true, true)] public Color darkRed     = new Color(0.5f,  0f,    0f,    1f);
    [ColorUsage(true, true)] public Color neonOrange  = new Color(1f,    0.4f,  0f,    1f);
    [ColorUsage(true, true)] public Color corpseWhite = new Color(0.85f, 0.85f, 0.9f,  1f);

    [Header("Audio Alertes")]
    public AudioClip alertAttackSound;
    public AudioClip alertEncryptSound;
    public AudioClip alertCorruptSound;
    public float     alertSoundVolume = 0.4f;

    // ─── internes ────────────────────────────────────────
    private NavMeshAgent _agent;
    private Animator     _anim;
    private AudioSource  _audioSource;

    private List<GameObject>     _allTargets  = new List<GameObject>();
    private HashSet<int>         _infectedIDs = new HashSet<int>();
    private Dictionary<int, int> _hitCount    = new Dictionary<int, int>();

    private GameObject _currentTarget = null;
    private bool       _isBusy        = false;

    // VFX aura
    private GameObject     _auraRoot;
    private ParticleSystem _auraPS;
    private LineRenderer[] _auraRings;
    private float          _auraTime  = 0f;
    private const int      AURA_RINGS = 3;

    private Light _eyeLight;

    // ═════════════════════════════════════════════════════
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = attackRange * 0.8f;

        _anim = GetComponent<Animator>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;
        _audioSource.volume = alertSoundVolume;

        if (encryptedFilePrefab == null)
            Debug.LogError("[RansomwareAI] ⚠ encryptedFilePrefab non assigné dans l'Inspector !");

        foreach (var go in GameObject.FindGameObjectsWithTag(fileTag))
            _allTargets.Add(go);

        Debug.Log($"[RansomwareAI] {_allTargets.Count} cible(s) avec tag '{fileTag}'.");

        BuildAuraVFX();
        BuildEyeLight();
    }

    // ═════════════════════════════════════════════════════
    void Update()
    {
        _allTargets.RemoveAll(go => go == null);
        UpdateAuraVFX();

        if (_isBusy) return;

        if (_currentTarget == null || IsInfected(_currentTarget))
            _currentTarget = GetClosest();

        if (_currentTarget == null)
        {
            _agent.isStopped = true;
            SetWalk(false);
            return;
        }

        _agent.isStopped = false;
        _agent.SetDestination(_currentTarget.transform.position);
        SetWalk(_agent.velocity.sqrMagnitude > 0.01f);

        if (DistXZ(transform.position, _currentTarget.transform.position) <= attackRange
            && !_agent.pathPending)
        {
            StartCoroutine(AttackLoop());
        }
    }

    // ═════════════════════════════════════════════════════
    //  ATTAQUE + CRYPTAGE
    // ═════════════════════════════════════════════════════
    IEnumerator AttackLoop()
    {
        _isBusy          = true;
        _agent.isStopped = true;
        _agent.velocity  = Vector3.zero;
        SetWalk(false);
        SetAttack(true);

        GameObject victim = _currentTarget;

        if (victim == null || IsInfected(victim))
        {
            _isBusy = false;
            SetAttack(false);
            yield break;
        }

        string victimName = victim.name;

        // Alerte attaque
        ShowAlert($"[THREAT] RANSOMWARE ATTACKING {victimName.ToUpper()}", dangerRed, 3f);
        PlayAlertSound(alertAttackSound);

        var fx = victim.GetComponent<RansomwareFileEffect>();
        if (fx == null) fx = victim.AddComponent<RansomwareFileEffect>();
        fx.PlayAttackAura(dangerRed, neonOrange);

        // ── Frappe jusqu'au seuil ────────────────────────
        int hitNumber = 0;
        while (victim != null && !HasEnoughHits(victim))
        {
            hitNumber++;
            FaceTarget(victim);
            RegisterHit(victim);

            // ── DÉGÂTS SUR LA SANTÉ SYSTÈME ──────────────
            if (SystemHealth.Instance != null)
                SystemHealth.Instance.TakeDamage(damagePerHit);
            else
                Debug.LogWarning("[RansomwareAI] SystemHealth.Instance est null !");
            // ─────────────────────────────────────────────

            ShowAlert($"[HIT {hitNumber}/{hitsToEncrypt}] FILE CORRUPTION {hitNumber * 33}%", neonOrange, 2f);
            PlayAlertSound(alertCorruptSound);

            yield return StartCoroutine(HitShake());
            yield return new WaitForSeconds(timeBetweenHits);
        }

        yield return new WaitForSeconds(encryptDelay);

        if (victim != null)
        {
            MarkInfected(victim);

            // Alertes cryptage
            ShowAlert($"[CRITICAL] {victimName.ToUpper()} ENCRYPTED — AES-256", dangerRed, 4f);
            ShowAlert("[SYS] RECOVERY KEY DELETED", darkRed, 3f);
            PlayAlertSound(alertEncryptSound);

            fx = victim.GetComponent<RansomwareFileEffect>();
            if (fx == null) fx = victim.AddComponent<RansomwareFileEffect>();

            fx.TriggerEncryption(
                dangerRed, darkRed, neonOrange, corpseWhite,
                encryptedFilePrefab, prefabSpawnOffset
            );

            // ── DÉCLENCHE LE PANEL DE RANÇON ─────────────
            if (RansomPanelManager.Instance != null)
                RansomPanelManager.Instance.OnFileEncrypted(victimName);
            else
                Debug.LogWarning("[RansomwareAI] RansomPanelManager.Instance est null !");
        }

        _currentTarget   = null;
        _agent.isStopped = false;
        _isBusy          = false;
        SetAttack(false);
    }

    // ─── Alertes ─────────────────────────────────────────
    private void ShowAlert(string message, Color color, float duration)
    {
        if (CyberAlertRound2.Instance != null)
            CyberAlertRound2.Instance.Enqueue(message, color, duration);
        else
            Debug.LogWarning($"[RansomwareAI] CyberAlertRound2.Instance null — {message}");
    }

    private void PlayAlertSound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip, alertSoundVolume);
    }

    // ─── État interne ─────────────────────────────────────
    private bool IsInfected(GameObject go)
        => go == null || _infectedIDs.Contains(go.GetInstanceID());

    private void MarkInfected(GameObject go)
    {
        if (go != null) _infectedIDs.Add(go.GetInstanceID());
    }

    private void RegisterHit(GameObject go)
    {
        int id = go.GetInstanceID();
        if (!_hitCount.ContainsKey(id)) _hitCount[id] = 0;
        _hitCount[id]++;
    }

    private bool HasEnoughHits(GameObject go)
    {
        int id = go.GetInstanceID();
        return _hitCount.ContainsKey(id) && _hitCount[id] >= hitsToEncrypt;
    }

    private IEnumerator HitShake()
    {
        Vector3 basePos = transform.position;
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float shake = Mathf.Lerp(0.05f, 0f, elapsed / 0.1f);
            transform.position = basePos + new Vector3(
                Random.Range(-shake, shake), 0f,
                Random.Range(-shake, shake));
            yield return null;
        }
        transform.position = basePos;
    }

    // ═════════════════════════════════════════════════════
    //  VFX AURA
    // ═════════════════════════════════════════════════════
    private void BuildAuraVFX()
    {
        _auraRoot = new GameObject("RansomwareAura");
        _auraRoot.transform.SetParent(transform);
        _auraRoot.transform.localPosition = Vector3.zero;

        _auraRings = new LineRenderer[AURA_RINGS];
        for (int i = 0; i < AURA_RINGS; i++)
        {
            var go = new GameObject("AuraRing_" + i);
            go.transform.SetParent(_auraRoot.transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount     = 33;
            lr.useWorldSpace     = false;
            lr.loop              = true;
            lr.alignment         = LineAlignment.View;
            lr.startWidth        = 0.05f;
            lr.endWidth          = 0.05f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.material          = MakeMat(dangerRed, 0.9f);
            SetRingPositions(lr, 0.5f + i * 0.35f);
            _auraRings[i] = lr;
        }

        var psGo = new GameObject("AuraPS");
        psGo.transform.SetParent(_auraRoot.transform);
        psGo.transform.localPosition = Vector3.zero;

        _auraPS = psGo.AddComponent<ParticleSystem>();
        var main = _auraPS.main;
        main.loop            = true;
        main.playOnAwake     = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(dangerRed, neonOrange);
        main.maxParticles    = 60;

        var emission = _auraPS.emission;
        emission.rateOverTime = 25f;

        var shape = _auraPS.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.5f;

        var col = _auraPS.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(neonOrange, 0f),
                new GradientColorKey(dangerRed,  0.4f),
                new GradientColorKey(darkRed,    1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.7f, 0.6f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var trail = _auraPS.trails;
        trail.enabled  = true;
        trail.lifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);

        var ren = _auraPS.GetComponent<ParticleSystemRenderer>();
        ren.renderMode = ParticleSystemRenderMode.Billboard;
        ren.material   = MakeMat(dangerRed, 0.95f);
        _auraPS.Play();
    }

    private void BuildEyeLight()
    {
        var go = new GameObject("EyeLight");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 1.2f, 0.3f);

        _eyeLight           = go.AddComponent<Light>();
        _eyeLight.type      = LightType.Point;
        _eyeLight.color     = dangerRed;
        _eyeLight.intensity = 2f;
        _eyeLight.range     = 2f;
        _eyeLight.shadows   = LightShadows.None;
    }

    private void UpdateAuraVFX()
    {
        if (_auraRings == null) return;
        _auraTime += Time.deltaTime;

        for (int i = 0; i < AURA_RINGS; i++)
        {
            if (_auraRings[i] == null) continue;
            float phase = _auraTime * 4f + i * Mathf.PI * 0.7f;
            float pulse = 0.85f + Mathf.Sin(phase) * 0.15f;
            float alpha = 0.4f + Mathf.Sin(phase) * 0.5f;
            SetRingPositions(_auraRings[i], (0.5f + i * 0.35f) * pulse);

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(neonOrange, 0f),
                    new GradientColorKey(dangerRed,  0.5f),
                    new GradientColorKey(darkRed,    1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(alpha,        0f),
                    new GradientAlphaKey(alpha * 0.8f, 0.8f),
                    new GradientAlphaKey(0f,           1f)
                }
            );
            _auraRings[i].colorGradient = g;
        }

        if (_eyeLight != null)
        {
            _eyeLight.intensity = 1.5f + Mathf.Sin(_auraTime * 5f) * 1f;
            _eyeLight.color = Color.Lerp(darkRed, dangerRed,
                (Mathf.Sin(_auraTime * 3f) + 1f) * 0.5f);
        }
    }

    // ═════════════════════════════════════════════════════
    //  UTILS
    // ═════════════════════════════════════════════════════
    private void SetWalk(bool w)   { if (_anim) _anim.SetBool(walkParamName,   w); }
    private void SetAttack(bool a) { if (_anim) _anim.SetBool(attackParamName, a); }

    private void FaceTarget(GameObject t)
    {
        if (t == null) return;
        Vector3 d = t.transform.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
    }

    private GameObject GetClosest()
    {
        GameObject best = null;
        float minD = Mathf.Infinity;
        foreach (var go in _allTargets)
        {
            if (go == null || IsInfected(go)) continue;
            float d = DistXZ(transform.position, go.transform.position);
            if (d < detectionRadius && d < minD) { minD = d; best = go; }
        }
        return best;
    }

    private float DistXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void SetRingPositions(LineRenderer lr, float radius)
    {
        int count = lr.positionCount;
        for (int s = 0; s < count; s++)
        {
            float angle = (float)s / (count - 1) * Mathf.PI * 2f;
            lr.SetPosition(s, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private Material MakeMat(Color col, float alpha = 1f)
    {
        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Unlit/Color");
        Material m = new Material(sh);
        Color c = col; c.a = alpha; m.color = c;
        return m;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (_currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
        }
    }
}