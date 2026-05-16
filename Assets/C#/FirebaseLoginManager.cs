using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the Login scene.
/// Initializes Firebase, handles sign-in, checks user role,
/// and routes to the correct scene (MainScene or CompanyScene).
///
/// NOTE: Delete FirebaseInit.cs — this script already handles Firebase initialization.
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
    [Tooltip("Scene loaded for regular customers.")]
    public string customerSceneName = "MainScene";

    [Tooltip("Scene loaded when the logged-in user has role == 'company'.")]
    public string companySceneName = "CompanyScene";

    // ─────────────────────────────────────────────
    // Public State
    // ─────────────────────────────────────────────

    /// <summary>True once Firebase SDK is ready.</summary>
    public bool IsFirebaseReady { get; private set; } = false;

    /// <summary>The currently signed-in Firebase user.</summary>
    public FirebaseUser CurrentUser => auth?.CurrentUser;

    /// <summary>
    /// The Firestore document data for the logged-in company user.
    /// Null if the user is a regular customer.
    /// Other scripts in CompanyScene can read this (e.g. CompanyPropertyManager).
    /// </summary>
    public static string LoggedInCompanyId { get; private set; } = null;

    // ─────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────

    private FirebaseAuth auth;
    private FirebaseFirestore db;

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
    // Firebase Initialization
    // ─────────────────────────────────────────────

    private async Task InitFirebase()
    {
        try
        {
            DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db   = FirebaseFirestore.DefaultInstance;
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

            SetStatus("Checking account type...");

            // ── Role check ──
            string role = await GetUserRole(result.User.UserId);
            Debug.Log($"[FirebaseLoginManager] User role: '{role}'");

            SetStatus("");

            if (role == "company")
            {
                SceneManager.LoadScene(companySceneName);
            }
            else
            {
                // Regular customer — or role not found — go to main scene
                if (!string.IsNullOrEmpty(customerSceneName))
                    SceneManager.LoadScene(customerSceneName);
                else if (uiManager != null)
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
    // Role Resolution
    // ─────────────────────────────────────────────

    /// <summary>
    /// Looks up the user's role from Firestore.
    ///
    /// Strategy:
    ///   1. Check the "company" collection for a document whose uid == userId.
    ///      (This is how the screenshot shows company accounts are stored.)
    ///   2. If not found there, fall back to the "Customer" collection.
    ///   3. Returns the value of the "role" field, or an empty string if not found.
    /// </summary>
    private async Task<string> GetUserRole(string userId)
    {
        if (db == null) return string.Empty;

        try
        {
            // ── Check company collection by document ID (uid is the doc ID) ──
            DocumentReference companyDocRef = db.Collection("company").Document(userId);
            DocumentSnapshot companySnapshot = await companyDocRef.GetSnapshotAsync();

            if (companySnapshot.Exists)
            {
                if (companySnapshot.TryGetValue("role", out string companyRole))
                {
                    // Cache the companyId so CompanyPropertyManager can use it
                    if (companySnapshot.TryGetValue("companyId", out string companyId))
                        LoggedInCompanyId = companyId;

                    return companyRole;
                }
            }

            // ── Fallback: check Customer collection ──
            DocumentReference customerDocRef = db.Collection("Customer").Document(userId);
            DocumentSnapshot customerSnapshot = await customerDocRef.GetSnapshotAsync();

            if (customerSnapshot.Exists &&
                customerSnapshot.TryGetValue("role", out string customerRole))
            {
                return customerRole;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FirebaseLoginManager] GetUserRole failed: {ex.Message}");
        }

        return string.Empty; // Default → treat as regular customer
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