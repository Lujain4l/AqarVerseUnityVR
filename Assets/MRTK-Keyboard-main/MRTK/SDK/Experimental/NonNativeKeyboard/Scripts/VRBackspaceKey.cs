using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

[RequireComponent(typeof(Button))]
public class VRBackspaceKey : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(HandleBackspace);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(HandleBackspace);
    }

    private void HandleBackspace()
    {
        TMP_InputField field = ShowKeyboard.ActiveInputField;

        if (field == null)
        {
            HUDLogger.Log("VRBackspaceKey: ActiveInputField is NULL");
            return;
        }

        if (string.IsNullOrEmpty(field.text))
        {
            HUDLogger.Log("VRBackspaceKey: nothing to delete");
            return;
        }

        // حذف آخر حرف
        string oldText = field.text;
        field.text = field.text.Substring(0, field.text.Length - 1);
        field.caretPosition = field.text.Length;

        HUDLogger.Log($"VRBackspaceKey: deleted 1 char. '{oldText}' -> '{field.text}'");
    }
}

