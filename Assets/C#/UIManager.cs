using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject mainUI;
    public GameObject howToUseUI;


    // Show Main UI and hide How-To-Use UI
    public void ShowMainUI()
    {
        mainUI.SetActive(true);
        howToUseUI.SetActive(false);
    }

    // Show How-To-Use UI and hide Main UI
    public void ShowHowToUseUI()
    {
        mainUI.SetActive(false);
        howToUseUI.SetActive(true);
    }

    //  Hide everything
    public void HideAllUI()
    {
        mainUI.SetActive(false);
        howToUseUI.SetActive(false);
    }

}
