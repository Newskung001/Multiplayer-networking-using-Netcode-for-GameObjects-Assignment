using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Game manager script for handling network connection via UI buttons.
/// Attach this to a GameObject in your scene and bind the public methods to UI buttons.
/// </summary>
public class MainGameManagerScript : MonoBehaviour
{
    /// <summary>
    /// Called when Server button is clicked.
    /// Starts the network as a server only (no player character spawned).
    /// </summary>
    public void OnServerButtonClick()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log("Server started successfully!");
            }
            else
            {
                Debug.LogError("Failed to start server!");
            }
        }
        else
        {
            Debug.LogError("NetworkManager not found! Make sure NetworkManager is in the scene.");
        }
    }
    
    /// <summary>
    /// Called when Host button is clicked.
    /// Starts the network as a host (server + client with player character).
    /// </summary>
    public void OnHostButtonClick()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host started successfully!");
            }
            else
            {
                Debug.LogError("Failed to start host!");
            }
        }
        else
        {
            Debug.LogError("NetworkManager not found! Make sure NetworkManager is in the scene.");
        }
    }
    
    /// <summary>
    /// Called when Client button is clicked.
    /// Starts the network as a client (connects to server/host).
    /// </summary>
    public void OnClientButtonClick()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client started successfully!");
            }
            else
            {
                Debug.LogError("Failed to start client!");
            }
        }
        else
        {
            Debug.LogError("NetworkManager not found! Make sure NetworkManager is in the scene.");
        }
    }
}
