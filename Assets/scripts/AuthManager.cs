using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

// ══════════════════════════════════════════════════════════════
//  MAIN THREAD DISPATCHER
// ══════════════════════════════════════════════════════════════
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> _queue = new Queue<Action>();

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                _instance = go.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null) { _instance = this; DontDestroyOnLoad(gameObject); }
        else if (_instance != this) Destroy(gameObject);
    }

    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
                _queue.Dequeue()?.Invoke();
        }
    }

    public void Enqueue(Action action) { lock (_queue) { _queue.Enqueue(action); } }
    public static void Run(Action action) => Instance.Enqueue(action);
}

// ══════════════════════════════════════════════════════════════
//  AUTH MANAGER — FIREBASE REST API
// ══════════════════════════════════════════════════════════════
public class AuthManager : MonoBehaviour
{
    [Header("Firebase Config")]
    public string firebaseWebApiKey = "AIzaSyDzakHKcdC3nYmc3rxLuUhIyaZZraYn5dM";

    [Header("Options")]
    public bool sendVerificationEmailOnSignUp = true;

    private const string URL_SIGNUP   = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=";
    private const string URL_SIGNIN   = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=";
    private const string URL_SEND_OOB = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=";
    private const string URL_UPDATE   = "https://identitytoolkit.googleapis.com/v1/accounts:update?key=";

    // ── État public statique ─────────────────────────────────
    public static bool   IsGuestUser     { get; private set; } = false;
    public static string CurrentIdToken  { get; private set; } = "";   // ✅ NOUVEAU
    public static string CurrentLocalId  { get; private set; } = "";   // ✅ NOUVEAU

    public static void SetGuestUser(bool v) { IsGuestUser = v; }

    // ── Session courante ─────────────────────────────────────
    private string _idToken   = "";
    private string _localId   = "";
    private string _userEmail = "";

    private bool HasValidSession => !string.IsNullOrEmpty(_idToken) && !string.IsNullOrEmpty(_localId);

    // ── UI LOGIN ─────────────────────────────────────────────
    [Header("LOGIN UI")]
    public GameObject      loginPanel;
    public GameObject      verificationLoginPanel;
    public TMP_InputField  loginEmailField;
    public TMP_InputField  loginPasswordField;
    public TextMeshProUGUI erreurLogin;

    // ── UI SIGN UP ───────────────────────────────────────────
    [Header("SIGN UP UI")]
    public GameObject      signUpPanel;
    public GameObject      verificationSignUpPanel;
    public TMP_InputField  signUpEmailField;
    public TMP_InputField  signUpPasswordField;
    public TMP_InputField  signUpUsernameField;
    public TextMeshProUGUI erreurCreateAccount;

    // ── UI EMAIL VERIFICATION ────────────────────────────────
    [Header("EMAIL VERIFICATION UI (Sign-Up only)")]
    public GameObject      emailVerificationPanel;
    public TextMeshProUGUI verificationEmailText;
    public Button          emailVerificationOkButton;
    public Button          resendEmailButton;

    // ── MANAGERS ─────────────────────────────────────────────
    [Header("UI & DB Managers")]
    public UIManager         uiManager;
    public RealtimeDBManager dbManager;
    public bool              autoFindDBManager        = true;
    public bool              createDBManagerIfMissing = true;

    // ══════════════════════════════════════════
    //  INIT
    // ══════════════════════════════════════════
    private void Awake()
    {
        var _ = MainThreadDispatcher.Instance;
        SetupDBManager();
    }

    private void Start()
    {
        if (dbManager == null && autoFindDBManager)
            SetupDBManager();
        HideAllPanels();
        Debug.Log("✅ AuthManager REST prêt");
    }

