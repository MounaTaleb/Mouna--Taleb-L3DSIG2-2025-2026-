using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class CyberTerminalManager : MonoBehaviour
{
    [Header("=== TERMINAL PANEL ===")]
    public GameObject terminalPanel;
    public GameObject openButton;

    [Header("=== TITLE ===")]
    public TMP_Text titleText;

    [Header("=== CLOSE BUTTON ===")]
    public Button closeButton;

    [Header("=== OUTPUT ===")]
    public TMP_Text       outputText;
    public ScrollRect     scrollRect;
    public RectTransform  contentRect;

    [Header("=== INPUT ===")]
    public TMP_InputField inputField;
    public Button         submitButton;

    [Header("=== TIMING ===")]
    public float lineDelay = 0.035f;

    [Header("=== AUDIO ===")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    [Header("=== AUTO OPEN ===")]
    public bool autoOpen = true;

    [Header("=== ROUND TRANSITION ===")]
    public float victoryCloseDelay = 2.0f;

    private int  _panel     = 0;
    private bool _busy      = false;
    private int  _hintCount = 0;

    const string GR = "#00FF41";
    const string DK = "#008F11";
    const string AM = "#FFB000";
    const string RE = "#FF0033";
    const string GY = "#666666";
    const string WH = "#E0E0E0";

    string C(string txt, string hex) => $"<color={hex}>{txt}</color>";
    string N(string raw) => Regex.Replace(raw.ToLower().Trim(), @"\s+", " ");
    string VIRUS(string label = "SPYWARE") => C(label, RE);

    void NotifyCMDStep()
    {
        if (QuestManager2.Instance != null)
            QuestManager2.Instance.OnSpywareEliminatedViaCMD();
        else
            Debug.LogWarning("[CyberTerminalManager] QuestManager2.Instance est NULL !");
    }

    void Awake()
    {
        contentRect.anchorMin        = new Vector2(0f, 1f);
        contentRect.anchorMax        = new Vector2(1f, 1f);
        contentRect.pivot            = new Vector2(0.5f, 1f);
        contentRect.offsetMin        = new Vector2(0f, contentRect.offsetMin.y);
        contentRect.offsetMax        = new Vector2(0f, 0f);
        contentRect.anchoredPosition = new Vector2(0f, 0f);
    }

    void Start()
    {
        if (titleText != null) titleText.gameObject.SetActive(false);

        outputText.color              = Color.white;
        outputText.richText           = true;
        outputText.enableWordWrapping = true;
        outputText.overflowMode       = TextOverflowModes.Overflow;

        var vlg = contentRect.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;

        var csf = contentRect.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.vertical     = true;
        scrollRect.horizontal   = false;

        submitButton.onClick.AddListener(Submit);
        inputField.onSubmit.AddListener(_ => Submit());

        if (closeButton == null)
            foreach (Button btn in FindObjectsOfType<Button>(true))
                if (btn.gameObject.name == "CloseButton") { closeButton = btn; break; }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTerminal);
        }

        if (autoOpen) OpenTerminal();
    }

    public void OpenTerminal()
    {
        StopAllCoroutines();
        outputText.text = "";
        _panel = 0; _busy = false; _hintCount = 0;
        terminalPanel.SetActive(true);
        if (openButton  != null) openButton.SetActive(false);
        if (titleText   != null) titleText.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        StartCoroutine(PrintLines(Panel0_Intro()));
        inputField.ActivateInputField();
    }

    public void CloseTerminal()
    {
        StopAllCoroutines();
        _busy = false;
        terminalPanel.SetActive(false);
        if (openButton  != null) openButton.SetActive(true);
        if (titleText   != null) titleText.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
    }

    // ✅ Victoire : on bloque le joueur pour éviter tout déplacement intempestif
    IEnumerator VictoryThenClose()
    {
        // Bloque le joueur (désactive CharacterController + contrôles)
        GameObject player = GameObject.FindWithTag("Player");
        CharacterController cc = null;
        if (player != null)
        {
            cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            SetPlayerControls(player, false);
        }

        yield return StartCoroutine(PrintLines(Panel9_Victory()));
        yield return new WaitForSeconds(victoryCloseDelay);

        CloseTerminal();

        // Réactive (RoundManager refera de toute façon une réactivation, mais pour cohérence)
        if (player != null && cc != null)
        {
            cc.enabled = true;
            SetPlayerControls(player, true);
        }

        NotifyCMDStep();
    }

    void Submit()
    {
        if (_busy) return;
        string raw = inputField.text.Trim();
        if (string.IsNullOrEmpty(raw)) return;
        inputField.text = "";
        inputField.ActivateInputField();

        switch (_panel)
        {
            case 0: HandleP0(raw); break;
            case 1: HandleP1(raw); break;
            case 2: HandleP2(raw); break;
            case 3: HandleP3(raw); break;
            case 4: HandleP4(raw); break;
            case 9:
                Append(C("  [!] TERMINAL VERROUILLÉ - ACCÈS REFUSÉ", RE));
                PlayWrongSound();
                break;
        }
    }

    void PlayCorrectSound() { if (audioSource != null && correctSound != null) audioSource.PlayOneShot(correctSound); }
    void PlayWrongSound()   { if (audioSource != null && wrongSound   != null) audioSource.PlayOneShot(wrongSound); }

    string[] Panel0_Intro() => new[]
    {
        C(@"███████╗██████╗ ██╗   ██╗██╗    ██╗ █████╗ ██████╗ ███████╗", RE),
        C(@"██╔════╝██╔══██╗╚██╗ ██╔╝██║    ██║██╔══██╗██╔══██╗██╔════╝", RE),
        C(@"███████╗██████╔╝ ╚████╔╝ ██║ █╗ ██║███████║██████╔╝█████╗  ", RE),
        C(@"╚════██║██╔═══╝   ╚██╔╝  ██║███╗██║██╔══██║██╔══██╗██╔══╝  ", RE),
        C(@"███████║██║        ██║   ╚███╔███╔╝██║  ██║██║  ██║███████╗", RE),
        C(@"╚══════╝╚═╝        ╚═╝    ╚══╝╚══╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝", RE) + C(" v2.4", GR),
        "",
        C("+-------------------------------------------------------+", GY),
        C("|", GY) + C(" [!] ", RE) + "Host: " + C("192.168.1.77", WH) +
            "  |  " + C("CRITICAL", RE) + " : " + VIRUS("active spyware") + C("   |", GY),
        C("|", GY) + C(" [*] ", GR) + "User: " + C("root", GR) +
            "          |  Time: " + C("04:47 UTC", WH) + C("               |", GY),
        C("+-------------------------------------------------------+", GY),
        "",
        C(" > ", GR) + "Identifie, stoppe et neutralise le " + VIRUS() + ".",
        C(" > ", GR) + "Tape " + C("indice", AM) + " à tout moment si tu bloques.",
        "",
        "   :: Tape " + C("scan", GR) + " pour démarrer le diagnostic",
    };

    void HandleP0(string raw)
    {
        if (N(raw) == "scan" || N(raw) == "start")
        {
            _panel = 1; _hintCount = 0;
            PlayCorrectSound();
            NotifyCMDStep();
            StartCoroutine(PrintLines(Panel1_PsAux()));
        }
        else WrongCmd(raw);
    }

    string[] Panel1_PsAux() => new[]
    {
        "",
        C("--------------------------------------------------", DK),
        C(" :: [1/4] PROCESS HUNTING ::", GR),
        C("--------------------------------------------------", DK),
        " Le spyware tourne en cache et consomme le CPU.",
        " On doit lister tous les processus triés par CPU.",
        "",
        C(" [?] ", AM) + "Complète la commande :",
        "",
        C("     $ ", GY) + "ps aux " + C("--sort=", WH) + C("___?___", RE),
        "",
        C("     > Indice 1 : ", GY) + "Le signe '-' = ordre décroissant",
        C("     > Indice 2 : ", GY) + "La colonne s'appelle cpu  ->  " + C("-%cpu", WH),
        "",
        C(" [>] Tape seulement la partie manquante :", AM),
    };

    void HandleP1(string raw)
    {
        if (N(raw) == "indice") { ShowHintP1(); return; }
        string v = N(raw);
        if (v == "--sort=-%cpu" || v == "-%cpu" || v == "%cpu" ||
            v == "ps aux --sort=-%cpu" || v.Contains("--sort=-%cpu"))
        {
            _panel = 2; _hintCount = 0;
            PlayCorrectSound();
            NotifyCMDStep();
            StartCoroutine(PrintThenGo(Panel1_Result(), Panel2_Top));
        }
        else
        {
            PlayWrongSound();
            _hintCount++;
            if (_hintCount >= 2) ShowHintP1();
            else Append(C(" [!] Syntax Error. Rappel : ps aux --sort=", RE) + C("___?___", AM));
        }
    }

    void ShowHintP1() => Append(
        C(" [*] SOLUTION : ", AM) + "Commande = " + C("ps aux --sort=-%cpu", GR) + "\n" +
        C("                Tu peux taper juste : ", GY) + C("--sort=-%cpu", WH) + " ou " + C("-%cpu", WH)
    );

    string[] Panel1_Result() => new[]
    {
        "",
        C("root@soc:~# ", GR) + "ps aux --sort=-%cpu",
        "",
        C("USER      PID   %CPU  COMMAND", GY),
        C("----------------------------------------", GY),
        C("spyproc  7331  ", RE) + C("98.7  /tmp/.hidden/spyware --exfil", RE),
        C("root     1001   0.8  systemd", GY),
        C("syslog    893   0.3  rsyslogd", GY),
        "",
        C(" [!] TARGET ACQUIRED : PID 7331 (98.7% CPU)", RE),
    };

    string[] Panel2_Top() => new[]
    {
        "",
        C("--------------------------------------------------", DK),
        C(" :: [2/4] SURVEILLANCE TEMPS REEL ::", GR),
        C("--------------------------------------------------", DK),
        " Avant de stopper le spyware, vérifie s'il",
        " a créé des processus enfants (keylogger, etc.).",
        "",
        C(" [?] ", AM) + "Quelle commande surveille les processus EN DIRECT ?",
        "",
        C("  [A] ", WH) + "ls -la /proc    " + C("// liste des fichiers", GY),
        C("  [B] ", WH) + "top             " + C("// monitoring interactif", GR),
        C("  [C] ", WH) + "cat /var/log    " + C("// lecture de logs", GY),
        C("  [D] ", WH) + "ping 8.8.8.8    " + C("// test réseau", GY),
        "",
        "     Tape la lettre " + C("A", WH) + ", " + C("B", WH) +
        ", " + C("C", WH) + " ou " + C("D", WH) + ".",
    };

    void HandleP2(string raw)
    {
        if (N(raw) == "indice") { PlayWrongSound(); Append(C(" [*] Indice : C'est un mot de 3 lettres (option B).", AM)); return; }
        string v = N(raw);
        if (v == "b" || v == "top" || v.StartsWith("top"))
        {
            _panel = 3; _hintCount = 0;
            PlayCorrectSound();
            NotifyCMDStep();
            StartCoroutine(PrintThenGo(Panel2_Result(), Panel3_Crontab));
        }
        else
        {
            PlayWrongSound();
            Append(C(" [!] Erreur. Laquelle surveille EN TEMPS RÉEL ?", RE));
        }
    }

    string[] Panel2_Result() => new[]
    {
        "",
        C("root@soc:~# ", GR) + "top",
        "",
        C("top - 04:48:12 up 14 days,  3:22,  1 user", GY),
        C("%Cpu : ", GY) + C("98.7 us", RE) + C("  // usage anormal detecte", AM),
        C("  PID  USER     %CPU  COMMAND", GY),
        C(" 7331  spyproc  98.7  spyware --exfil   ", RE) + C("[PARENT]", AM),
        C(" 7332  spyproc   0.2  spyware --persist ", RE) + C("[ENFANT]", AM),
        C(" 7333  spyproc   0.1  spyware --keylog  ", RE) + C("[ENFANT]", AM),
        "",
        C(" [+] Analyse : 3 processus hostiles actifs.", GR),
    };

    string[] Panel3_Crontab() => new[]
    {
        "",
        C("--------------------------------------------------", DK),
        C(" :: [3/4] PERSISTANCE ::", GR),
        C("--------------------------------------------------", DK),
        " Le processus --persist indique que le spyware",
        " se relance tout seul au redémarrage via cron.",
        "",
        C(" [?] ", AM) + "Complète la commande pour voir les tâches planifiées :",
        "",
        C("     $ ", GY) + "crontab " + C("___?___", RE),
        "",
        C("     > Indice 1 : ", GY) + "L'option commence par un tiret  -",
        C("     > Indice 2 : ", GY) + "'List' commence par la lettre " + C("L", WH),
        "",
        C(" [>] Tape seulement l'option manquante :", AM),
    };

    void HandleP3(string raw)
    {
        if (N(raw) == "indice") { ShowHintP3(); return; }
        string v = N(raw);
        if (v == "-l" || v == "crontab -l" || v == "-lu" || v == "crontab -lu root")
        {
            _panel = 4; _hintCount = 0;
            PlayCorrectSound();
            NotifyCMDStep();
            StartCoroutine(PrintThenGo(Panel3_Result(), Panel4_Systemctl));
        }
        else
        {
            PlayWrongSound();
            _hintCount++;
            if (_hintCount >= 2) ShowHintP3();
            else Append(C(" [!] Syntax Error. Rappel : crontab ", RE) + C("___?___", AM));
        }
    }

    void ShowHintP3()
    {
        PlayWrongSound();
        Append(
            C(" [*] SOLUTION : ", AM) + "Commande = " + C("crontab -l", GR) + "\n" +
            C("                Tu peux taper juste : ", GY) + C("-l", WH)
        );
    }

    string[] Panel3_Result() => new[]
    {
        "",
        C("root@soc:~# ", GR) + "crontab -l",
        "",
        C("@reboot   /tmp/.hidden/spyware --silent &", RE),
        C("*/5 * * * * /tmp/.hidden/spyware --exfil", RE),
        "",
        C(" [!] TRIGGER DE PERSISTANCE TROUVÉ :", RE),
        C("     > @reboot  : relance automatique", AM),
        C("     > */5 min  : exfiltration récurrente", AM),
    };

    string[] Panel4_Systemctl() => new[]
    {
        "",
        C("--------------------------------------------------", DK),
        C(" :: [4/4] NEUTRALISATION ::", GR),
        C("--------------------------------------------------", DK),
        " Le spyware tourne comme service système en arrière-plan.",
        " La commande pour stopper un service est :",
        "",
        C("     $ ", GY) + C("systemctl stop", WH) + " " + C("<nom_du_service>", RE),
        "",
        C(" [?] ", AM) + "Quel est le nom du service malveillant ?",
        "",
        C("  [A] ", WH) + "network_manager",
        C("  [B] ", WH) + "suspicious_service",
        C("  [C] ", WH) + "ssh_daemon",
        C("  [D] ", WH) + "cron",
        "",
        "     Tape la lettre correspondante.",
    };

    void HandleP4(string raw)
    {
        if (N(raw) == "indice")
        {
            PlayWrongSound();
            Append(C(" [*] Indice : Quel service semble suspect ? (Option B)", AM));
            return;
        }

        string v = N(raw);
        bool ok = v == "b" || v.Contains("suspicious_service") ||
                  v == "systemctl stop suspicious_service" ||
                  v == "sudo systemctl stop suspicious_service";

        if (ok)
        {
            _panel = 9;
            PlayCorrectSound();
            StartCoroutine(PrintThenVictory(Panel4_Result()));
        }
        else
        {
            PlayWrongSound();
            Append(C(" [!] Échec. Identifie le service anormal.", RE));
        }
    }

    string[] Panel4_Result() => new[]
    {
        "",
        C("root@soc:~# ", GR) + "systemctl stop suspicious_service",
        "",
        C(" [ OK ] Stopping suspicious_service ...", GR),
        C(" [ OK ] Sending SIGKILL to PID 7331...", GR),
        C(" [ OK ] Sending SIGKILL to PID 7332...", GR),
        C(" [ OK ] Sending SIGKILL to PID 7333...", GR),
        "",
        C(" [+] Processus neutralisés.", GR),
        C(" [+] Trafic réseau hostile interrompu.", GR),
    };

    IEnumerator PrintThenVictory(string[] resultLines)
    {
        yield return StartCoroutine(PrintLines(resultLines));
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(VictoryThenClose());
    }

    string[] Panel9_Victory() => new[]
    {
        "",
        C("==================================================", DK),
        C(" SYSTEM SECURITY LOG -- THREAT NEUTRALIZED        ", GR),
        C("==================================================", DK),
        C(" > ACTION: scan            ->  Diagnostic       ", GY) + C("[ OK ]", GR),
        C(" > ACTION: ps aux          ->  Target ID        ", GY) + C("[ OK ]", GR),
        C(" > ACTION: top             ->  Process Tree     ", GY) + C("[ OK ]", GR),
        C(" > ACTION: crontab         ->  Cron Config      ", GY) + C("[ OK ]", GR),
        C(" > ACTION: systemctl       ->  Service Kill     ", GY) + C("[ OK ]", GR),
        C("--------------------------------------------------", DK),
        C(" ACCESS LEVEL : ANALYST SOC Lvl.2                 ", AM),
        C(" STATUS       : SYSTEM SECURED                    ", WH),
        C("==================================================", DK),
        "",
        C("  >> ROUND 2 INCOMING — PREPARE FOR RANSOMWARE <<", AM),
        C("     [ CONNEXION TERMINÉE ]", GY),
    };

    IEnumerator PrintThenGo(string[] lines, System.Func<string[]> next)
    {
        yield return StartCoroutine(PrintLines(lines));
        yield return new WaitForSeconds(0.6f);
        if (next != null) yield return StartCoroutine(PrintLines(next()));
    }

    IEnumerator PrintLines(string[] lines)
    {
        _busy = true;
        foreach (var line in lines)
        {
            outputText.text += line + "\n";
            yield return null;
            yield return null;
            ScrollToBottom();
            yield return new WaitForSeconds(lineDelay);
        }
        _busy = false;
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    void WrongCmd(string raw)
    {
        PlayWrongSound();
        StartCoroutine(AppendAndScroll(
            C("root@soc:~# ", GR) + raw + "\n" +
            C(" bash: command not found. Tape 'indice' pour de l'aide.", RE)
        ));
    }

    void Append(string line) => StartCoroutine(AppendAndScroll(line));

    IEnumerator AppendAndScroll(string line)
    {
        outputText.text += line + "\n";
        yield return null;
        yield return null;
        ScrollToBottom();
    }

    // Helper pour désactiver/réactiver les contrôles du joueur
    private void SetPlayerControls(GameObject player, bool enabled)
    {
        if (player == null) return;
        foreach (MonoBehaviour s in player.GetComponents<MonoBehaviour>())
        {
            string t = s.GetType().Name;
            if (t == "ThirdPersonController" || t == "FirstPersonController" ||
                t == "StarterAssetsInputs"   || t == "PlayerController")
            {
                s.enabled = enabled;
            }
        }
    }
}