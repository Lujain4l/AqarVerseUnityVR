using System.Collections;
using System.Collections.Generic;

using Firebase;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit Instance;
    public bool IsInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeFirebase();
    }

    private async void InitializeFirebase()
    {
        Debug.Log("Firebase: Checking dependencies...");

        var dependencyStatus = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
            IsInitialized = true;
            Debug.Log("Firebase: READY");
        }
        else
        {
            IsInitialized = false;
            Debug.LogError($"Firebase: Could not resolve dependencies: {dependencyStatus}");
        }
    }
}
