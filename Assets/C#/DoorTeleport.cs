using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorTeleport : MonoBehaviour
{
    public Transform xrOrigin;      // XR Origin
    public Transform insidePoint;   // المكان داخل البيت

    public void TeleportPlayer()
    {
        xrOrigin.position = insidePoint.position;
        xrOrigin.rotation = insidePoint.rotation;
    }
}