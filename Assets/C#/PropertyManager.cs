using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertySignManager : MonoBehaviour
{
    [Header("Property Settings")]
    public string propertyId = "YOUR_PROPERTY_ID_HERE";

    [Header("Info Text Fields")]
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI sizeText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI ownerText;
    public TextMeshProUGUI typeText;

    [Header("Room Counts")]
    public TextMeshProUGUI bedroomText;
    public TextMeshProUGUI bathroomText;
    public TextMeshProUGUI kitchenText;

    [Header("Favorite Button")]
    public Button actionButton;

    [Tooltip("The UI Image that should change when favorite is toggled")]
    public Image favoriteImage;

    [Tooltip("Image shown when property is NOT favorited")]
    public Sprite notFavoritedSprite;

    [Tooltip("Image shown when property IS favorited")]
    public Sprite favoritedSprite;

    [Header("Debug")]
    public TextMeshProUGUI debugText;

    private FirebaseFirestore db;
    private FirebaseUser currentUser;

    private bool isFavorited = false;
    private bool isBusy = false;

    private string cachedTitle = "";
    private string cachedDescription = "";

    private async void Start()
    {
        Log("[SignManager] Start");

        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Log("[SignManager] Firebase not ready: " + status);
            return;
        }

        db = FirebaseFirestore.DefaultInstance;
        currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser == null)
        {
            Log("[SignManager] ERROR: No logged-in user found.");
            return;
        }

        Log("[SignManager] Current user id: " + currentUser.UserId);

        if (string.IsNullOrWhiteSpace(propertyId) || propertyId == "YOUR_PROPERTY_ID_HERE")
        {
            Log("[SignManager] ERROR: propertyId is not assigned.");
            return;
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);
            Log("[SignManager] Button listener attached.");
        }
        else
        {
            Log("[SignManager] WARNING: actionButton is not assigned.");
        }

        await LoadPropertyData();
        await CheckIfAlreadyFavorited();
        UpdateFavoriteVisual();
    }

    public async void OnActionButtonClicked()
    {
        if (isBusy)
        {
            Log("[SignManager] Button click ignored: operation already running.");
            return;
        }

        if (currentUser == null || db == null)
        {
            Log("[SignManager] Button click ignored: Firebase or user not ready.");
            return;
        }

        isBusy = true;

        if (actionButton != null)
            actionButton.interactable = false;

        try
        {
            if (!isFavorited)
            {
                await SaveFavorite();
            }
            else
            {
                await RemoveFavorite();
            }

            UpdateFavoriteVisual();
        }
        catch (Exception ex)
        {
            Log("[SignManager] Button action failed: " + ex.Message);
        }
        finally
        {
            if (actionButton != null)
                actionButton.interactable = true;

            isBusy = false;
        }
    }

    private void UpdateFavoriteVisual()
    {
        if (favoriteImage == null)
        {
            Log("[SignManager] WARNING: favoriteImage is not assigned.");
            return;
        }

        if (isFavorited)
        {
            if (favoritedSprite != null)
                favoriteImage.sprite = favoritedSprite;

            Log("[SignManager] Favorite visual -> favorited");
        }
        else
        {
            if (notFavoritedSprite != null)
                favoriteImage.sprite = notFavoritedSprite;

            Log("[SignManager] Favorite visual -> not favorited");
        }
    }

    private async Task LoadPropertyData()
    {
        Log("[SignManager] Loading Property/" + propertyId);

        try
        {
            DocumentSnapshot snap = await db.Collection("Property")
                                            .Document(propertyId)
                                            .GetSnapshotAsync();

            if (!snap.Exists)
            {
                Log("[SignManager] Property document not found.");
                return;
            }

            string title = "";
            string description = "";
            string city = "";
            string neighborhood = "";
            string ownerUid = "";
            string propertyType = "";

            snap.TryGetValue("title", out title);
            snap.TryGetValue("description", out description);
            snap.TryGetValue("city", out city);
            snap.TryGetValue("neighborhood", out neighborhood);
            snap.TryGetValue("ownerUid", out ownerUid);
            snap.TryGetValue("type", out propertyType);

            cachedTitle = title ?? "";
            cachedDescription = description ?? "";

            string sizeDisplay = GetDisplayValue(snap, "size", "m²");
            string priceDisplay = GetDisplayValue(snap, "price", "SAR");
            string ownerName = await ResolveOwnerName(ownerUid);

            if (bedroomText != null)
                bedroomText.text = ExtractNumber(cachedDescription, "bedroom");

            if (bathroomText != null)
                bathroomText.text = ExtractNumber(cachedDescription, "bathroom");

            if (kitchenText != null)
                kitchenText.text = ExtractNumber(cachedDescription, "kitchen");

            if (locationText != null)
                locationText.text = BuildLocation(city, neighborhood);

            if (sizeText != null)
                sizeText.text = sizeDisplay;

            if (priceText != null)
                priceText.text = priceDisplay;

            if (ownerText != null)
                ownerText.text = ownerName;

            if (typeText != null)
                typeText.text = string.IsNullOrWhiteSpace(propertyType) ? "Unknown" : propertyType;

            Log("[SignManager] Loaded property successfully. Title: " + cachedTitle);
            Log("[SignManager] Owner resolved to: " + ownerName);
            Log("[SignManager] Property type: " + propertyType);
        }
        catch (Exception ex)
        {
            Log("[SignManager] LoadPropertyData error: " + ex.Message);
        }
    }

    private string GetDisplayValue(DocumentSnapshot snap, string fieldName, string suffix)
    {
        long longValue;
        if (snap.TryGetValue(fieldName, out longValue))
        {
            if (fieldName == "price")
                return string.Format("{0:N0} {1}", longValue, suffix);

            return longValue + " " + suffix;
        }

        double doubleValue;
        if (snap.TryGetValue(fieldName, out doubleValue))
        {
            if (fieldName == "price")
                return string.Format("{0:N0} {1}", doubleValue, suffix);

            return doubleValue + " " + suffix;
        }

        string stringValue;
        if (snap.TryGetValue(fieldName, out stringValue))
            return stringValue ?? "";

        return "";
    }

    private async Task<string> ResolveOwnerName(string ownerUid)
    {
        if (string.IsNullOrWhiteSpace(ownerUid))
            return "Unknown";

        try
        {
            Log("[SignManager] Trying company/" + ownerUid);

            DocumentSnapshot companySnap = await db.Collection("company")
                                                   .Document(ownerUid)
                                                   .GetSnapshotAsync();

            if (companySnap.Exists)
            {
                string companyName;
                if (companySnap.TryGetValue("companyName", out companyName) &&
                    !string.IsNullOrWhiteSpace(companyName))
                    return companyName;

                string fallbackName;
                if (companySnap.TryGetValue("name", out fallbackName) &&
                    !string.IsNullOrWhiteSpace(fallbackName))
                    return fallbackName;
            }
        }
        catch (Exception ex)
        {
            Log("[SignManager] Company doc lookup failed: " + ex.Message);
        }

        try
        {
            Log("[SignManager] Trying company query uid=" + ownerUid);

            QuerySnapshot companyQuery = await db.Collection("company")
                                                 .WhereEqualTo("uid", ownerUid)
                                                 .Limit(1)
                                                 .GetSnapshotAsync();

            foreach (DocumentSnapshot companyDoc in companyQuery.Documents)
            {
                string companyName;
                if (companyDoc.TryGetValue("companyName", out companyName) &&
                    !string.IsNullOrWhiteSpace(companyName))
                    return companyName;

                string fallbackName;
                if (companyDoc.TryGetValue("name", out fallbackName) &&
                    !string.IsNullOrWhiteSpace(fallbackName))
                    return fallbackName;
            }
        }
        catch (Exception ex)
        {
            Log("[SignManager] Company query lookup failed: " + ex.Message);
        }

        try
        {
            Log("[SignManager] Trying Customer/" + ownerUid);

            DocumentSnapshot customerSnap = await db.Collection("Customer")
                                                    .Document(ownerUid)
                                                    .GetSnapshotAsync();

            if (customerSnap.Exists)
            {
                string displayName;
                if (customerSnap.TryGetValue("displayName", out displayName) &&
                    !string.IsNullOrWhiteSpace(displayName))
                        return displayName;

                string customerName;
                if (customerSnap.TryGetValue("name", out customerName) &&
                    !string.IsNullOrWhiteSpace(customerName))
                        return customerName;
            }
        }
        catch (Exception ex)
        {
            Log("[SignManager] Customer lookup failed: " + ex.Message);
        }

        return "Unknown";
    }

    private async Task CheckIfAlreadyFavorited()
    {
        try
        {
            DocumentSnapshot snap = await db.Collection("Customer")
                                            .Document(currentUser.UserId)
                                            .Collection("favorites")
                                            .Document(propertyId)
                                            .GetSnapshotAsync();

            isFavorited = snap.Exists;
            Log("[SignManager] Is favorited: " + isFavorited);
        }
        catch (Exception ex)
        {
            Log("[SignManager] CheckIfAlreadyFavorited error: " + ex.Message);
        }
    }

    private async Task SaveFavorite()
    {
        if (isFavorited)
        {
            Log("[SignManager] Property already favorited.");
            return;
        }

        try
        {
            Dictionary<string, object> favoriteData = new Dictionary<string, object>();
            favoriteData["propertyId"] = propertyId;
            favoriteData["savedAt"] = FieldValue.ServerTimestamp;
            favoriteData["title"] = cachedTitle;
            favoriteData["description"] = cachedDescription;
            favoriteData["customerUid"] = currentUser.UserId;

            await db.Collection("Customer")
                    .Document(currentUser.UserId)
                    .Collection("favorites")
                    .Document(propertyId)
                    .SetAsync(favoriteData);

            isFavorited = true;
            Log("[SignManager] Favorite saved successfully.");
        }
        catch (Exception ex)
        {
            Log("[SignManager] SaveFavorite error: " + ex.Message);
            throw;
        }
    }

    private async Task RemoveFavorite()
    {
        if (!isFavorited)
        {
            Log("[SignManager] Property is not currently favorited.");
            return;
        }

        try
        {
            await db.Collection("Customer")
                    .Document(currentUser.UserId)
                    .Collection("favorites")
                    .Document(propertyId)
                    .DeleteAsync();

            isFavorited = false;
            Log("[SignManager] Favorite removed successfully.");
        }
        catch (Exception ex)
        {
            Log("[SignManager] RemoveFavorite error: " + ex.Message);
            throw;
        }
    }

    private string ExtractNumber(string text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "?";

        string lower = text.ToLower();
        string key = keyword.ToLower();

        int keyIndex = lower.IndexOf(key);
        if (keyIndex < 0)
            return "?";

        string before = lower.Substring(0, keyIndex).TrimEnd();
        string[] parts = before.Split(' ');

        for (int i = parts.Length - 1; i >= 0; i--)
        {
            int num;
            if (int.TryParse(parts[i], out num))
                return num.ToString();
        }

        return "?";
    }

    private string BuildLocation(string city, string neighborhood)
    {
        if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(neighborhood))
            return city + " - " + neighborhood;

        if (!string.IsNullOrWhiteSpace(city))
            return city;

        if (!string.IsNullOrWhiteSpace(neighborhood))
            return neighborhood;

        return "";
    }

    private void Log(string message)
    {
        Debug.Log(message);

        if (debugText != null)
            debugText.text += "\n" + message;
    }
}
