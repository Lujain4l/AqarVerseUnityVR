using Microsoft.MixedReality.Toolkit.Experimental.UI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeyDebugLogger : MonoBehaviour
{
    public string keyName;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(LogKeyPress);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(LogKeyPress);
    }

    private void LogKeyPress()
    {
        string nameToShow = string.IsNullOrEmpty(keyName) ? gameObject.name : keyName;

        string fieldName = "NULL";
        string fieldText = "";
        if (ShowKeyboard.ActiveInputField != null)
        {
            fieldName = ShowKeyboard.ActiveInputField.name;
            fieldText = ShowKeyboard.ActiveInputField.text;
        }

        HUDLogger.Log(
            "KeyDebugLogger: " + nameToShow +
            " clicked. ActiveInputField = " + fieldName +
            ", Text = '" + fieldText + "'"
        );
    }
}
