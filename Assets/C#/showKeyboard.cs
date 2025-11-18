using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class showKeyboard : MonoBehaviour
{

    private TMP_InputField inputField;

    // Start is called before the first frame update
    void Start()
    {

        inputField = GetComponent<TMP_InputField>();

        // Add listener for when the input field is selected
        inputField.onSelect.AddListener(OpenKeyboard);

    }
    public void OpenKeyboard(string text)
    {
        NonNativeKeyboard.Instance.InputField = inputField;
        NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);
    }
    
}
