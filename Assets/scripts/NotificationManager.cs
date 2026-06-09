using UnityEngine;
using Unity.Notifications.Android;
using UnityEngine.Android;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    private string[] tips = {
        // ── Mots de passe & Accès ──────────────────────────────
        "🔑 Active toujours la double authentification — même un mot de passe volé ne suffit plus.",
        "🔑 Ne réutilise jamais le même mot de passe — une seule fuite expose tous tes comptes.",
        "🔑 Un mot de passe long est plus sûr qu'un mot de passe complexe — préfère une phrase secrète.",

        // ── Phishing ───────────────────────────────────────────
        "🎣 Avant de cliquer un lien, vérifie toujours la vraie URL — les faux sites imitent à 99%.",
        "🎣 Un email urgent qui demande une action immédiate est presque toujours du phishing.",
        "🎣 Méfie-toi des pièces jointes inattendues — même venant d'un collègue connu.",

        // ── Mises à jour ───────────────────────────────────────
        "🛡️ Mets à jour dès que possible — la plupart des attaques exploitent des failles déjà corrigées.",
        "🛡️ Désactive les services que tu n'utilises pas — chaque port ouvert est une surface d'attaque.",
        "🛡️ Utilise uniquement des logiciels officiels — les versions crackées contiennent souvent des backdoors.",

        // ── Réseaux ────────────────────────────────────────────
        "📡 Sur un WiFi public, considère que tout ton trafic non chiffré est visible par n'importe qui.",
        "📡 Change les identifiants par défaut de tous tes équipements réseau dès l'installation.",
        "📡 Segmente ton réseau — un appareil compromis ne doit pas atteindre tous les autres.",

        // ── Sauvegardes ────────────────────────────────────────
        "💾 Règle 3-2-1 : 3 copies, 2 supports différents, 1 hors site — seule vraie protection contre le ransomware.",
        "💾 Teste régulièrement tes sauvegardes — une sauvegarde non testée est une sauvegarde inutile.",
        "💾 Isole tes sauvegardes du réseau — un ransomware cherche et chiffre les sauvegardes en premier.",

        // ── Comportement humain ────────────────────────────────
        "🧠 Le maillon le plus faible d'un système sécurisé est presque toujours humain — pas technique.",
        "🧠 Ne branche jamais une clé USB trouvée — c'est une technique d'intrusion classique et efficace.",
        "🧠 Moins tu partages d'infos personnelles en ligne, moins tu es vulnérable au ciblage.",
    };

    // ─────────────────────────────────────────────────────────────
    //  SINGLETON + DONT DESTROY ON LOAD
    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    //  START
    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        RegisterChannel();
        RequestPermission();
        ScheduleAllNotifications();
    }

    // ─────────────────────────────────────────────────────────────
    //  CHANNEL
    // ─────────────────────────────────────────────────────────────
    void RegisterChannel()
    {
        AndroidNotificationCenter.RegisterNotificationChannel(
            new AndroidNotificationChannel
            {
                Id          = "security_app",
                Name        = "Sécurité & Rappels",
                Importance  = Importance.High,
                Description = "Conseils malware et rappels quotidiens"
            });
    }

    // ─────────────────────────────────────────────────────────────
    //  PERMISSION
    // ─────────────────────────────────────────────────────────────
    void RequestPermission()
    {
        if (!Permission.HasUserAuthorizedPermission(
                "android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission(
                "android.permission.POST_NOTIFICATIONS");
        }

        Debug.Log("🔔 Permission status : " +
            Permission.HasUserAuthorizedPermission(
                "android.permission.POST_NOTIFICATIONS"));
    }

    // ─────────────────────────────────────────────────────────────
    //  PLANIFIER TOUT
    // ─────────────────────────────────────────────────────────────
    void ScheduleAllNotifications()
    {
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        ScheduleTip();
        SchedulePlayReminder();
        ScheduleInactivity();
        ScheduleTestExtras();

        PlayerPrefs.SetString("LastOpen", System.DateTime.Now.ToString());
        PlayerPrefs.Save();

        Debug.Log("✅ Toutes les notifications planifiées !");
    }

    // ─────────────────────────────────────────────────────────────
    //  CONSEIL DU JOUR
    // ─────────────────────────────────────────────────────────────
    void ScheduleTip()
    {
        int seed  = System.DateTime.Now.DayOfYear +
                    System.DateTime.Now.Year * 1000;
        Random.InitState(seed);
        int index = Random.Range(0, tips.Length);

        // ⚠️ MODE TEST — notif dans 1 minute
        var fire = System.DateTime.Now.AddMinutes(1);
        Debug.Log("🎓 Conseil prévu pour : " + fire.ToString());

        Send(
            "🎓 Conseil Sécurité du Jour",
            tips[index],
            fire,
            System.TimeSpan.FromDays(1)
        );
    }

    // ─────────────────────────────────────────────────────────────
    //  RAPPEL DE JEU
    // ─────────────────────────────────────────────────────────────
    void SchedulePlayReminder()
    {
        // ⚠️ MODE TEST — notif dans 2 minutes
        var fire = System.DateTime.Now.AddMinutes(2);
        Debug.Log("🎮 Rappel prévu pour : " + fire.ToString());

        Send(
            "🎮 C'est l'heure de jouer !",
            "Ton agent t'attend — viens te connecter !",
            fire,
            System.TimeSpan.FromDays(1)
        );
    }

    // ─────────────────────────────────────────────────────────────
    //  INACTIVITÉ
    // ─────────────────────────────────────────────────────────────
    void ScheduleInactivity()
    {
        // ⚠️ MODE TEST — notif dans 3 minutes
        var fire = System.DateTime.Now.AddMinutes(3);
        Debug.Log("😴 Inactivité prévue pour : " + fire.ToString());

        Send(
            "😴 Tu nous manques !",
            "Ça fait un moment que tu n'as pas joué — reviens défendre le réseau !",
            fire,
            System.TimeSpan.FromDays(2)
        );
    }

    // ─────────────────────────────────────────────────────────────
    //  ✅ NOTIFS SUPPLÉMENTAIRES TEST
    // ─────────────────────────────────────────────────────────────
    void ScheduleTestExtras()
    {
        // Notif dans 6 min — Bienvenue
        Send(
            "👋 Bienvenue dans CyberAgent !",
            "Prêt à défendre le réseau ? Lance une mission maintenant !",
            System.DateTime.Now.AddMinutes(6),
            null
        );

        // Notif dans 7 min — Défi quotidien
        Send(
            "🏆 Défi du jour disponible !",
            "Un nouveau défi t'attend — gagne des points bonus aujourd'hui !",
            System.DateTime.Now.AddMinutes(7),
            null
        );

        // Notif dans 8 min — Conseil rapide
        Send(
            "💡 Le savais-tu ?",
            "90% des cyberattaques commencent par un simple email. Reste vigilant !",
            System.DateTime.Now.AddMinutes(8),
            null
        );

        // Notif dans 9 min — Récompense
        Send(
            "🎁 Récompense disponible !",
            "Tu as une récompense qui t'attend — connecte-toi pour la récupérer !",
            System.DateTime.Now.AddMinutes(9),
            null
        );

        Debug.Log("🧪 Notifs de test supplémentaires planifiées (6-9 min)");
    }

    // ─────────────────────────────────────────────────────────────
    //  SEND
    // ─────────────────────────────────────────────────────────────
    void Send(string title, string text, System.DateTime fire, System.TimeSpan? repeat)
    {
        var notif = new AndroidNotification
        {
            Title     = title,
            Text      = text,
            SmallIcon = "icon_small",
            LargeIcon = "icon_0",
            FireTime  = fire
        };

        if (repeat.HasValue)
            notif.RepeatInterval = repeat.Value;

        AndroidNotificationCenter.SendNotification(notif, "security_app");
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────────────────────
    System.DateTime Today(int hour, int minute)
    {
        var now = System.DateTime.Now;
        return new System.DateTime(
                   now.Year, now.Month, now.Day, hour, minute, 0);
    }
}