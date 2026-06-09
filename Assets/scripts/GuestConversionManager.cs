using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

/// <summary>
/// Conversion Guest → Email/Password via Firebase REST API.
/// Gagné  (_guestLevel >= 2) → Scene 3
/// Perdu  (_guestLevel <  2) → Scene 2 (rejouer Level 1)
/// </summary>
public class GuestConversionManager : MonoBehaviour
{
    public static GuestConversionManager Instance;

    [Header("Firebase")]
    public string firebaseWebApiKey = "AIzaSyDzakHKcdC3nYmc3rxLuUhIyaZZraYn5dM";

    [Header("Conversion Panel")]
    public GameObject conversionPanel;

    [Header("Champs")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TMP_InputField usernameField;

    [Header("Boutons")]
    public Button convertButton;
    public Button cancelButton;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;
    public GameObject      loadingIndicator;

    private const string URL_SIGNUP = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";
    private const string URL_UPDATE = "https://identitytoolkit.googleapis.com/v1/accounts:update?key=";

    private int _guestLevel = 0;

    // ══════════════════════════════════════════
    //  INIT
    // ══════════════════════════════════════════
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (conversionPanel  != null) conversionPanel .SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (convertButton    != null) convertButton.onClick.AddListener(OnClick_Convert);
        if (cancelButton     != null) cancelButton.gameObject.SetActive(false); // Guest doit convertir
    }

    // ══════════════════════════════════════════
    //  PUBLIC
    // ══════════════════════════════════════════
    public void ShowPanel()
    {
        _guestLevel = 0;

        if (RealtimeDBManager.Instance != null)
        {
            RealtimeDBManager.Instance.LoadUserData(ud =>
            {
                if (ud?.profile != null)
                    _guestLevel = ud.profile.level;

                MainThreadDispatcher.Run(() =>
                {
                    Debug.Log($"[GuestConversion] Guest level chargé : {_guestLevel}");
                    OpenPanel();
                });
            });
        }
        else
        {
            OpenPanel();
        }
    }

    public void HidePanel()
    {
        if (conversionPanel != null) conversionPanel.SetActive(false);
    }

    private void OpenPanel()
    {
        if (conversionPanel != null) conversionPanel.SetActive(true);
        ClearFields();
        HideFeedback();
    }

    // ══════════════════════════════════════════
    //  BOUTON CONVERTIR
    // ══════════════════════════════════════════
    private void OnClick_Convert()
    {
        string email    = emailField    != null ? emailField   .text.Trim() : "";
        string password = passwordField != null ? passwordField.text        : "";
        string username = usernameField != null ? usernameField.text.Trim() : "";

        if (string.IsNullOrEmpty(username)) { ShowFeedback("Username requis.");              return; }
        if (username.Length < 3)            { ShowFeedback("Username trop court (min 3)."); return; }
        if (string.IsNullOrEmpty(email))    { ShowFeedback("Email requis.");                 return; }
        if (!IsValidEmail(email))           { ShowFeedback("Email invalide.");               return; }
        if (string.IsNullOrEmpty(password)) { ShowFeedback("Mot de passe requis.");          return; }
        if (password.Length < 6)            { ShowFeedback("Minimum 6 caractères.");         return; }

        SetLoading(true);
        ShowFeedback("Création du compte...");
        StartCoroutine(DoConvert(email, password, username));
    }

    // ══════════════════════════════════════════
    //  COROUTINE PRINCIPALE
    // ══════════════════════════════════════════
    private IEnumerator DoConvert(string email, string password, string username)
    {
        // ── ÉTAPE 1 : Créer un nouveau compte email/password ──────────────
        string signupBody = "{\"email\":\""      + EscapeJson(email)
                          + "\",\"password\":\"" + EscapeJson(password)
                          + "\",\"returnSecureToken\":true}";

        string newIdToken = "";
        string newLocalId = "";

        using (var req = BuildPost(URL_SIGNUP + firebaseWebApiKey, signupBody))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                SetLoading(false);
                ShowFeedback(ParseError(req.downloadHandler.text));
                yield break;
            }

