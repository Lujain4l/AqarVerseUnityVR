using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the Login scene.
/// Initializes Firebase, handles sign-in, and persists across scenes via DontDestroyOnLoad.
///
/// NOTE: Delete FirebaseInit.cs — this script already handles Firebase initialization.
///       Having two scripts call CheckAndFixDependenciesAsync() is redundant.
/// </summary>
public class FirebaseLoginManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Singleton
    // ─────────────────────────────────────────────

    public static FirebaseLoginManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────

    [Header("UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;
    public UIManager uiManager;

    [Header("Scene Settings")]
    public string nextSceneName = "MainScene";

    // ─────────────────────────────────────────────
    // Public State (readable from other scripts)
    // ─────────────────────────────────────────────

    /// <summary>True once Firebase SDK is ready.</summary>
    public bool IsFirebaseReady { get; private set; } = false;

    /// <summary>
    /// The currently signed-in Firebase user.
    /// Prefer FirebaseAuth.DefaultInstance.CurrentUser directly in other scripts —
    /// this is provided as a convenience accessor.
    /// </summary>
    public FirebaseUser CurrentUser => auth?.CurrentUser;

    // ─────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────

    private FirebaseAuth auth;

    // ─────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────

    private async void Awake()
    {
        // ── Singleton with scene persistence ──
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetStatus("Checking Firebase...");
        await InitFirebase();
    }

    // ─────────────────────────────────────────────
    // Firebase Initialization (single location)
    // ─────────────────────────────────────────────

    private async Task InitFirebase()
    {
        try
        {
            DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                IsFirebaseReady = true;
                SetStatus("");
                Debug.Log("[FirebaseLoginManager] Firebase is ready.");
            }
            else
            {
                IsFirebaseReady = false;
                string msg = $"Firebase dependencies not resolved: {dependencyStatus}. " +
                              "Check google-services.json and Android setup.";
                SetStatus(msg);
                Debug.LogError($"[FirebaseLoginManager] {msg}");
            }
        }
        catch (Exception ex)
        {
            IsFirebaseReady = false;
            SetStatus("Firebase init error:\n" + ex.Message);
            Debug.LogError($"[FirebaseLoginManager] Init exception: {ex}");
        }
    }

    // ─────────────────────────────────────────────
    // Login
    // ─────────────────────────────────────────────

    public async void OnLoginButtonPressed()
    {
        if (!IsFirebaseReady)
        {
            SetStatus("Firebase not ready yet. Please wait...");
            return;
        }

        string email    = emailInput    != null ? emailInput.text.Trim() : string.Empty;
        string password = passwordInput != null ? passwordInput.text     : string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please fill in email and password.");
            return;
        }

        SetStatus("Logging in...");

        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            Debug.Log($"[FirebaseLoginManager] Signed in: {result.User.UserId} ({result.User.Email})");

            SetStatus("");

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else if (uiManager != null)
            {
                uiManager.ShowMainUI();
            }
        }
        catch (Exception ex)
        {
            string displayMessage = "Login failed. Please check your email and password.";
            string msg = ex.Message ?? "";

            if (msg.Contains("badly formatted"))
                displayMessage = "Please enter a valid email address.";
            else if (msg.Contains("INVALID_LOGIN_CREDENTIALS") || msg.Contains("WRONG_PASSWORD"))
                displayMessage = "Incorrect email or password.";
            else if (msg.Contains("EMAIL_NOT_FOUND") || msg.Contains("USER_NOT_FOUND"))
                displayMessage = "No user found with this email.";
            else if (msg.ToLower().Contains("network"))
                displayMessage = "Network error. Check your internet connection.";

            Debug.LogError($"[FirebaseLoginManager] Login failed: {ex.Message}");
            SetStatus(displayMessage);
            ResetLoginUI();
        }
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void ResetLoginUI()
    {
        if (emailInput != null)    emailInput.text    = "";
        if (passwordInput != null) passwordInput.text = "";

        if (emailInput != null)
        {
            ShowKeyboard.ActiveInputField = emailInput;
            emailInput.Select();
            emailInput.ActivateInputField();
        }

        if (NonNativeKeyboard.Instance != null)
        {
            NonNativeKeyboard.Instance.Close();

            if (emailInput != null)
            {
                NonNativeKeyboard.Instance.InputField = emailInput;
                NonNativeKeyboard.Instance.PresentKeyboard(emailInput.text);
            }
        }
    }
}