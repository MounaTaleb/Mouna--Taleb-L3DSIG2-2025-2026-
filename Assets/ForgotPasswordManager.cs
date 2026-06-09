using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

// ============================================================
//  ForgotPasswordManager
//
//  Panel 1 — ForgotPasswordPanel :
//    - EmailField (input)
//    - SendButton  → envoie le reset email
//    - ReturnButton → retour Login
//
//  Panel 2 — VerifyEmailPanel (partagé avec signup) :
//    - MessageText  → affiche le résultat (succès ou erreur)
//    - ResendButton → renvoie l'email
//    - OkButton     → retour Login
// ============================================================

public class ForgotPasswordManager : MonoBehaviour
{
    [Header("── Forgot Password Panel ──")]
    public TMP_InputField emailField;
    public Button sendButton;
    public Button returnButton;

    [Header("── Verify Email Panel (résultat) ──")]
    public TextMeshProUGUI messageText;  // texte dans le panel "Verify Email"
    public Button resendButton;          // bouton "Resend"
    public Button okButton;              // bouton "OK"

    [Header("── References ──")]
    public UIManager uiManager;

    private FirebaseAuth auth;
    private string lastEmailSent = ""; // garder l'email pour le Resend

    // ══════════════════════════════════════════
    private void Start()
    {
        // Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
                auth = FirebaseAuth.DefaultInstance;
            else
                Debug.LogError("❌ Firebase ERROR : " + task.Result);
        });

        // Wiring boutons Forgot Password Panel
        if (sendButton != null)   sendButton.onClick.AddListener(SendResetEmail);
        if (returnButton != null) returnButton.onClick.AddListener(GoBack);

        // Wiring boutons Verify Email Panel
        if (resendButton != null) resendButton.onClick.AddListener(ResendResetEmail);
        if (okButton != null)     okButton.onClick.AddListener(GoBackFromVerify);
    }

    // ══════════════════════════════════════════
    //  ENVOYER EMAIL RESET
    // ══════════════════════════════════════════
    private void SendResetEmail()
    {
        string email = emailField.text.Trim();

        // Validations
        if (string.IsNullOrEmpty(email))
        {
            ShowVerifyPanel("Veuillez entrer votre email.");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowVerifyPanel("Email invalide.");
            return;
        }

        if (auth == null)
        {
            ShowVerifyPanel("Firebase non prêt. Réessayez.");
            return;
        }

        // Désactiver bouton pendant l'envoi
        sendButton.interactable = false;

        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            sendButton.interactable = true;

            if (task.IsFaulted)
            {
                var ex = task.Exception?.GetBaseException() as FirebaseException;
                string errorMsg = GetErrorMessage((AuthError)ex.ErrorCode);
                Debug.LogError("❌ Reset email error : " + ex?.Message);
                ShowVerifyPanel(errorMsg);
            }
            else
            {
                lastEmailSent = email;
                Debug.Log("✅ Email de reset envoyé à : " + email);
                ShowVerifyPanel("Email envoyé à " + email +
                                ".\nVérifiez votre boîte mail (et spam).\nCliquez sur le lien pour changer votre mot de passe.");
            }
        });
    }

    // ══════════════════════════════════════════
    //  RENVOYER EMAIL RESET (bouton Resend)
    // ══════════════════════════════════════════
    private void ResendResetEmail()
    {
        if (string.IsNullOrEmpty(lastEmailSent))
        {
            messageText.text = "Aucun email à renvoyer.";
            return;
        }

        if (auth == null)
        {
            messageText.text = "Firebase non prêt. Réessayez.";
            return;
        }

        resendButton.interactable = false;
        messageText.text = "Envoi en cours...";

        auth.SendPasswordResetEmailAsync(lastEmailSent).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                var ex = task.Exception?.GetBaseException() as FirebaseException;
                messageText.text = "Erreur : " + GetErrorMessage((AuthError)ex.ErrorCode);
                resendButton.interactable = true;
            }
            else
            {
                messageText.text = "Email renvoyé à " + lastEmailSent +
                                   ".\nVérifiez votre boîte mail (et spam).";
                Debug.Log("✅ Email reset renvoyé à : " + lastEmailSent);

                // Réactiver Resend après 3 secondes
                StartCoroutine(ReenableResendAfter(3f));
            }
        });
    }

    private System.Collections.IEnumerator ReenableResendAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resendButton != null) resendButton.interactable = true;
    }

    // ══════════════════════════════════════════
    //  NAVIGATION
    // ══════════════════════════════════════════

    // Retour depuis Forgot Password Panel → Login
    private void GoBack()
    {
        EnsureUIManager();
        emailField.text = "";
        uiManager?.ShowLogin();
    }

    // Retour depuis Verify Email Panel → Login
    private void GoBackFromVerify()
    {
        EnsureUIManager();
        uiManager?.ShowLogin();
    }

    // ══════════════════════════════════════════
    //  AFFICHER LE PANEL VERIFY EMAIL
    //  (utilisé pour succès ET erreur)
    // ══════════════════════════════════════════
    private void ShowVerifyPanel(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (resendButton != null)
            resendButton.interactable = true;

        EnsureUIManager();
        uiManager?.ShowVerifyEmail();
    }

    // ══════════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════════
    private bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private string GetErrorMessage(AuthError error)
    {
        switch (error)
        {
            case AuthError.InvalidEmail:         return "Email invalide.";
            case AuthError.UserNotFound:         return "Aucun compte trouvé avec cet email.";
            case AuthError.NetworkRequestFailed: return "Problème de connexion Internet.";
            case AuthError.TooManyRequests:      return "Trop de tentatives. Réessayez plus tard.";
            default:                             return "Erreur. Réessayez.";
        }
    }

    private void EnsureUIManager()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
    }
}