            newIdToken = ExtractJson(req.downloadHandler.text, "idToken");
            newLocalId = ExtractJson(req.downloadHandler.text, "localId");
            Debug.Log($"[GuestConversion] Nouveau compte Firebase : {newLocalId}");
        }

        if (string.IsNullOrEmpty(newIdToken) || string.IsNullOrEmpty(newLocalId))
        {
            SetLoading(false);
            ShowFeedback("Erreur création compte. Réessayez.");
            yield break;
        }

        // ── ÉTAPE 2 : Mettre à jour le displayName ────────────────────────
        string updateBody = "{\"idToken\":\""      + newIdToken
                          + "\",\"displayName\":\"" + EscapeJson(username)
                          + "\",\"returnSecureToken\":false}";

        using (var req = BuildPost(URL_UPDATE + firebaseWebApiKey, updateBody))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log($"[GuestConversion] DisplayName mis à jour : {username}");
            else
                Debug.LogWarning($"[GuestConversion] DisplayName update échoué : {req.downloadHandler.text}");
        }

        // ── ÉTAPE 3 : Injecter la nouvelle session ────────────────────────
        ShowFeedback("Migration de ta progression...");

        if (RealtimeDBManager.Instance != null)
        {
            RealtimeDBManager.Instance.SetSession(newIdToken, newLocalId, email, false);

            // Créer / charger le profil du nouveau compte
            bool profileDone = false;
            RealtimeDBManager.Instance.InitializeWhenReady(() =>
                RealtimeDBManager.Instance.CreateOrLoadUser(username, ud =>
                {
                    Debug.Log("[GuestConversion] Profil nouveau compte créé/chargé.");
                    profileDone = true;
                }));

            float waited = 0f;
            while (!profileDone && waited < 8f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!profileDone)
                Debug.LogWarning("[GuestConversion] Timeout profil – on continue quand même.");

            // ── ÉTAPE 4 : Migrer le level ──────────────────────────────────
            // On sauvegarde le VRAI level du guest, sans le forcer à 2.
            // "SkipFirstTimePanel" indique à MenuManager que c'est un compte
            // converti (pas un nouveau joueur) → afficher ProgressPanel si besoin.
            RealtimeDBManager.Instance.UpdateLevel(_guestLevel);
            PlayerPrefs.SetInt("SkipFirstTimePanel", 1);
            PlayerPrefs.Save();
            Debug.Log($"[GuestConversion] Level migré → {_guestLevel} (valeur réelle conservée)");

            // Laisser Firebase écrire avant de changer de scène
            yield return new WaitForSeconds(0.6f);
        }

        // ── ÉTAPE 5 : Finaliser UI ────────────────────────────────────────
        AuthManager.SetGuestUser(false);
        SetLoading(false);
        ShowFeedback($"✅ Compte créé ! Bienvenue {username} !");

        yield return new WaitForSeconds(2f);
        HidePanel();

        // ── ÉTAPE 6 : Redirection finale ──────────────────────────────────
        // GAGNÉ  : guest a terminé au moins un niveau (_guestLevel >= 2)
        //          → Scene 3 (écran victoire)
        // PERDU  : guest n'a pas terminé (_guestLevel < 2)
        //          → Scene 2 (Level 1) pour rejouer directement
        //          (on ne triche plus avec le level en DB)

        if (_guestLevel >= 2)
        {
            Debug.Log("[GuestConversion] 🏆 Gagné → Scene 3");
            UnityEngine.SceneManagement.SceneManager.LoadScene(3);
        }
        else
        {
            Debug.Log("[GuestConversion] 🔄 Perdu → Scene 2 (rejouer Level 1)");
            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
        }
    }

    // ══════════════════════════════════════════
    //  UI HELPERS
    // ══════════════════════════════════════════
    private void SetLoading(bool loading)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(loading);
        if (convertButton    != null) convertButton.interactable = !loading;
    }

    private void ShowFeedback(string msg)
    {
        if (feedbackText == null) return;
        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(true);
    }

    private void HideFeedback()
    {
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    private void ClearFields()
    {
        if (emailField    != null) emailField   .text = "";
        if (passwordField != null) passwordField.text = "";
        if (usernameField != null) usernameField.text = "";
    }

    // ══════════════════════════════════════════
    //  HTTP + JSON UTILS
    // ══════════════════════════════════════════
    private UnityWebRequest BuildPost(string url, string json)
    {
        var req             = new UnityWebRequest(url, "POST");
        var raw             = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler   = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    private string ExtractJson(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return "";
        string search = "\"" + key + "\"";
        int keyPos    = json.IndexOf(search);
        if (keyPos < 0) return "";
        int colonPos  = json.IndexOf(':', keyPos + search.Length);
        if (colonPos < 0) return "";
        int start     = json.IndexOf('"', colonPos + 1);
        if (start < 0) return "";
        start++;
        int end = start;
        while (end < json.Length)
        {
            if (json[end] == '"' && json[end - 1] != '\\') break;
            end++;
        }
        if (end >= json.Length) return "";
        return json.Substring(start, end - start);
    }

    private string ParseError(string json)
    {
        if (string.IsNullOrEmpty(json)) return "Erreur inconnue";
        int i = json.IndexOf("\"message\":");
        if (i < 0) return "Erreur de connexion";
        i += 10;
        int e       = json.IndexOfAny(new[] { ',', '}' }, i);
        string code = (e > 0 ? json.Substring(i, e - i) : json.Substring(i)).Trim().Trim('"');
        if (code.Contains(" : ")) code = code.Substring(0, code.IndexOf(" : "));
        switch (code)
        {
            case "EMAIL_EXISTS":     return "Email déjà utilisé.";
            case "INVALID_EMAIL":    return "Email invalide.";
            case "WEAK_PASSWORD":    return "Mot de passe trop faible (min 6).";
            case "TOKEN_EXPIRED":
            case "INVALID_ID_TOKEN": return "Session expirée. Relancez le jeu.";
            default: return string.IsNullOrEmpty(code) ? "Erreur inconnue" : code;
        }
    }

    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private bool IsValidEmail(string email) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}