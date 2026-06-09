using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class TerminalManager : MonoBehaviour
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
    public RectTransform contentRect;

    [Header("=== INPUT ===")]
    public TMP_InputField inputField;
    public Button         submitButton;

    [Header("=== TIMING ===")]
    public float lineDelay = 0.040f;

    [Header("=== AUDIO ===")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    private int  _panel = 0;
    private bool _busy  = false;

    const string G  = "#39FF14";
    const string CY = "#00FFFF";
    const string RE = "#FF003C";
    const string YE = "#FDF500";
    const string PU = "#B026FF";
    const string PL = "#D966FF";
    const string DM = "#556B8D";
    const string WH = "#FFFFFF";
    const string OR = "#FF6600";

    string C(string txt, string hex) => $"<color={hex}>{txt}</color>";
    string N(string raw) => Regex.Replace(raw.ToLower().Trim(), @"\s+", " ");
    string VIRUS(string label = "VIRUS") => C(label, PU);

    // =========================================================================
    //  QUEST NOTIFY
    // =========================================================================
    void NotifyQuest()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnVirusEliminatedViaCMD();
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
        {
            foreach (Button btn in FindObjectsOfType<Button>(true))
            {
                if (btn.gameObject.name == "CloseButton")
                {
                    closeButton = btn;
                    break;
                }
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTerminal);
        }

        if (autoOpen) OpenTerminal();
    }

    [Header("=== AUTO OPEN ===")]
    public bool autoOpen = true;

    public void OpenTerminal()
    {
        StopAllCoroutines();
        outputText.text = "";
        _panel = 0;
        _busy  = false;
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

    void Submit()
    {
        if (_busy) return;
        string raw = inputField.text.Trim();
        if (string.IsNullOrEmpty(raw)) return;
        inputField.text = "";
        inputField.ActivateInputField();

        switch (_panel)
        {
            case 0: HandleStart(raw); break;
            case 1: HandleP1(raw);    break;
            case 3: HandleP3(raw);    break;
            case 4: HandleP4(raw);    break;
            case 5: HandleP5(raw);    break;
            case 6: HandleP6(raw);    break;
            case 7: HandleP7(raw);    break;
            case 8:
                Append(C("  [ TERMINAL LOCKED ]", DM));
                PlayWrongSound();
                break;
        }
    }

    // =========================================================================
    //  AUDIO HELPERS
    // =========================================================================
    void PlayCorrectSound()
    {
        if (audioSource != null && correctSound != null)
            audioSource.PlayOneShot(correctSound);
    }

    void PlayWrongSound()
    {
        if (audioSource != null && wrongSound != null)
            audioSource.PlayOneShot(wrongSound);
    }

    // =========================================================================
    //  PANEL 0 -- INTRO
    // =========================================================================
    string[] Panel0_Intro() => new[]
    {
        C(@"██╗   ██╗██╗██████╗ ██╗   ██╗███████╗", PU),
        C(@"██║   ██║██║██╔══██╗██║   ██║██╔════╝", PU),
        C(@"██║   ██║██║██████╔╝██║   ██║███████╗", PL),
        C(@"╚██╗ ██╔╝██║██╔══██╗██║   ██║╚════██║", PL),
        C(@" ╚████╔╝ ██║██║  ██║╚██████╔╝███████║", PU),
        C(@"  ╚═══╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝", PU) + C("  TERMINAL v1.0", CY),
        "",
        C("┌─────────────────────────────────────────────┐", DM),
        C("│", DM) + C(" [!] ", RE) + "System: " + C("192.168.1.105", WH) +
            "  |  " + C("CRITICAL", RE) + " — " + VIRUS("active virus") + C("        │", DM),
        C("│", DM) + C(" [*] ", CY) + "Analyst: " + C("root", PU) +
            "  |  Time: " + C("03:12", RE) + C("                         │", DM),
        C("└─────────────────────────────────────────────┘", DM),
        "",
        C("  > ", CY) + "Find the " + VIRUS() + ". Eliminate it. Secure the system.",
        "",
        "   -> Type " + C("start", G),
    };

    void HandleStart(string raw)
    {
        if (N(raw) == "start")
        {
            _panel = 1;
            PlayCorrectSound();
            NotifyQuest(); // étape 1/7
            StartCoroutine(PrintLines(Panel1_Reco()));
        }
        else WrongCmd(raw);
    }

    // =========================================================================
    //  PANEL 1 -- RECONNAISSANCE
    // =========================================================================
    string[] Panel1_Reco() => new[]
    {
        "",
        C("╔══════════════════════════════╗", PU),
        C("║", PU) + C("  [1/5] RECONNAISSANCE          ", CY) + C("║", PU),
        C("╚══════════════════════════════╝", PU),
        "  List all files on this machine.",
        "",
        C("  [?] ", YE) + "Type this command: " + C("ls -lh", WH),
    };

    void HandleP1(string raw)
    {
        string v = N(raw);
        if (v == "ls -lh" || v == "ls -la" || v == "ls -lah" || v == "ls -l" || v == "ls")
        {
            _panel = 3;
            PlayCorrectSound();
            NotifyQuest(); // étape 2/7
            StartCoroutine(PrintThenGo(Panel1_Result(raw), Panel3_Analysis));
        }
        else WrongCmd(raw);
    }

    string[] Panel1_Result(string raw) => new[]
    {
        "",
        C("root@virus-terminal:~# ", PU) + raw,
        C("  -rw-r--r--  2.1K   report.docx",  DM),
        C("  -rw-r--r--  156K   budget.xlsx",  DM),
        C("  -rw-r--r--   84K   notes.txt",    DM),
        C("  -rwxr-xr-x  847M   ", DM) + C("update.exe", RE) + C("  ◄ suspicious", OR),
        C("  -rw-r--r--   12K   config.json",  DM),
    };

    // =========================================================================
    //  PANEL 3 -- ANALYSIS
    // =========================================================================
    string[] Panel3_Analysis() => new[]
    {
        "",
        C("╔══════════════════════════════╗", PU),
        C("║", PU) + C("  VIRUS ANALYSIS                ", PL) + C("║", PU),
        C("╚══════════════════════════════╝", PU),
        C("  [?] ", YE) + "One of these files is the " + VIRUS() + ". Which one?",
        "",
        C("  1) ", CY) + "report.docx",
        C("  2) ", CY) + "budget.xlsx",
        C("  3) ", CY) + "notes.txt",
        C("  4) ", CY) + C("update.exe", RE),
        C("  5) ", CY) + "config.json",
        "",
        "   Type the number " + C("(1-5)", WH) + ".",
    };

    void HandleP3(string raw)
    {
        string v = N(raw);
        if (v == "4" || v.Contains("update.exe") || v == "update")
        {
            _panel = 4;
            PlayCorrectSound();
            NotifyQuest(); // étape 3/7
            StartCoroutine(PrintThenGo(Panel3_Correct(), Panel4_Grep));
        }
        else
        {
            PlayWrongSound();
            Append(C("  [-] Wrong. Analyse the file list again.", RE));
        }
    }

    string[] Panel3_Correct() => new[]
    {
        C("  [+] Correct! ", G) + VIRUS() + C(" identified: ", WH) + C("update.exe", RE),
    };

    // =========================================================================
    //  PANEL 4 -- GREP
    // =========================================================================
    string[] Panel4_Grep() => new[]
    {
        "",
        C("╔══════════════════════════════╗", PU),
        C("║", PU) + C("  [2/5] THREAT INTEL            ", CY) + C("║", PU),
        C("╚══════════════════════════════╝", PU),
        "  Search for the " + VIRUS() + " signature inside the infected folder.",
        "",
        C("  [?] ", YE) + "You have all the pieces:",
        C("      command   -> ", DM) + C("grep -r", WH),
        C("      signature -> ", DM) + C("MALWARE_SIG", RE),
        C("      folder    -> ", DM) + C("/files", CY),
        "",
        "   Now build and type the full command.",
    };

    void HandleP4(string raw)
    {
        string v = N(raw).Replace("\"", "").Replace("'", "");
        if (v == "grep -r malware_sig /files" || v == "grep -ri malware_sig /files")
        {
            _panel = 5;
            PlayCorrectSound();
            NotifyQuest(); // étape 4/7
            StartCoroutine(PrintThenGo(Panel4_Result(raw), Panel5_Process));
        }
        else WrongCmd(raw);
    }

    string[] Panel4_Result(string raw) => new[]
    {
        "",
        C("root@virus-terminal:~# ", PU) + raw,
        C("  /files/update.exe          ", RE) + C("MALWARE_SIG", OR) + C(" found", RE),
        C("  /files/invoices_oct.xlsx   ", RE) + C("MALWARE_SIG", OR) + C(" found", RE),
        C("  [+] 2 files infected by ", G) + VIRUS() + C("!", G),
    };

    // =========================================================================
    //  PANEL 5 -- PROCESS
    // =========================================================================
    string[] Panel5_Process() => new[]
    {
        "",
        C("╔══════════════════════════════╗", PU),
        C("║", PU) + C("  [3/5] MEMORY ANALYSIS         ", CY) + C("║", PU),
        C("╚══════════════════════════════╝", PU),
        "  The " + VIRUS() + " is running in memory " + C("RIGHT NOW", RE) + ".",
        "  Which command shows active processes?",
        "",
        C("  A) ", CY) + "ls -la /tmp         " + C("(list files)", DM),
        C("  B) ", CY) + "ping 8.8.8.8        " + C("(network test)", DM),
        C("  C) ", CY) + "ps aux | grep ...   " + C("(active processes)", G),
        C("  D) ", CY) + "cat /etc/passwd     " + C("(read a file)", DM),
    };

    void HandleP5(string raw)
    {
        string v = N(raw);
        if (v == "c" || v.Contains("ps aux"))
        {
            _panel = 6;
            PlayCorrectSound();
            NotifyQuest(); // étape 5/7
            StartCoroutine(PrintThenGo(Panel5_Result(), Panel6_Delete));
        }
        else
        {
            PlayWrongSound();
            Append(C("  [-] Wrong. Which one shows PROCESSES?", RE));
        }
    }

    string[] Panel5_Result() => new[]
    {
        C("  [+] Correct!", G),
        C("root@virus-terminal:~# ", PU) + "ps aux | grep update.exe",
        C("  root  PID:7139  98.7%  /tmp/update.exe --phoning-home", RE),
        C("  [!] ", RE) + VIRUS() + C(" process detected — PID 7139", WH),
    };

    // =========================================================================
    //  PANEL 6 -- DELETE
    // =========================================================================
    string[] Panel6_Delete() => new[]
    {
        "",
        C("╔══════════════════════════════╗", PU),
        C("║", PU) + C("  [4/5] CONTAINMENT             ", CY) + C("║", PU),
        C("╚══════════════════════════════╝", PU),
        "  The " + VIRUS() + " is at " + C("/tmp/update.exe", CY) + ". Delete it.",
        "",
        C("  [?] ", YE) + "Start your command with: " + C("rm -f", WH),
        C("      path -> /tmp/update.exe", DM),
        C("      (no space inside the path!)", OR),
    };

    void HandleP6(string raw)
    {
        string v = N(raw);
        v = v.Replace("/tmp /update.exe", "/tmp/update.exe");
        if (v == "rm -f /tmp/update.exe" || v == "rm -rf /tmp/update.exe" || v == "rm /tmp/update.exe")
        {
            _panel = 7;
            PlayCorrectSound();
            NotifyQuest(); // étape 6/7
            StartCoroutine(PrintThenGo(Panel6_Result(raw), Panel7_Chmod));
        }
        else WrongCmd(raw);
    }

    string[] Panel6_Result(string raw) => new[]
    {
        "",
        C("root@virus-terminal:~# ", PU) + raw,
        C("  PID 7139          ", G) + C("KILLED",  G),
        C("  /tmp/update.exe   ", G) + C("DELETED", G),
        C("  [+] ", G) + VIRUS() + C(" connection severed!", G),
    };

    // =========================================================================
    //  PANEL 7 -- CHMOD
    // =========================================================================
    string[] Panel7_Chmod() => new[]
    {
        "",
        C("╔══════════════════════════════╗", PU),
        C("║", PU) + C("  [5/5] QUARANTINE              ", CY) + C("║", PU),
        C("╚══════════════════════════════╝", PU),
        C("  invoices_oct.xlsx", RE) + " is infected — forensics needs it.",
        "  Lock it: " + C("zero permissions", YE) + " for everyone.",
        C("  [?] chmod  000=none  644=partial  777=full", DM),
        "",
        C("  A) ", CY) + "chmod 777 /files/invoices_oct.xlsx",
        C("  B) ", CY) + "chmod 000 /files/invoices_oct.xlsx",
        C("  C) ", CY) + "chmod 644 /files/invoices_oct.xlsx",
    };

    void HandleP7(string raw)
    {
        string v = N(raw);
        if (v == "b" || v.Contains("chmod 000"))
        {
            _panel = 8;
            PlayCorrectSound();
            NotifyQuest(); // étape 7/7 → déclenche la WIN si autres quêtes OK
            StartCoroutine(PrintThenGo(Panel7_Result(), Panel8_Victory));
        }
        else
        {
            PlayWrongSound();
            Append(C("  [-] Wrong. Which number means zero permissions?", RE));
        }
    }

    string[] Panel7_Result() => new[]
    {
        C("  [+] Correct!", G),
        C("root@virus-terminal:~# ", PU) + "chmod 000 /files/invoices_oct.xlsx",
        C("  invoices_oct.xlsx   ", G) + C("LOCKED", G),
    };

    // =========================================================================
    //  PANEL 8 -- VICTORY
    // =========================================================================
    string[] Panel8_Victory() => new[]
    {
        "",
        C("╔═════════════════════════════════════════════╗", PU),
        C("║", PU) + C("         THREAT ELIMINATION REPORT           ", PL) + C("║", PU),
        C("╠═════════════════════════════════════════════╣", PU),
        C("║", PU) + C("  [OK]  Files scanned      ", G)  + C("DONE", G)  + C("                ║", PU),
        C("║", PU) + C("  [OK]  ", G) + VIRUS() + C(" identified   ", G)  + C("DONE", G)  + C("                ║", PU),
        C("║", PU) + C("  [OK]  Signature found    ", G)  + C("DONE", G)  + C("                ║", PU),
        C("║", PU) + C("  [OK]  Process killed     ", G)  + C("DONE", G)  + C("                ║", PU),
        C("║", PU) + C("  [OK]  File deleted       ", G)  + C("DONE", G)  + C("                ║", PU),
        C("║", PU) + C("  [OK]  Quarantine set     ", G)  + C("DONE", G)  + C("                ║", PU),
        C("╠═════════════════════════════════════════════╣", PU),
        C("║", PU) + C("  SCORE: 5/5  —  INCIDENT RESPONDER          ", YE) + C("║", PU),
        C("╠═════════════════════════════════════════════╣", PU),
        C("║", PU) + C("       [ ", DM) + VIRUS("VIRUS NEUTRALIZED") + C(" ]             ║", DM),
        C("╚═════════════════════════════════════════════╝", PU),
    };

    // =========================================================================
    //  COROUTINES & SCROLLING
    // =========================================================================
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
            yield return null; yield return null;
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
            C("root@virus-terminal:~# ", PU) + raw + "\n" +
            C("  bash: command not found. Re-read the hint.", RE)
        ));
    }

    void Append(string line) => StartCoroutine(AppendAndScroll(line));

    IEnumerator AppendAndScroll(string line)
    {
        outputText.text += line + "\n";
        yield return null; yield return null;
        ScrollToBottom();
    }
}