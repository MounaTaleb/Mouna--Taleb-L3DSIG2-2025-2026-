using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public class RealtimeDBManager : MonoBehaviour
{
    public static RealtimeDBManager Instance;

    [Header("Database Configuration")]
    public string databaseURL = "https://viroguard-b69de-default-rtdb.firebaseio.com";

    private string _idToken     = "";
    private string _localId     = "";
    private string _userEmail   = "";
    private bool   _isAnonymous = false;

    private bool _isReady = false;
    public bool IsInitialized => _isReady;
    public bool HasSession => !string.IsNullOrEmpty(_localId) && !string.IsNullOrEmpty(_idToken);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        databaseURL = databaseURL?.Trim();

        if (!string.IsNullOrEmpty(databaseURL))
        {
            _isReady = true;
            Debug.Log("✅ RealtimeDBManager REST prêt — " + databaseURL);
        }
        else
        {
            Debug.LogError("❌ databaseURL non configurée !");
        }
    }

    public void SetSession(string idToken, string localId, string email, bool isAnonymous)
    {
        _idToken     = idToken;
        _localId     = localId;
        _userEmail   = email;
        _isAnonymous = isAnonymous;
        Debug.Log($"🔑 Session DB définie — localId: {localId} | anonymous: {isAnonymous}");
    }

    public void InitializeWhenReady(Action onComplete)
    {
        if (_isReady) { onComplete?.Invoke(); return; }
        StartCoroutine(WaitUntilReady(onComplete));
    }

    private IEnumerator WaitUntilReady(Action onComplete)
    {
        float timeout = 10f, elapsed = 0f;
        while (!_isReady && elapsed < timeout) { elapsed += Time.deltaTime; yield return null; }
        if (_isReady) onComplete?.Invoke();
        else Debug.LogError("❌ Timeout — RealtimeDBManager non prêt après " + timeout + "s");
    }

    private string UserUrl(string subPath = "")
    {
        string path = "users/" + _localId;
        if (!string.IsNullOrEmpty(subPath)) path += "/" + subPath;
        return databaseURL.TrimEnd('/') + "/" + path + ".json?auth=" + _idToken;
    }

    public void CreateOrLoadUser(string username = null, Action<UserData> onComplete = null)
    {
        if (!_isReady) { InitializeWhenReady(() => CreateOrLoadUser(username, onComplete)); return; }

        if (string.IsNullOrEmpty(_localId))
        {
            Debug.LogError("❌ localId vide — appelez SetSession d'abord");
            onComplete?.Invoke(null);
            return;
        }

        StartCoroutine(DoCreateOrLoad(username, onComplete));
    }

    private IEnumerator DoCreateOrLoad(string username, Action<UserData> onComplete)
    {
        using (var req = UnityWebRequest.Get(UserUrl()))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ DB GET error: " + req.error + " | " + req.downloadHandler.text);
                onComplete?.Invoke(null);
                yield break;
            }

            string body = req.downloadHandler.text;
            if (!string.IsNullOrEmpty(body) && body.Trim() != "null")
            {
                UserData data = SafeFromJson(body);
                if (data != null)
                {
                    Debug.Log($"✅ Chargé — {data.profile?.username} | Level: {data.profile?.level}");
                    onComplete?.Invoke(data);
                    yield break;
                }
            }
        }

        yield return StartCoroutine(DoCreateNewUser(username, onComplete));
    }

    private IEnumerator DoCreateNewUser(string username, Action<UserData> onComplete)
    {
        string finalUsername = _isAnonymous
            ? "Guest_" + UnityEngine.Random.Range(1000, 9999)
            : (!string.IsNullOrEmpty(username) ? username : "Player");

        string loginType = _isAnonymous ? "anonymous" : "email";
        string email     = _isAnonymous ? "anonymous" : _userEmail;

        var userData = new UserData
        {
            auth     = new AuthData    { loginType = loginType, email = email },
            profile  = new ProfileData { username = finalUsername, level = 0, score = 0, character = "" },
            settings = new SettingsData { notifications = true }
        };

        string json = JsonUtility.ToJson(userData, true);

        using (var req = BuildPutRequest(UserUrl(), json))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ DB PUT error: " + req.error);
                onComplete?.Invoke(null);
                yield break;
            }

            Debug.Log("✅ User créé: " + userData.profile.username);
            onComplete?.Invoke(userData);
        }
    }

    public void LoadUserData(Action<UserData> onComplete)
    {
        if (!_isReady) { InitializeWhenReady(() => LoadUserData(onComplete)); return; }
        if (string.IsNullOrEmpty(_localId)) { Debug.LogError("❌ localId vide"); onComplete?.Invoke(null); return; }
        StartCoroutine(DoLoadUserData(onComplete));
    }

    private IEnumerator DoLoadUserData(Action<UserData> onComplete)
    {
        using (var req = UnityWebRequest.Get(UserUrl()))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ DB GET error: " + req.error);
                onComplete?.Invoke(null);
                yield break;
            }

            string body = req.downloadHandler.text;
            if (string.IsNullOrEmpty(body) || body.Trim() == "null")
            {
                Debug.LogWarning("⚠️ User not found in DB");
                onComplete?.Invoke(null);
                yield break;
            }

            UserData data = SafeFromJson(body);
            Debug.Log($"✅ Chargé — {data?.profile?.username} | Level: {data?.profile?.level}");
            onComplete?.Invoke(data);
        }
    }

    public void UpdateScore(int newScore)     => StartCoroutine(DoPatch(UserUrl("profile/score"),          newScore.ToString()));
    public void UpdateLevel(int newLevel)     => StartCoroutine(DoPatch(UserUrl("profile/level"),          newLevel.ToString()));
    public void UpdateCharacter(string c)     => StartCoroutine(DoPatch(UserUrl("profile/character"),      "\"" + Escape(c) + "\""));
    public void UpdateUsername(string u)      => StartCoroutine(DoPatch(UserUrl("profile/username"),       "\"" + Escape(u) + "\""));
    public void UpdateNotifications(bool ena) => StartCoroutine(DoPatch(UserUrl("settings/notifications"), ena ? "true" : "false"));

    private IEnumerator DoPatch(string url, string jsonValue)
    {
        if (string.IsNullOrEmpty(_localId) || string.IsNullOrEmpty(_idToken)) yield break;

        using (var req = BuildPutRequest(url, jsonValue))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError("❌ DB PATCH error: " + req.error);
            else
                Debug.Log("✅ DB mis à jour: " + url);
        }
    }

    public void ConvertGuestToEmail(string uid, string email, Action<bool> onComplete = null)
    {
        StartCoroutine(DoConvertGuest(uid, email, onComplete));
    }

    private IEnumerator DoConvertGuest(string uid, string email, Action<bool> onComplete)
    {
        yield return StartCoroutine(DoPatch(UserUrl("auth/loginType"), "\"email\""));
        yield return StartCoroutine(DoPatch(UserUrl("auth/email"),     "\"" + Escape(email) + "\""));
        _isAnonymous = false;
        _userEmail   = email;
        Debug.Log("✅ Guest converti en email user");
        onComplete?.Invoke(true);
    }

    public void DeleteUserData()
    {
        if (string.IsNullOrEmpty(_localId)) return;
        StartCoroutine(DoDelete());
    }

    private IEnumerator DoDelete()
    {
        using (var req = UnityWebRequest.Delete(UserUrl()))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError("❌ DB DELETE error: " + req.error);
            else
                Debug.Log("✅ User data deleted");
        }
    }

    private UnityWebRequest BuildPutRequest(string url, string jsonBody)
    {
        var req             = new UnityWebRequest(url, "PUT");
        byte[] raw          = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler   = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    private UserData SafeFromJson(string json)
    {
        try { return JsonUtility.FromJson<UserData>(json); }
        catch (Exception e) { Debug.LogError("❌ JSON parse error: " + e.Message); return null; }
    }

    private string Escape(string s) =>
        s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

    [Serializable] public class UserData     { public AuthData auth; public ProfileData profile; public SettingsData settings; }
    [Serializable] public class AuthData     { public string loginType; public string email; }
    [Serializable] public class ProfileData  { public string username; public int level; public int score; public string character; }
    [Serializable] public class SettingsData { public bool notifications; }
}