using Convai.Scripts;
using Convai.Scripts.Utils;
using UnityEngine;

/// <summary>
/// Positions the VR transcript UI relative to the active ConvaiNPC based on appearance changes.
/// </summary>
public class VRTranscriptUIPositionSetter : MonoBehaviour
{
    // Offset for positioning the UI relative to the ConvaiNPC
    [SerializeField] private Vector3 _offset;
    private UIAppearanceSettings _uiAppearanceSettings;

    /// <summary>
    /// Initializes necessary components and variables.
    /// </summary>
    private void Awake()
    {
        _uiAppearanceSettings = FindObjectOfType<UIAppearanceSettings>();
    }

    /// <summary>
    /// Subscribes to events when the script is enabled.
    /// </summary>
    private void OnEnable()
    {
        ConvaiNPCManager.Instance.OnActiveNPCChanged += ConvaiNPCManager_OnActiveNPCChanged;
        _uiAppearanceSettings.OnAppearanceChanged += UIAppearanceSettings_OnAppearanceChanged;
    }

    /// <summary>
    /// Unsubscribes from events to prevent issues when the script is disabled.
    /// </summary>
    private void OnDisable()
    {
        ConvaiNPCManager.Instance.OnActiveNPCChanged -= ConvaiNPCManager_OnActiveNPCChanged;
        _uiAppearanceSettings.OnAppearanceChanged -= UIAppearanceSettings_OnAppearanceChanged;
    }

    /// <summary>
    /// Updates the UI position when the appearance of the ConvaiNPC changes.
    /// </summary>
    private void UIAppearanceSettings_OnAppearanceChanged()
    {
        UpdateCurrentUIPosition(ConvaiNPCManager.Instance.GetActiveConvaiNPC());
    }

    /// <summary>
    /// Updates the UI position when the active ConvaiNPC changes.
    /// </summary>
    /// <param name="convaiNpc">The newly active ConvaiNPC.</param>
    private void ConvaiNPCManager_OnActiveNPCChanged(ConvaiNPC convaiNpc)
    {
        if (convaiNpc == null) return;
        UpdateCurrentUIPosition(convaiNpc);
    }

    /// <summary>
    /// Updates the position of the current UI based on the location and offset of the active ConvaiNPC.
    /// </summary>
    /// <param name="convaiNpc">The active ConvaiNPC.</param>
    private void UpdateCurrentUIPosition(ConvaiNPC convaiNpc)
    {
        Transform npcTransform = convaiNpc.transform;
        Vector3 targetOffset = _offset.x * npcTransform.right + _offset.y * npcTransform.up + _offset.z * npcTransform.forward;
        ConvaiChatUIHandler.Instance.GetCurrentUI().GetCanvasGroup().transform.position = npcTransform.position + targetOffset;
    }
}