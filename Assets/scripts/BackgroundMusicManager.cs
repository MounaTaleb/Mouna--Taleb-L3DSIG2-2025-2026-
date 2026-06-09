using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager instance;
    public AudioSource musicSource;

    // Mets ici les noms des scènes où tu veux de la musique
    public string[] scenesWithMusic = { "Scene1" };

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        musicSource.loop = true;
        musicSource.Play();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldPlay = System.Array.IndexOf(scenesWithMusic, scene.name) >= 0;

        if (shouldPlay && !musicSource.isPlaying)
            musicSource.Play();
        else if (!shouldPlay)
            musicSource.Stop();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetVolume(float value)
    {
        musicSource.volume = value;
    }
}