    private void SetupDBManager()
    {
        if (dbManager != null) return;
        if (RealtimeDBManager.Instance != null) { dbManager = RealtimeDBManager.Instance; return; }
        dbManager = FindObjectOfType<RealtimeDBManager>();
        if (dbManager != null) return;
        if (createDBManagerIfMissing)
        {
            var go = new GameObject("RealtimeDBManager");
            dbManager = go.AddComponent<RealtimeDBManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void HideAllPanels()
    {
        if (verificationLoginPanel  != null) verificationLoginPanel .SetActive(false);
        if (verificationSignUpPanel != null) verificationSignUpPanel.SetActive(false);
        if (emailVerificationPanel  != null) emailVerificationPanel .SetActive(false);
    }

    // ══════════════════════════════════════════
    //  NAVIGATION PUBLIQUE
    // ══════════════════════════════════════════
    public void OnClick_GoToForgotPassword()
    {
        EnsureUIManager();
        uiManager?.ShowForgotPassword();
    }

    public void OnForgotPasswordEmailSent()
    {
        EnsureUIManager();
        uiManager?.ShowLogin();
        Debug.Log("🔑 Forgot password envoyé → retour login");
    }

    // ══════════════════════════════════════════
    //  LOGIN
    // ══════════════════════════════════════════
    public void OnClick_Login()
    {
        if (loginEmailField == null || loginPasswordField == null) return;

        string email    = loginEmailField.text.Trim();
        string password = loginPasswordField.text;
        HideLoginError();

        if (string.IsNullOrEmpty(email))    { ShowLoginError("Email is empty");    return; }
        if (!IsValidEmail(email))           { ShowLoginError("Invalid email");      return; }
        if (string.IsNullOrEmpty(password)) { ShowLoginError("Password is empty"); return; }

        ShowLoginInfo("Connexion en cours...");
        StartCoroutine(DoLogin(email, password));
    }

    private IEnumerator DoLogin(string email, string password)
    {
        string body = "{\"email\":\"" + email + "\",\"password\":\"" + password + "\",\"returnSecureToken\":true}";

        using (var req = BuildPostRequest(URL_SIGNIN + firebaseWebApiKey, body))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                ShowLoginError(ParseFirebaseError(req.downloadHandler.text));
                yield break;
            }

            ParseAuthResponse(req.downloadHandler.text);   // ✅ met à jour CurrentIdToken/CurrentLocalId
            IsGuestUser = false;
            HideLoginError();
            NavigateAfterAuth();
        }
    }

    // ── LOGIN ANONYME (GUEST) ────────────────────────────────
    public void OnClick_LoginAnonymously()
    {
        HideLoginError();
        ShowLoginInfo("Connexion invité...");
        StartCoroutine(DoLoginAnonymously());
    }

    private IEnumerator DoLoginAnonymously()
    {
        string body = "{\"returnSecureToken\":true}";

        using (var req = BuildPostRequest(URL_SIGNUP + firebaseWebApiKey, body))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                ShowLoginError(ParseFirebaseError(req.downloadHandler.text));
                yield break;
            }

            ParseAuthResponse(req.downloadHandler.text);   // ✅ stocke token guest dans CurrentIdToken
            IsGuestUser = true;
            HideLoginError();

            SetupDBManager();
            InjectSessionIntoDBManager();

