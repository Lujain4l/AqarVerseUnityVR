using TMPro;
using UnityEngine;

public static class HUDLogger
{
    // سيتم ربطه في FirebaseLoginManager بـ Error msg TextMeshProUGUI
    public static TextMeshProUGUI Output;

    public static void Log(string msg)
    {
        Debug.Log(msg);

        if (Output != null)
        {
            Output.text = msg;
        }
    }
}
