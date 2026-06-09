using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RansomPanelManager : MonoBehaviour
{
    public static RansomPanelManager Instance;

    [Header("Panels")]
    public GameObject ransomPanel;
    public GameObject infoPanel;
    public GameObject gameOverPanel;

    [Header("Buttons - Ransom")]
    public Button declineButton;
    public Button acceptButton;

    [Header("Buttons - Info Panel")]
    public Button okButton;

    [Header("Audio")]
    public AudioClip  panelAppearSound;
    public AudioClip  declineSound;
    public AudioClip  acceptSound;
    public AudioClip  okSound;
    public AudioSource audioSource;

    [Header("Settings")]
    public float minimumGameTime = 60f;

    private bool _isShowing    = false;
    private bool _hasBeenShown = false;
    private bool _pendingShow  = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (ransomPanel  != null) ransomPanel .SetActive(false);
        if (infoPanel    != null) infoPanel   .SetActive(false);
        if (gameOverPanel!= null) gameOverPanel.SetActive(false);

        if (declineButton != null) declineButton.onClick.AddListener(OnDecline);
        if (acceptButton  != null) acceptButton .onClick.AddListener(OnAccept);
        if (okButton      != null) okButton     .onClick.AddListener(OnInfoOk);

        SetRansomButtons(false);
    }

    void Update()
    {
        if (_pendingShow && !_hasBeenShown && Time.timeSinceLevelLoad >= minimumGameTime)
        {
            _pendingShow = false;
            ShowRansomPanel();
        }
    }

    // ───────── TRIGGER ─────────
    public void OnFileEncrypted(string fileName)
    {
        if (_hasBeenShown) return;

        if (Time.timeSinceLevelLoad >= minimumGameTime)
            ShowRansomPanel();
        else
            _pendingShow = true;
    }

    // ───────── RANSOM PANEL ─────────
    void ShowRansomPanel()
    {
        if (_hasBeenShown) return;

        _hasBeenShown = true;
        _isShowing    = true;

        Time.timeScale = 0f;

        if (ransomPanel != null) ransomPanel.SetActive(true);

        if (panelAppearSound != null)
            audioSource.PlayOneShot(panelAppearSound);

        SetRansomButtons(true);
    }

    // ───────── DECLINE → INFO PANEL ─────────
    public void OnDecline()
    {
        if (!_isShowing) return;

        if (declineSound != null)
            audioSource.PlayOneShot(declineSound);

        ransomPanel.SetActive(false);
        infoPanel  .SetActive(true);

        SetRansomButtons(false);

        // ✅ Q3 — Refus de rançon validé (1 → 2/2 si quiz WiFi déjà fait)
        if (QuestManager2_Round2.Instance != null)
            QuestManager2_Round2.Instance.OnRansomDeclined();
    }

    // ───────── ACCEPT → GAME OVER ─────────
    public void OnAccept()
    {
        if (!_isShowing) return;

        _isShowing = false;
        SetRansomButtons(false);

        if (acceptSound != null)
            audioSource.PlayOneShot(acceptSound);

        Time.timeScale = 1f;

        StartCoroutine(ShowGameOver());
    }

    // ───────── INFO PANEL OK ─────────
    public void OnInfoOk()
    {
        if (okSound != null)
            audioSource.PlayOneShot(okSound);

        if (infoPanel != null) infoPanel.SetActive(false);

        Time.timeScale = 1f;

        _isShowing = false;
    }

    // ───────── GAME OVER ─────────
    IEnumerator ShowGameOver()
    {
        yield return new WaitForSecondsRealtime(1f);

        if (ransomPanel  != null) ransomPanel .SetActive(false);
        if (infoPanel    != null) infoPanel   .SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ───────── UTILS ─────────
    void SetRansomButtons(bool state)
    {
        if (declineButton != null) declineButton.interactable = state;
        if (acceptButton  != null) acceptButton .interactable = state;
    }

    public void ResetPanel()
    {
        _hasBeenShown = false;
        _isShowing    = false;
        _pendingShow  = false;

        if (ransomPanel  != null) ransomPanel .SetActive(false);
        if (infoPanel    != null) infoPanel   .SetActive(false);
        if (gameOverPanel!= null) gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }
}