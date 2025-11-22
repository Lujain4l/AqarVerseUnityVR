using Microsoft.MixedReality.Toolkit.Experimental.UI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VRCloseKeyboardKey : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(HandleClose);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(HandleClose);
    }

    private void HandleClose()
    {
        HUDLogger.Log("VRCloseKeyboardKey: Close keyboard");

        ShowKeyboard.ActiveInputField = null;

        if (NonNativeKeyboard.Instance != null)
        {
            NonNativeKeyboard.Instance.Close();
        }
    }
}
