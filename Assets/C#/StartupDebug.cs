using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupDebug : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[StartupDebug] Awake scene: " + SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        Debug.Log("[StartupDebug] Start scene: " + SceneManager.GetActiveScene().name);
    }
}
