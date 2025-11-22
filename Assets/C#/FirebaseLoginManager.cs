using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseLoginManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;
    public UIManager uiManager;

    [Header("Scene Settings")]
    public string nextSceneName = "MainScene";

    private FirebaseAuth auth;
    private bool firebaseReady = false;

    private async void Start()
    {
        if (statusText != null)
        {
            statusText.text = "Checking Firebase...";
        }

        await InitFirebase();
    }

    private async Task InitFirebase()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (statusText != null)
                statusText.text = "Deps: " + dependencyStatus;

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firebaseReady = true;

                if (statusText != null)
                    statusText.text = "";
            }
            else
            {
                firebaseReady = false;

                string msg =
                    "Firebase not ready.\nDependencyStatus = " + dependencyStatus +
                    "\nCheck google-services.json & Android setup.";

                if (statusText != null)
                    statusText.text = msg;
            }
        }
        catch (Exception ex)
        {
            firebaseReady = false;

            if (statusText != null)
                statusText.text = "Firebase init exception:\n" + ex.Message;
        }
    }

    public async void OnLoginButtonPressed()
    {
        if (!firebaseReady)
        {
            if (statusText != null)
                statusText.text = "Firebase not ready yet. Please wait...";
            return;
        }

        string email = emailInput != null ? emailInput.text.Trim() : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (statusText != null)
                statusText.text = "Please fill in email and password.";
            return;
        }

        if (statusText != null)
            statusText.text = "Logging in...";

        try
        {
            var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
            var userCredential = await loginTask;

            FirebaseUser user = userCredential.User;

            if (statusText != null)
                statusText.text = "";

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

            if (statusText != null)
                statusText.text = displayMessage;

            // Reset after each failed login to keep behavior stable in VR
            ResetLoginUI();
        }
    }

    /// <summary>
    /// Clear fields and reset keyboard/focus after failed login.
    /// </summary>
    private void ResetLoginUI()
    {
        if (emailInput != null)
        {
            emailInput.text = "";
        }

        if (passwordInput != null)
        {
            passwordInput.text = "";
        }

        // Set email as active field
        if (emailInput != null)
        {
            ShowKeyboard.ActiveInputField = emailInput;

            emailInput.Select();
            emailInput.ActivateInputField();
        }

        // Reset the MRTK keyboard instance safely
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
