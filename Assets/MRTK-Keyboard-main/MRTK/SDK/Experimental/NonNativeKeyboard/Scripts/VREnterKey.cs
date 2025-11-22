using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VREnterKey : MonoBehaviour
{
    [Header("Assign the password field here")]
    public TMP_InputField passwordField;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(HandleEnter);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(HandleEnter);
    }

    private void HandleEnter()
    {
        TMP_InputField active = ShowKeyboard.ActiveInputField;

        if (active == null)
        {
            HUDLogger.Log("VREnterKey: ActiveInputField is NULL");
            return;
        }

        // لو نحن فعلاً في حقل الباسورد → نفّذ تسجيل الدخول
        if (passwordField != null && active == passwordField)
        {
            HUDLogger.Log("VREnterKey: Enter on password field -> Login");

            FirebaseLoginManager login = FindObjectOfType<FirebaseLoginManager>();
            if (login != null)
            {
                login.OnLoginButtonPressed();
            }
            else
            {
                HUDLogger.Log("VREnterKey: FirebaseLoginManager not found");
            }
        }
        else if (passwordField != null)
        {
            // لو نحن في الإيميل أو أي حقل آخر → انتقل للباسورد وافتح الكيبورد عليه
            HUDLogger.Log("VREnterKey: Enter on email -> focus password field");

            ShowKeyboard.ActiveInputField = passwordField;
            passwordField.Select();
            passwordField.ActivateInputField();

            if (NonNativeKeyboard.Instance != null)
            {
                NonNativeKeyboard.Instance.InputField = passwordField;
                NonNativeKeyboard.Instance.PresentKeyboard(passwordField.text);
            }
        }
        else
        {
            HUDLogger.Log("VREnterKey: passwordField is NULL");
        }
    }
}
