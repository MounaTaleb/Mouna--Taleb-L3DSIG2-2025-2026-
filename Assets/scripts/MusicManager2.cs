using UnityEngine;

public class MusicManager2 : MonoBehaviour
{
    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject winPanel;

    [Header("Audio — Background")]
    public AudioSource levelMusicSource;
    public AudioClip   levelMusic;

    [Header("Audio — Game Over")]
    public AudioSource gameOverSource;
    public AudioClip   gameOverMusic;

    [Header("Audio — Victoire")]
    public AudioSource winSource;
    public AudioClip   winMusic;

    // ══════════════════════════════════════════
    //  START — Lance la musique background
    // ══════════════════════════════════════════
    private void Start()
    {
        PlayBackgroundMusic();
    }

    private void PlayBackgroundMusic()
    {
        if (levelMusicSource == null || levelMusic == null) return;

        levelMusicSource.clip = levelMusic;
        levelMusicSource.loop = true;
        levelMusicSource.Play();

        Debug.Log("🎵 Background music started");
    }

    // ══════════════════════════════════════════
    //  GAME OVER
    // ══════════════════════════════════════════
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        StopLevelMusic();
        PlayClip(gameOverSource, gameOverMusic);
        Debug.Log("💀 Game Over — musique lancée");
    }

    // ══════════════════════════════════════════
    //  VICTOIRE
    // ══════════════════════════════════════════
    public void ShowWin()
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        StopLevelMusic();
        PlayClip(winSource, winMusic);
        Debug.Log("🏆 Victoire — musique lancée");
    }

    // ══════════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════════
    private void StopLevelMusic()
    {
        if (levelMusicSource != null && levelMusicSource.isPlaying)
            levelMusicSource.Stop();
    }

    private void PlayClip(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.Stop();
        source.clip = clip;
        source.loop = false;
        source.Play();
    }
}