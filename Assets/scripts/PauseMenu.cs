using System.Collections;
using UnityEngine;
using Cinemachine;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    public GameObject pausePanel;
    private bool isPaused = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);

        StartCoroutine(SnapCameraOnSceneLoad());
    }

    private IEnumerator SnapCameraOnSceneLoad()
    {
        yield return null;

        CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain == null) yield break;

        float originalTime                         = brain.m_DefaultBlend.m_Time;
        CinemachineBlendDefinition.Style origStyle = brain.m_DefaultBlend.m_Style;

        brain.m_DefaultBlend.m_Time  = 0f;
        brain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
        brain.m_IgnoreTimeScale      = true;

        yield return null;

        brain.m_DefaultBlend.m_Time  = originalTime;
        brain.m_DefaultBlend.m_Style = origStyle;
        brain.m_IgnoreTimeScale      = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void QuitGame()
    {
        // ✅ Reset complet avant de quitter
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);

        // ✅ Laisser 1 frame pour appliquer timeScale avant de quitter
        StartCoroutine(QuitAfterFrame());
    }

    private IEnumerator QuitAfterFrame()
    {
        yield return null; // attendre 1 frame

        Debug.Log("👋 Quit appelé");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_ANDROID || UNITY_IOS
            Application.Quit();
        #else
            Application.Quit();
        #endif
    }
}