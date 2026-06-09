using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static int score  = 0;
    public static int anti   = 0;
    public static int spybot = 0;
    public static int backup = 0;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI antiText;
    public TextMeshProUGUI spybotText;
    public TextMeshProUGUI backupText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (PlayerPrefs.GetInt("CurrentRound", 1) == 1)
        {
            score  = 0;
            anti   = 0;
            spybot = 0;
            backup = 0;
        }

        UpdateDisplays();
    }

    public void AddCoin(int value)
    {
        score += value;
        Debug.Log("SCORE = " + score);
        UpdateDisplays();
    }

    public void AddAntivirus(int value)
    {
        anti += value;
        Debug.Log("ANTI = " + anti);
        UpdateDisplays();
    }

    public bool UseAntivirus()
    {
        if (anti > 0)
        {
            anti--;
            Debug.Log("ANTI utilisé ! Reste = " + anti);
            UpdateDisplays();
            return true;
        }
        Debug.Log("Pas d'antivirus disponible !");
        return false;
    }

    public void AddSpybot(int value)
    {
        spybot += value;
        Debug.Log("SPYBOT = " + spybot);
        UpdateDisplays();
    }

    public bool UseSpybot()
    {
        if (spybot > 0)
        {
            spybot--;
            Debug.Log("SPYBOT utilisé ! Reste = " + spybot);
            UpdateDisplays();
            return true;
        }
        Debug.Log("Pas de spybot disponible !");
        return false;
    }

    public void AddBackup(int value)
    {
        backup += value;
        Debug.Log("BACKUP = " + backup);
        UpdateDisplays();
    }

    public bool UseBackup()
    {
        if (backup > 0)
        {
            backup--;
            Debug.Log("BACKUP utilisé ! Reste = " + backup);
            UpdateDisplays();
            return true;
        }
        Debug.Log("Pas de backup disponible !");
        return false;
    }

    public void ForceUpdateDisplays() => UpdateDisplays();

    void UpdateDisplays()
    {
        if (scoreText  != null) scoreText.text  = score.ToString();
        else Debug.LogWarning("scoreText est NULL dans GameManager !");

        if (antiText   != null) antiText.text   = anti.ToString();
        else Debug.LogWarning("antiText est NULL dans GameManager !");

        if (spybotText != null) spybotText.text = spybot.ToString();
        else Debug.LogWarning("spybotText est NULL dans GameManager !");

        if (backupText != null) backupText.text = backup.ToString();
        else Debug.LogWarning("backupText est NULL dans GameManager !");
    }
}