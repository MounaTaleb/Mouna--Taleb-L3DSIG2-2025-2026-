using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class PlaceObject : MonoBehaviour
{
    public GameObject[] prefabs; // [0]=BIOS [1]=RAM [2]=CPU [3]=GPU [4]=SSD
    public float distanceFromCamera = 1.5f;

    private Camera arCamera;
    private GameObject spawnedObject;
    private bool isPlaced = false;
    private int currentIndex = 0;

    // ── Rotation ──────────────────────────────────────
    private bool    isDragging     = false;
    private Vector2 lastTouchPos   = Vector2.zero;
    private float   rotationSpeedY = 0.3f;
    private float   rotationSpeedX = 0.3f;
    private float   rotX           = -90f;
    private float   rotY           = 180f;

    // ── Donnees ───────────────────────────────────────
    private string[] titres = { "BIOS", "RAM", "CPU", "GPU", "SSD" };

    private string[,] phrases = {
        {
            "Le BIOS est le premier programme\nqui s'allume quand tu demarres\nton ordinateur.",
            "Il verifie que le clavier,\nl'ecran et la memoire\nfonctionnent bien.",
            "Sans le BIOS, l'ordi ne peut\npas demarrer ni charger\nWindows."
        },
        {
            "La RAM est la memoire vive\nde l'ordinateur. Elle stocke\nles donnees temporaires.",
            "Plus tu as de RAM, plus tu peux\nouvrir des applications\nen meme temps.",
            "La RAM se vide completement\nquand tu eteins\nton ordinateur."
        },
        {
            "Le CPU est le cerveau\nde l'ordinateur. Il execute\ntoutes les instructions.",
            "Il calcule des milliards\nd'operations par seconde\ngrace a ses coeurs.",
            "Un CPU puissant rend\nton ordinateur plus rapide\npour toutes les taches."
        },
        {
            "Le GPU est la carte graphique.\nIl affiche les images\nsur ton ecran.",
            "Il est specialise pour\ntraiter des milliers de\ncalculs en parallele.",
            "Sans GPU, les jeux video\net les videos ne pourraient\npas s'afficher correctement."
        },
        {
            "Le SSD est un disque dur\nrapide qui stocke tes\nfichiers et Windows.",
            "Il est 10x plus rapide\nqu'un disque dur classique\n(HDD).",
            "Avec un SSD, ton ordi\ndемarre en quelques\nsecondes seulement."
        }
    };

    private TextMeshProUGUI txtTitre;
    private TextMeshProUGUI txtPhrase1;
    private TextMeshProUGUI txtPhrase2;
    private TextMeshProUGUI txtPhrase3;
    private TextMeshProUGUI txtCompteur;

    void Start()
    {
        arCamera = Camera.main;
        // ✅ Lit le composant choisi depuis level 1
        currentIndex = PlayerPrefs.GetInt("Composant", 0);
        CreerPanelUI();
        SpawnerObjet();
    }

    void SpawnerObjet()
    {
        if (spawnedObject != null)
            Destroy(spawnedObject);

        isPlaced = false;
        rotX = -90f;
        rotY = 180f;

        Vector3 pos = arCamera.transform.position
                    + arCamera.transform.forward * distanceFromCamera;

        spawnedObject = Instantiate(prefabs[currentIndex], pos,
            Quaternion.Euler(rotX, rotY, 0f));

        MettreAJourTexte();
    }

    void MettreAJourTexte()
    {
        txtTitre.text    = "<b>" + titres[currentIndex] + "</b>";
        txtPhrase1.text  = phrases[currentIndex, 0];
        txtPhrase2.text  = phrases[currentIndex, 1];
        txtPhrase3.text  = phrases[currentIndex, 2];
        txtCompteur.text = (currentIndex + 1) + " / " + titres.Length;
    }

    void CreerPanelUI()
    {
        GameObject canvasObj = new GameObject("ARCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ── Panel GAUCHE ──────────────────────────────
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        panel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.1f, 0.88f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0f, 0f);
        panelRect.anchorMax        = new Vector2(0f, 1f);
        panelRect.pivot            = new Vector2(0f, 0.5f);
        panelRect.sizeDelta        = new Vector2(620, 0);
        panelRect.anchoredPosition = Vector2.zero;

        // ── Ligne accent droite ───────────────────────
        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(panel.transform, false);
        accent.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 1f);
        RectTransform ar = accent.GetComponent<RectTransform>();
        ar.anchorMin        = new Vector2(1, 0);
        ar.anchorMax        = new Vector2(1, 1);
        ar.pivot            = new Vector2(1, 0.5f);
        ar.sizeDelta        = new Vector2(5, 0);
        ar.anchoredPosition = Vector2.zero;

        txtTitre = AjouterTexte(panel, "",
            fontSize: 68, couleur: Color.white,
            posY: -40f, hauteur: 90f);

        CreerSep(panel, -140f);

        txtPhrase1 = AjouterTexte(panel, "",
            fontSize: 34, couleur: new Color(0.75f, 0.88f, 1f),
            posY: -160f, hauteur: 130f);

        CreerSep(panel, -300f);

        txtPhrase2 = AjouterTexte(panel, "",
            fontSize: 34, couleur: new Color(0.75f, 0.88f, 1f),
            posY: -320f, hauteur: 130f);

        CreerSep(panel, -460f);

        txtPhrase3 = AjouterTexte(panel, "",
            fontSize: 34, couleur: new Color(0.75f, 0.88f, 1f),
            posY: -480f, hauteur: 130f);

        CreerBadgeControles(panel);

        // ── Compteur ──────────────────────────────────
        GameObject cptObj = new GameObject("Compteur");
        cptObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI tc = cptObj.AddComponent<TextMeshProUGUI>();
        txtCompteur         = tc;
        tc.text             = "1 / 5";
        tc.fontSize         = 32;
        tc.alignment        = TextAlignmentOptions.Center;
        tc.color            = Color.white;
        RectTransform cptRect = cptObj.GetComponent<RectTransform>();
        cptRect.anchorMin        = new Vector2(0.5f, 0f);
        cptRect.anchorMax        = new Vector2(0.5f, 0f);
        cptRect.pivot            = new Vector2(0.5f, 0f);
        cptRect.sizeDelta        = new Vector2(120, 85);
        cptRect.anchoredPosition = new Vector2(0f, 30f);

        // ── Bouton PRECEDANT ──────────────────────────
        CreerBoutonNav(canvasObj, "< Precedant", 0.35f,
            () => {
                currentIndex = (currentIndex - 1 + titres.Length) % titres.Length;
                SpawnerObjet();
            });

        // ── Bouton SUIVANT ────────────────────────────
        CreerBoutonNav(canvasObj, "Suivant >", 0.65f,
            () => {
                currentIndex = (currentIndex + 1) % titres.Length;
                SpawnerObjet();
            });

        // ── Bouton RETOUR vers level 1 (haut droite) ──
        GameObject btnRetour = new GameObject("BtnRetour");
        btnRetour.transform.SetParent(canvasObj.transform, false);
        btnRetour.AddComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f, 0.92f);

        RectTransform rr = btnRetour.GetComponent<RectTransform>();
        rr.anchorMin        = new Vector2(1f, 1f);
        rr.anchorMax        = new Vector2(1f, 1f);
        rr.pivot            = new Vector2(1f, 1f);
        rr.sizeDelta        = new Vector2(220, 80);
        rr.anchoredPosition = new Vector2(-20f, -20f);

        Button bRetour = btnRetour.AddComponent<Button>();
        bRetour.onClick.AddListener(() => {
            SceneManager.LoadScene(2); // ✅ level 1 final
        });

        GameObject txtRetour = new GameObject("Txt");
        txtRetour.transform.SetParent(btnRetour.transform, false);
        TextMeshProUGUI tRetour = txtRetour.AddComponent<TextMeshProUGUI>();
        tRetour.text      = "< Retour";
        tRetour.fontSize  = 28;
        tRetour.alignment = TextAlignmentOptions.Center;
        tRetour.color     = Color.white;
        RectTransform trr = txtRetour.GetComponent<RectTransform>();
        trr.anchorMin = Vector2.zero;
        trr.anchorMax = Vector2.one;
        trr.sizeDelta = Vector2.zero;
    }

    void CreerBoutonNav(GameObject canvas, string label,
                        float anchorX, System.Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(canvas.transform, false);
        btnObj.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 0.92f);

        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(anchorX, 0f);
        r.anchorMax        = new Vector2(anchorX, 0f);
        r.pivot            = new Vector2(0.5f, 0f);
        r.sizeDelta        = new Vector2(260, 85);
        r.anchoredPosition = new Vector2(0f, 30f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.1f, 0.4f, 0.9f);
        cb.pressedColor     = new Color(0.05f, 0.25f, 0.7f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        GameObject txtObj = new GameObject("Txt");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI t = txtObj.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = 28;
        t.alignment = TextAlignmentOptions.Center;
        t.color     = Color.white;
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;
    }

    void CreerBadgeControles(GameObject panel)
    {
        GameObject badge = new GameObject("Badge");
        badge.transform.SetParent(panel.transform, false);
        badge.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 0.15f);

        RectTransform br = badge.GetComponent<RectTransform>();
        br.anchorMin        = new Vector2(0.04f, 0f);
        br.anchorMax        = new Vector2(0.96f, 0f);
        br.pivot            = new Vector2(0.5f, 0f);
        br.sizeDelta        = new Vector2(0, 160);
        br.anchoredPosition = new Vector2(0, 210);

        GameObject ligne = new GameObject("LigneBadge");
        ligne.transform.SetParent(badge.transform, false);
        ligne.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 0.8f);
        RectTransform lb = ligne.GetComponent<RectTransform>();
        lb.anchorMin        = new Vector2(0, 1);
        lb.anchorMax        = new Vector2(1, 1);
        lb.pivot            = new Vector2(0.5f, 1f);
        lb.sizeDelta        = new Vector2(0, 2);
        lb.anchoredPosition = Vector2.zero;

        GameObject titre = new GameObject("TitreBadge");
        titre.transform.SetParent(badge.transform, false);
        TextMeshProUGUI tt = titre.AddComponent<TextMeshProUGUI>();
        tt.text      = "<b>CONTROLES</b>";
        tt.fontSize  = 22;
        tt.color     = new Color(0.2f, 0.6f, 1f);
        tt.alignment = TextAlignmentOptions.Left;
        RectTransform ttr = titre.GetComponent<RectTransform>();
        ttr.anchorMin        = new Vector2(0, 1);
        ttr.anchorMax        = new Vector2(1, 1);
        ttr.pivot            = new Vector2(0.5f, 1f);
        ttr.sizeDelta        = new Vector2(-30, 35);
        ttr.anchoredPosition = new Vector2(15, -10);

        CreerLigneControle(badge, new Color(0.2f, 0.6f, 1f),   "Glisser   ->   Rotation",     -52f);
        CreerLigneControle(badge, new Color(0.4f, 0.9f, 0.6f), "Tap       ->   Poser / Lever", -100f);
    }

    void CreerLigneControle(GameObject parent, Color couleur, string texte, float posY)
    {
        GameObject dot = new GameObject("Dot");
        dot.transform.SetParent(parent.transform, false);
        dot.AddComponent<Image>().color = couleur;
        RectTransform dr = dot.GetComponent<RectTransform>();
        dr.anchorMin        = new Vector2(0, 1);
        dr.anchorMax        = new Vector2(0, 1);
        dr.pivot            = new Vector2(0, 1f);
        dr.sizeDelta        = new Vector2(18, 18);
        dr.anchoredPosition = new Vector2(18, posY);

        GameObject txt = new GameObject("TxtLigne");
        txt.transform.SetParent(parent.transform, false);
        TextMeshProUGUI t = txt.AddComponent<TextMeshProUGUI>();
        t.text      = texte;
        t.fontSize  = 24;
        t.color     = new Color(0.85f, 0.95f, 1f);
        t.alignment = TextAlignmentOptions.Left;
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin        = new Vector2(0, 1);
        tr.anchorMax        = new Vector2(1, 1);
        tr.pivot            = new Vector2(0.5f, 1f);
        tr.sizeDelta        = new Vector2(-50, 40);
        tr.anchoredPosition = new Vector2(30, posY);
    }

    TextMeshProUGUI AjouterTexte(GameObject parent, string texte, float fontSize,
                                  Color couleur, float posY, float hauteur)
    {
        GameObject obj = new GameObject("Txt");
        obj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text               = texte;
        tmp.fontSize           = fontSize;
        tmp.alignment          = TextAlignmentOptions.Left;
        tmp.color              = couleur;
        tmp.lineSpacing        = 6;
        tmp.enableWordWrapping = true;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(0, 1);
        r.anchorMax        = new Vector2(1, 1);
        r.pivot            = new Vector2(0.5f, 1f);
        r.sizeDelta        = new Vector2(-40, hauteur);
        r.anchoredPosition = new Vector2(20, posY);
        return tmp;
    }

    void CreerSep(GameObject parent, float posY)
    {
        GameObject sep = new GameObject("Sep");
        sep.transform.SetParent(parent.transform, false);
        sep.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        RectTransform r = sep.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.02f, 1);
        r.anchorMax        = new Vector2(0.98f, 1);
        r.pivot            = new Vector2(0.5f, 1f);
        r.sizeDelta        = new Vector2(0, 2);
        r.anchoredPosition = new Vector2(0, posY);
    }

    void Update()
    {
        Vector3 centerPos = arCamera.transform.position
                          + arCamera.transform.forward * distanceFromCamera;

        if (spawnedObject == null) return;

        if (!isPlaced)
        {
            spawnedObject.transform.position = centerPos;
            spawnedObject.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            if (touch.phase == TouchPhase.Began)
            {
                isDragging   = false;
                lastTouchPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                isDragging = true;
                Vector2 delta = touch.position - lastTouchPos;
                rotY += delta.x * rotationSpeedY;
                rotX += delta.y * rotationSpeedX;
                spawnedObject.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
                lastTouchPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended && !isDragging)
            {
                isPlaced = !isPlaced;
            }
        }

        if (isPlaced)
            spawnedObject.transform.position = centerPos;
    }
}