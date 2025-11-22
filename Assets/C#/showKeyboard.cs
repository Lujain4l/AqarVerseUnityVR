using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class ShowKeyboard : MonoBehaviour
{
    private TMP_InputField inputField;

    public static TMP_InputField ActiveInputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    private void Start()
    {
        inputField.onSelect.AddListener(OnSelected);
    }

    private void OnDestroy()
    {
        inputField.onSelect.RemoveListener(OnSelected);
    }

    private void OnSelected(string _)
    {
        ActiveInputField = inputField;

        if (NonNativeKeyboard.Instance == null)
        {
            HUDLogger.Log("ShowKeyboard: NonNativeKeyboard.Instance is NULL");
            return;
        }

        HUDLogger.Log("ShowKeyboard: Selected field = " + inputField.name);

        NonNativeKeyboard.Instance.InputField = inputField;
        NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);
    }
}
