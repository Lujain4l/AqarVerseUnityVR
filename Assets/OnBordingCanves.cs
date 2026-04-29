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

        bool primary = false;   // A
        bool secondary = false; // B

        rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out primary);
        rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out secondary);

        // إذا ضغط أي زر
        if (!isHidden && (primary || secondary))
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