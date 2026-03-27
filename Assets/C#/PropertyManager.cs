using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a GameObject in MainScene.
/// Handles: loading property description from Firestore + saving/removing favorites.
/// </summary>
public class PropertyManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────

    [Header("Property Settings")]
    [Tooltip("Paste the Firestore document ID of the property you want to display.")]
    public string propertyId = "YOUR_PROPERTY_ID_HERE";

    [Header("UI References")]
    [Tooltip("Drag the TMP text component that shows the property description.")]
    public TextMeshProUGUI descriptionText;

    [Tooltip("Drag the Favorite button here.")]
    public Button favoriteButton;

    [Tooltip("(Optional) Text label on the Favorite button to toggle its display.")]
    public TextMeshProUGUI favoriteButtonLabel;

    // ─────────────────────────────────────────────
    // Private State
    // ─────────────────────────────────────────────

    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private bool isFavorited = false;
    private string cachedTitle = "";
    private string cachedDescription = "";

    // ─────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────

    private async void Start()
    {
        // ── 1. Get the current authenticated user ──
        currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser == null)
        {
            Debug.LogError("[PropertyManager] No authenticated user found. " +
                           "Make sure FirebaseLoginManager authenticated before loading MainScene.");
            SetDescriptionText("Error: User not logged in.");
            return;
        }

        Log($"[PropertyManager] Current user UID: {currentUser.UserId}");

        // ── 2. Validate propertyId ──
        if (string.IsNullOrEmpty(propertyId) || propertyId == "YOUR_PROPERTY_ID_HERE")
        {
            Debug.LogError("[PropertyManager] propertyId is not set in the Inspector.");
            SetDescriptionText("Error: No property ID assigned.");
            return;
        }

        // ── 3. Get Firestore instance ──
        db = FirebaseFirestore.DefaultInstance;

        if (db == null)
        {
            Debug.LogError("[PropertyManager] FirebaseFirestore.DefaultInstance is null. " +
                           "Ensure Firebase was initialized in the Login scene.");
            return;
        }

        // ── 4. Wire the Favorite button ──
        if (favoriteButton != null)
        {
            favoriteButton.onClick.AddListener(OnFavoriteButtonClicked);
        }
        else
        {
            Debug.LogWarning("[PropertyManager] Favorite button is not assigned in the Inspector.");
        }

        // ── 5. Load data ──
        await LoadPropertyDescription();
        await CheckIfAlreadyFavorited();
    }

    // ─────────────────────────────────────────────
    // Feature 1 — Load Property Description
    // ─────────────────────────────────────────────

    private async Task LoadPropertyDescription()
    {
        Log($"[PropertyManager] Fetching property: Property/{propertyId}");

        try
        {
            DocumentReference docRef = db.Collection("Property").Document(propertyId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning($"[PropertyManager] No document found at Property/{propertyId}");
                SetDescriptionText("Property not found.");
                return;
            }

            // Pull description
            if (snapshot.TryGetValue("description", out string description))
            {
                cachedDescription = description;
                SetDescriptionText(description);
                Log($"[PropertyManager] Description loaded: {description}");
            }
            else
            {
                Debug.LogWarning("[PropertyManager] Field 'description' not found in document.");
                SetDescriptionText("No description available.");
            }

            // Cache title for use in the favorite document (optional but useful)
            if (snapshot.TryGetValue("title", out string title))
            {
                cachedTitle = title;
                Log($"[PropertyManager] Title cached: {title}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PropertyManager] Failed to load property description: {ex.Message}");
            SetDescriptionText("Failed to load property.");
        }
    }

    // ─────────────────────────────────────────────
    // Feature 2 — Favorites
    // ─────────────────────────────────────────────

    /// <summary>
    /// Check on scene load whether this property is already in the user's favorites.
    /// Updates the button label accordingly.
    /// </summary>
    private async Task CheckIfAlreadyFavorited()
    {
        if (currentUser == null || db == null) return;

        try
        {
            DocumentReference favRef = db
                .Collection("Customer")
                .Document(currentUser.UserId)
                .Collection("favorites")
                .Document(propertyId);

            DocumentSnapshot snapshot = await favRef.GetSnapshotAsync();
            isFavorited = snapshot.Exists;

            Log($"[PropertyManager] Is favorited: {isFavorited}");
            UpdateFavoriteButtonLabel();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PropertyManager] Failed to check favorite status: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when the user clicks the Favorite button.
    /// Toggles between saving and removing the favorite.
    /// </summary>
    public async void OnFavoriteButtonClicked()
{
    // Guard: ignore if this manager is already processing
    if (currentUser == null)
    {
        Debug.LogError("[PropertyManager] Cannot save favorite — no authenticated user.");
        return;
    }

    if (string.IsNullOrEmpty(propertyId))
    {
        Debug.LogError("[PropertyManager] Cannot save favorite — propertyId is empty.");
        return;
    }

    // Disable ONLY this manager's button during async operation
    if (favoriteButton != null) favoriteButton.interactable = false;

    Log($"[PropertyManager] Button clicked for propertyId: {propertyId}, isFavorited: {isFavorited}");

    if (isFavorited)
        await RemoveFavorite();
    else
        await SaveFavorite();

    if (favoriteButton != null) favoriteButton.interactable = true;
}

    /// <summary>
    /// Writes the favorite document to:
    /// Customer/{uid}/favorites/{propertyId}
    /// </summary>
   private async Task SaveFavorite()
{
    // Double-check state hasn't changed from another manager
    if (isFavorited)
    {
        Log("[PropertyManager] Already favorited, skipping save.");
        return;
    }

    Log($"[PropertyManager] Saving favorite: Customer/{currentUser.UserId}/favorites/{propertyId}");

    try
    {
        DocumentReference favRef = db
            .Collection("Customer")
            .Document(currentUser.UserId)
            .Collection("favorites")
            .Document(propertyId);

        Dictionary<string, object> favoriteData = new Dictionary<string, object>
        {
            { "propertyId",   propertyId },
            { "savedAt",      FieldValue.ServerTimestamp },
            { "title",        cachedTitle },
            { "description",  cachedDescription },
            { "customerUid",  currentUser.UserId }
        };

        await favRef.SetAsync(favoriteData);

        isFavorited = true;
        UpdateFavoriteButtonLabel();
        Log("[PropertyManager] Favorite saved successfully.");
    }
    catch (Exception ex)
    {
        Log($"[PropertyManager] Failed to save favorite: {ex.Message}");
    }
}

private async Task RemoveFavorite()
{
    // Double-check state hasn't changed from another manager
    if (!isFavorited)
    {
        Log("[PropertyManager] Already not favorited, skipping remove.");
        return;
    }

    Log($"[PropertyManager] Removing favorite: Customer/{currentUser.UserId}/favorites/{propertyId}");

    try
    {
        DocumentReference favRef = db
            .Collection("Customer")
            .Document(currentUser.UserId)
            .Collection("favorites")
            .Document(propertyId);

        await favRef.DeleteAsync();

        isFavorited = false;
        UpdateFavoriteButtonLabel();
        Log("[PropertyManager] Favorite removed successfully.");
    }
    catch (Exception ex)
    {
        Log($"[PropertyManager] Failed to remove favorite: {ex.Message}");
    }
}

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void SetDescriptionText(string text)
    {
        if (descriptionText != null)
            descriptionText.text = text;
        else
            Debug.LogWarning("[PropertyManager] descriptionText TMP component is not assigned.");
    }

  [Header("Heart Button Visual")]
public Image heartImage;
public Color favoritedColor   = new Color(1f, 0.15f, 0.15f, 1f);   // red
public Color unfavoritedColor = new Color(0.96f, 0.72f, 0f, 1f);   // yellow

private void UpdateFavoriteButtonLabel()
{
    if (heartImage != null)
        heartImage.color = isFavorited ? favoritedColor : unfavoritedColor;
}
[Header("Debug (optional)")]
public TextMeshProUGUI debugText; // drag any TMP text here, or leave empty

private void Log(string message)
{
    Debug.Log(message);
    if (debugText != null)
        debugText.text += "\n" + message;
}
}