            if (dbManager != null)
                dbManager.InitializeWhenReady(() =>
                    LoadUserDataThen(ud =>
                    {
                        EnsureUIManager();
                        uiManager?.ShowMenu(ud);
                    }));
            else
            {
                EnsureUIManager();
                uiManager?.ShowMenu(null);
            }
        }
    }

    // ── SIGN UP ──────────────────────────────────────────────
    public void OnClick_SignUp()
    {
        if (signUpEmailField == null || signUpPasswordField == null || signUpUsernameField == null) return;

        string email    = signUpEmailField.text.Trim();
        string password = signUpPasswordField.text;
        string username = signUpUsernameField.text.Trim();
        HideSignUpError();

        if (string.IsNullOrEmpty(username))  { ShowSignUpError("Username is empty");                return; }
        if (username.Length < 3)             { ShowSignUpError("Username too short (min 3 chars)"); return; }
        if (string.IsNullOrEmpty(email))     { ShowSignUpError("Email is empty");                   return; }
        if (!IsValidEmail(email))            { ShowSignUpError("Invalid email");                    return; }
        if (string.IsNullOrEmpty(password))  { ShowSignUpError("Password is empty");                return; }
        if (password.Length < 6)             { ShowSignUpError("Password too short (min 6 chars)"); return; }

        ShowSignUpInfo("Création du compte...");
        StartCoroutine(DoSignUp(email, password, username));
    }

    private IEnumerator DoSignUp(string email, string password, string username)
    {
        // ── 1 : Créer le compte Firebase ─────────────────────
        string body = "{\"email\":\"" + email + "\",\"password\":\"" + password + "\",\"returnSecureToken\":true}";

        using (var req = BuildPostRequest(URL_SIGNUP + firebaseWebApiKey, body))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                ShowSignUpError(ParseFirebaseError(req.downloadHandler.text));
                yield break;
            }

            ParseAuthResponse(req.downloadHandler.text);   // ✅ met à jour CurrentIdToken/CurrentLocalId
            IsGuestUser = false;

            if (!HasValidSession)
            {
                ShowSignUpError("Compte créé mais token manquant. Réessayez.");
                yield break;
            }

            HideSignUpError();
        }

        // ── 2 : Injecter session dans DB ──────────────────────
        SetupDBManager();
        InjectSessionIntoDBManager();

        yield return new WaitForSeconds(0.5f);

        // ── 3 : DisplayName ───────────────────────────────────
        yield return StartCoroutine(TryUpdateDisplayName(username));

        // ── 4 : Créer profil en DB ────────────────────────────
        ShowSignUpInfo("Création du profil...");
        bool dbDone = false;
        dbManager?.InitializeWhenReady(() =>
            dbManager.CreateOrLoadUser(username, ud =>
            {
                Debug.Log("✅ Profil créé: " + (ud?.profile?.username ?? "null"));
                dbDone = true;
            }));

        float waited = 0f;
        while (!dbDone && waited < 10f) { waited += Time.deltaTime; yield return null; }

        // ── 5 : Email de vérification puis → LOGIN ────────────
        if (signUpPanel != null) signUpPanel.SetActive(false);

        if (sendVerificationEmailOnSignUp)
        {
            yield return StartCoroutine(TrySendVerificationEmail());
            ShowVerificationSentPanel();
        }
        else
        {
            GoToLogin();
        }
    }

    // ── FORGOT PASSWORD ──────────────────────────────────────
    public void SendPasswordResetEmail(string email, Action<bool, string> onComplete)
    {
        StartCoroutine(DoSendPasswordReset(email, onComplete));
    }

    private IEnumerator DoSendPasswordReset(string email, Action<bool, string> onComplete)
    {
        string body = "{\"requestType\":\"PASSWORD_RESET\",\"email\":\"" + email + "\"}";

        using (var req = BuildPostRequest(URL_SEND_OOB + firebaseWebApiKey, body))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                onComplete?.Invoke(false, ParseFirebaseError(req.downloadHandler.text));
            else
                onComplete?.Invoke(true, "Email envoyé à " + email);
        }
    }

    // ── CLOSE ERRORS ─────────────────────────────────────────
    public void OnClick_CloseLoginError()  => HideLoginError();
    public void OnClick_CloseSignUpError() => HideSignUpError();

    // ── PANEL VÉRIFICATION EMAIL ─────────────────────────────
    public void OnClick_CheckEmailVerified()
    {
        if (emailVerificationPanel != null) emailVerificationPanel.SetActive(false);
        GoToLogin();
    }

    public void OnClick_ResendEmailVerification()
    {
        if (string.IsNullOrEmpty(_idToken)) return;
        if (resendEmailButton         != null) resendEmailButton        .interactable = false;
        if (emailVerificationOkButton != null) emailVerificationOkButton.interactable = false;
        if (verificationEmailText     != null) verificationEmailText    .text         = "Sending...";

        StartCoroutine(TrySendVerificationEmail(
            onSuccess: () =>
            {
                if (verificationEmailText != null)
                    verificationEmailText.text = "Email resent to " + _userEmail + ".\nCheck your inbox.";
                StartCoroutine(ReenableButtons(3f));
            },
            onFail: err =>
            {
                if (verificationEmailText     != null) verificationEmailText    .text         = "Error: " + err;
                if (resendEmailButton         != null) resendEmailButton        .interactable = true;
                if (emailVerificationOkButton != null) emailVerificationOkButton.interactable = true;
            }));
    }

    // ══════════════════════════════════════════
    //  SESSION
    // ══════════════════════════════════════════
    private void InjectSessionIntoDBManager()
    {
        SetupDBManager();
        if (dbManager == null) return;
        dbManager.SetSession(_idToken, _localId, _userEmail, IsGuestUser);
        Debug.Log("✅ Session injectée dans RealtimeDBManager");
    }

    // ══════════════════════════════════════════
    //  NAVIGATION PRIVÉE
    // ══════════════════════════════════════════
    private void NavigateAfterAuth()
    {
        if (!HasValidSession)
        {
            Debug.LogError("❌ NavigateAfterAuth — session invalide, retour au login");
            GoToLogin();
            return;
        }

        EnsureUIManager();
        if (uiManager == null) return;

        SetupDBManager();
        InjectSessionIntoDBManager();

        if (dbManager != null)
            dbManager.InitializeWhenReady(() =>
                LoadUserDataThen(ud => uiManager.ShowMenu(ud)));
        else
            uiManager.ShowMenu(null);
    }

    private void GoToLogin()
    {
        if (signUpEmailField    != null) signUpEmailField   .text = "";
        if (signUpPasswordField != null) signUpPasswordField.text = "";
        if (signUpUsernameField != null) signUpUsernameField.text = "";

        EnsureUIManager();
        uiManager?.ShowLogin();
        Debug.Log("🔑 Retour au login");
    }

    private void LoadUserDataThen(Action<RealtimeDBManager.UserData> onComplete)
    {
        SetupDBManager();
        if (dbManager != null)
            dbManager.CreateOrLoadUser(null, ud =>
                MainThreadDispatcher.Run(() => onComplete?.Invoke(ud)));
        else
            onComplete?.Invoke(null);
    }

    private void EnsureUIManager()
    {
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
    }

    // ══════════════════════════════════════════
    //  HELPERS PRIVÉS
    // ══════════════════════════════════════════

    /// <summary>
    /// ✅ Parse la réponse Firebase et expose le token via les propriétés statiques
    ///    pour que GuestConversionManager puisse y accéder sans Firebase SDK.
    /// </summary>
    private void ParseAuthResponse(string json)
    {
        _idToken   = ExtractJsonString(json, "idToken");
        _localId   = ExtractJsonString(json, "localId");
        _userEmail = ExtractJsonString(json, "email");

        // ✅ Expose statiquement pour GuestConversionManager
        CurrentIdToken = _idToken;
        CurrentLocalId = _localId;

        Debug.Log("Token:   " + (_idToken.Length > 20 ? _idToken.Substring(0, 20) + "..." : _idToken));
        Debug.Log("LocalId: " + _localId);
        Debug.Log("Email:   " + _userEmail);
    }

    private IEnumerator TrySendVerificationEmail(Action onSuccess = null, Action<string> onFail = null)
    {
        if (string.IsNullOrEmpty(_idToken)) { onFail?.Invoke("Token manquant"); yield break; }

        string body = "{\"requestType\":\"VERIFY_EMAIL\",\"idToken\":\"" + _idToken + "\"}";
        using (var req = BuildPostRequest(URL_SEND_OOB + firebaseWebApiKey, body))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                string err = ParseFirebaseError(req.downloadHandler.text);
                Debug.LogWarning("Email vérification échoué: " + err);
                onFail?.Invoke(err);
            }
            else
            {
                Debug.Log("✅ Email de vérification envoyé à " + _userEmail);
                onSuccess?.Invoke();
            }
        }
    }

    private IEnumerator TryUpdateDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(_idToken)) yield break;
        string safe = displayName.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string body = "{\"idToken\":\"" + _idToken + "\",\"displayName\":\"" + safe + "\",\"returnSecureToken\":false}";
        using (var req = BuildPostRequest(URL_UPDATE + firebaseWebApiKey, body))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("Update displayName échoué: " + req.downloadHandler.text);
            else
                Debug.Log("✅ DisplayName mis à jour: " + displayName);
        }
    }

    private void ShowVerificationSentPanel()
    {
        if (emailVerificationPanel == null) return;
        emailVerificationPanel.SetActive(true);
        if (verificationEmailText     != null)
            verificationEmailText.text =
                "Un email de vérification a été envoyé à\n" + _userEmail +
                "\n\nVérifiez votre boîte mail.\nCliquez OK pour aller au login.";
        if (emailVerificationOkButton != null) emailVerificationOkButton.interactable = true;
        if (resendEmailButton         != null) resendEmailButton        .interactable = true;
    }

    private IEnumerator ReenableButtons(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resendEmailButton         != null) resendEmailButton        .interactable = true;
        if (emailVerificationOkButton != null) emailVerificationOkButton.interactable = true;
    }

    // ── Feedback UI ──────────────────────────────────────────
    private void ShowLoginInfo(string msg)
    {
        if (verificationLoginPanel != null) verificationLoginPanel.SetActive(true);
        if (erreurLogin            != null) erreurLogin.text = msg;
    }

    private void ShowLoginError(string msg)
    {
        if (verificationLoginPanel != null) verificationLoginPanel.SetActive(true);
        if (erreurLogin            != null) erreurLogin.text = msg;
        ShakePanel(verificationLoginPanel?.GetComponent<RectTransform>());
    }

    private void HideLoginError()
    {
        if (verificationLoginPanel != null) verificationLoginPanel.SetActive(false);
    }

    private void ShowSignUpInfo(string msg)
    {
        if (verificationSignUpPanel != null) verificationSignUpPanel.SetActive(true);
        if (erreurCreateAccount     != null) erreurCreateAccount.text = msg;
    }

    private void ShowSignUpError(string msg)
    {
        if (verificationSignUpPanel != null) verificationSignUpPanel.SetActive(true);
        if (erreurCreateAccount     != null) erreurCreateAccount.text = msg;
        ShakePanel(verificationSignUpPanel?.GetComponent<RectTransform>());
    }

    private void HideSignUpError()
    {
        if (verificationSignUpPanel != null) verificationSignUpPanel.SetActive(false);
    }

    private void ShakePanel(RectTransform panel, float duration = 0.5f, float magnitude = 10f)
    {
        if (panel != null && gameObject.activeInHierarchy)
            StartCoroutine(ShakeCoroutine(panel, duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(RectTransform panel, float duration, float magnitude)
    {
        Vector3 origin  = panel.anchoredPosition;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            panel.anchoredPosition = origin + new Vector3(UnityEngine.Random.Range(-1f, 1f) * magnitude, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panel.anchoredPosition = origin;
    }

    // ── HTTP ─────────────────────────────────────────────────
    private UnityWebRequest BuildPostRequest(string url, string jsonBody)
    {
        var req             = new UnityWebRequest(url, "POST");
        var raw             = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler   = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    // ── JSON minimal ─────────────────────────────────────────
    private string ExtractJsonString(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return "";
        string search   = "\"" + key + "\"";
        int    keyPos   = json.IndexOf(search);
        if (keyPos < 0) return "";
        int colonPos    = json.IndexOf(':', keyPos + search.Length);
        if (colonPos < 0) return "";
        int valueStart  = json.IndexOf('"', colonPos + 1);
        if (valueStart < 0) return "";
        valueStart++;
        int valueEnd = valueStart;
        while (valueEnd < json.Length)
        {
            if (json[valueEnd] == '"' && json[valueEnd - 1] != '\\') break;
            valueEnd++;
        }
        if (valueEnd >= json.Length) return "";
        return json.Substring(valueStart, valueEnd - valueStart);
    }

    private string ParseFirebaseError(string json)
    {
        if (string.IsNullOrEmpty(json)) return "Erreur inconnue";
        try
        {
            string code = ExtractJsonString(json, "message");
            if (string.IsNullOrEmpty(code))
            {
                int i = json.IndexOf("\"message\":");
                if (i >= 0)
                {
                    i += 10;
                    int e = json.IndexOfAny(new[] { ',', '}' }, i);
                    code = e > 0 ? json.Substring(i, e - i).Trim().Trim('"') : "";
                }
            }
            if (code.Contains(" : ")) code = code.Substring(0, code.IndexOf(" : "));
            switch (code)
            {
                case "EMAIL_NOT_FOUND":
                case "USER_NOT_FOUND":              return "Compte introuvable";
                case "INVALID_PASSWORD":
                case "INVALID_LOGIN_CREDENTIALS":   return "Email ou mot de passe incorrect";
                case "EMAIL_EXISTS":                return "Email déjà utilisé";
                case "WEAK_PASSWORD":               return "Mot de passe trop faible (min 6 chars)";
                case "INVALID_EMAIL":               return "Email invalide";
                case "OPERATION_NOT_ALLOWED":       return "Méthode non autorisée";
                case "TOO_MANY_ATTEMPTS_TRY_LATER": return "Trop de tentatives. Réessayez plus tard.";
                case "INVALID_ID_TOKEN":            return "Session expirée. Reconnectez-vous.";
                case "USER_DISABLED":               return "Compte désactivé";
                default: return string.IsNullOrEmpty(code) ? "Erreur de connexion" : code;
            }
        }
        catch { return "Erreur de connexion. Vérifiez votre internet."; }
    }

    private bool IsValidEmail(string email) =>
        System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}