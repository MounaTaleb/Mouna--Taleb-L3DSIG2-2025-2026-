using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VideoManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject videoPanel;
    public RawImage videoDisplay;
    public Button playButton;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public VideoClip introClip;

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    private RenderTexture renderTexture;

    void Start()
    {
        videoPanel.SetActive(false);
        playButton.onClick.AddListener(OnPlayClicked);

        renderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = renderTexture;
        videoDisplay.texture = renderTexture;

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    public void OnPlayClicked()
    {
        playButton.gameObject.SetActive(false);
        videoPanel.SetActive(true);

        videoPlayer.clip = introClip;
        videoPlayer.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void SkipVideo()
    {
        videoPlayer.Stop();
        SceneManager.LoadScene(gameSceneName);
    }

    void OnDestroy()
    {
        if (renderTexture != null)
            renderTexture.Release();
    }
}