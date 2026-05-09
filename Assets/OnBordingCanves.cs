using UnityEngine;
using UnityEngine.XR;

public class OnBordingCanves : MonoBehaviour
{
    [SerializeField] private GameObject[] canvases;

    private InputDevice rightHand;
    private bool isHidden = false;

    void Start()
    {
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        if (!rightHand.isValid)
            rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool secondary = false; // B

        rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out secondary);

        // إذا ضغط زر B
        if (!isHidden && secondary)
        {
            foreach (GameObject canvas in canvases)
            {
                if (canvas != null)
                    canvas.SetActive(false);
            }

            isHidden = true;
        }
    }
}