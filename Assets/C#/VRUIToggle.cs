using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRUIToggle : MonoBehaviour
{
    public GameObject uiCanvas;

    private InputDevice rightController;

    // CHANGED: renamed because trigger uses float-based press detection
    private bool lastButtonState = false;

    void Start()
    {
        if (uiCanvas != null)
            uiCanvas.SetActive(false);

        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        if (!rightController.isValid)
        {
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }

        // CHANGED: trigger uses float instead of bool
        float triggerValue = 0f;

        // CHANGED: gripButton -> trigger (Axis1D.SecondaryIndexTrigger)
        if (rightController.TryGetFeatureValue(CommonUsages.trigger, out triggerValue))
        {
            // CHANGED: convert float value to pressed/not pressed
            bool isPressed = triggerValue > 0.7f;

            if (isPressed && !lastButtonState)
            {
                uiCanvas.SetActive(!uiCanvas.activeSelf);
            }

            lastButtonState = isPressed;
        }
    }
}
