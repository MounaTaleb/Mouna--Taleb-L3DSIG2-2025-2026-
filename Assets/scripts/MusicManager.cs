using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Panels — Joueur connecté")]
    public GameObject gameOverPanel;
    public GameObject winPanel;

    [Header("Panels — Guest")]
    public GameObject gameOverPanelGuest;
    public GameObject winPanelGuest;

    [Header("Audio — Background")]
    public AudioSource levelMusicSource;
    public AudioClip levelMusic;

    [Header("Audio — Game Over")]
    public AudioSource gameOverSource;
    public AudioClip gameOverMusic;

    [Header("Audio — Victoire")]
    public AudioSource winSource;
    public AudioClip winMusic;

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
    //  GAME OVER — Joueur connecté
    // ══════════════════════════════════════════
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        TriggerGameOver();
    }

    // ══════════════════════════════════════════
    //  GAME OVER — Guest
    // ══════════════════════════════════════════
    public void ShowGameOverGuest()
    {
        if (gameOverPanelGuest != null)
            gameOverPanelGuest.SetActive(true);

        TriggerGameOver();
    }

    // ══════════════════════════════════════════
    //  VICTOIRE — Joueur connecté
    // ══════════════════════════════════════════
    public void ShowWin()
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        TriggerWin();
    }

    // ══════════════════════════════════════════
    //  VICTOIRE — Guest
    // ══════════════════════════════════════════
    public void ShowWinGuest()
    {
        if (winPanelGuest != null)
            winPanelGuest.SetActive(true);

        TriggerWin();
    }

    // ══════════════════════════════════════════
    //  LOGIQUE PARTAGÉE (même musique dans les 2 cas)
    // ══════════════════════════════════════════
    private void TriggerGameOver()
    {
        StopLevelMusic();
        PlayClip(gameOverSource, gameOverMusic);
        Debug.Log("💀 Game Over — musique lancée");
    }

    private void TriggerWin()
    {
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