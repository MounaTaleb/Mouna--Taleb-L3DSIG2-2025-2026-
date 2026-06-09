using UnityEngine;

public class WifiHackNode : MonoBehaviour
{
    [Header("Éléments UI")]
    public GameObject quizPanel;

    [Header("Couleurs")]
    public Color normalColor = Color.green;
    public Color hackedColor = Color.red;

    [Header("Animation (Avant piratage)")]
    public bool  animateIdle   = true;
    public float floatSpeed    = 2f;
    public float floatHeight   = 0.2f;
    public float rotationSpeed = 30f;
    private Vector3 startPos;

    [Header("Effets de Particules (Optionnels)")]
    [Tooltip("Petites particules vertes/bleues qui flottent")]
    public ParticleSystem idleParticles;
    [Tooltip("Étincelles électriques au moment du hack")]
    public ParticleSystem hackSparks;

    private bool       isHacked;
    private Renderer[] wifiRenderers;

    void Start()
    {
        startPos      = transform.position;
        wifiRenderers = GetComponentsInChildren<Renderer>();

        if (quizPanel != null) quizPanel.SetActive(false);

        SetWifiColor(normalColor);

        if (idleParticles != null) idleParticles.Play();
    }

    void Update()
    {
        if (animateIdle && !isHacked)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isHacked) ShowQuiz(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isHacked) ShowQuiz(false);
    }

    private void ShowQuiz(bool show)
    {
        if (quizPanel != null)
        {
            quizPanel.SetActive(show);
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = show;
        }
    }

    public void AnswerCorrect()
    {
        if (isHacked) return;

        isHacked = true;
        ShowQuiz(false);

        transform.position = startPos;
        SetWifiColor(hackedColor);

        if (idleParticles != null) idleParticles.Stop();
        if (hackSparks    != null) hackSparks.Play();

        // ✅ FIX : Q1 — WiFi désactivé (0 → 1/1)
        if (QuestManager2_Round2.Instance != null)
            QuestManager2_Round2.Instance.OnWifiDeactivated();

        // ✅ FIX : Q3 — Quiz WiFi validé (0 → 1/2)
        if (QuestManager2_Round2.Instance != null)
            QuestManager2_Round2.Instance.OnQuizAnsweredCorrectly();

        Debug.Log("WiFi désactivé — Q1 : 1/1 | Q3 : +1/2");
    }

    public void AnswerWrong()
    {
        ShowQuiz(false);
    }

    private void SetWifiColor(Color colorToSet)
    {
        foreach (Renderer rend in wifiRenderers)
        {
            rend.material.color = colorToSet;
            rend.material.SetColor("_BaseColor", colorToSet);
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", colorToSet * 2.5f);
        }
    }
}