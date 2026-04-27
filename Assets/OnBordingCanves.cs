using UnityEngine;
using UnityEngine.XR;

public class OnBordingCanves : MonoBehaviour
{
    [SerializeField] private GameObject[] canvases; // أكثر من كانفاس

    private bool isHidden = false;
    private bool wasPressed = false;

    void Update()
    {
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPressed))
        {
            if (!isHidden && isPressed && !wasPressed)
            {
                foreach (GameObject canvas in canvases)
                {
                    canvas.SetActive(false);
                }

                isHidden = true;
            }

            wasPressed = isPressed;
        }
    }
}