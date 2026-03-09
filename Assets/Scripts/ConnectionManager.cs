using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;
using TMPro;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject leaveButton;
    [SerializeField] private TMP_Text errorText; // optional (can be null)
    
    // spawn points to use when a client joins; populate in the inspector
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool useRandomSpawn = false;

    [Header("Character Selection")] // inspector grouping
    [SerializeField] private TMP_Dropdown characterDropdown; // dropdown to choose character

    // list of prefab hashes corresponding to dropdown index; element 0 = default
    [SerializeField] private List<uint> alternatePlayerPrefabHashes = new List<uint>();

    public enum ApprovalMode
    {
        AlwaysApprove,    // Always approve connections automatically
        ManualApprove     // Require manual approval via console command
    }

    [Header("Connection Settings")]
    [Tooltip("Determines whether connections are automatically approved or require manual approval")]
    public ApprovalMode approvalMode = ApprovalMode.ManualApprove;

    [Header("Player Limits")]
    [SerializeField] private int maxPlayers = 6;
    public int MaxPlayers => maxPlayers;

    /// <summary>
    /// Tracks connected player names to prevent duplicates and enforce player limits.
    /// Used for both duplicate username validation and player count enforcement.
    /// </summary>
    private bool isApproveConnection = false;
    [Command("set-approve")]
    public bool SetIsApproveConnection()
    {
        isApproveConnection = !isApproveConnection;
        string state = isApproveConnection ? "enabled" : "disabled";
        Debug.Log($"Connection approval is now {state}");
        return isApproveConnection;
    }
    
    /// <summary>
    /// Store both username and chosen character id in the connection payload.
    /// Format: "{username}|{characterId}" (pipe-separated).
    /// </summary>
    private void SetConnectionData(string username, int characterId)
    {
        string payload = $"{username}|{characterId}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            Encoding.UTF8.GetBytes(payload);
    }

    public void StartHostWithUsername()
    {
        string userName = usernameInput.GetComponent<TMP_InputField>().text;
        int characterId = 0;
        if (characterDropdown != null)
            characterId = characterDropdown.value;
        SetConnectionData(userName, characterId);
        // Host connection is always approved by Netcode (cannot reject itself),
        // but we still send payload for consistent logic and tracking.
        NetworkManager.Singleton.StartHost();
    }

    public void StartClientWithUsername()
    {
        string userName = usernameInput.GetComponent<TMP_InputField>().text;
        int characterId = 0;
        if (characterDropdown != null)
            characterId = characterDropdown.value;
        SetConnectionData(userName, characterId);
        NetworkManager.Singleton.StartClient();
    }
    
    private void Start()
    {
        // Validate maxPlayers configuration to prevent invalid values
        if (maxPlayers <= 0)
        {
            Debug.LogWarning($"MaxPlayers is set to {maxPlayers}, defaulting to 6");
            maxPlayers = 6;
        }
        
        // Force enable Connection Approval in code
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        }
        
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }
    
    private string DecodePayloadToString(ArraySegment<byte> payload)
    {
        if (payload.Array == null || payload.Count <= 0)
            return "";

        return Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
    }

    private readonly HashSet<string> _connectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ulong, string> _clientIdToName = new Dictionary<ulong, string>();
    
    private void PrintConnectedClients()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        Debug.Log("========== SERVER CONNECTED CLIENTS ==========");

        if (_clientIdToName.Count == 0)
        {
            Debug.Log("No connected clients.");
            return;
        }

        foreach (var kvp in _clientIdToName)
        {
            Debug.Log($"ClientID: {kvp.Key} | Username: {kvp.Value}");
        }

        Debug.Log("===============================================");
    }
    
    private void TrackNameOnServer(ulong clientId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        // Prevent double-tracking
        if (_clientIdToName.TryGetValue(clientId, out string existing))
        {
            if (!string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
            {
                _connectedNames.Remove(existing);
                _clientIdToName[clientId] = name;
                _connectedNames.Add(name);
            }
            return;
        }
        else
        {
            _clientIdToName.Add(clientId, name);
            _connectedNames.Add(name);
        }
        PrintConnectedClients();
    }
    
    private void UntrackNameOnServer(ulong clientId)
    {
        if (_clientIdToName.TryGetValue(clientId, out string name))
        {
            _clientIdToName.Remove(clientId);
            _connectedNames.Remove(name);
        }

        PrintConnectedClients();
    }
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // parse username + character id from payload
        if (!TryParseConnectionPayload(request.Payload, out string incomingName, out int characterId))
        {
            response.Approved = false;
            response.Reason = "Invalid connection payload";
            response.Pending = false;
            return;
        }
        Debug.Log($"incoming Name = {incomingName}, charId = {characterId}");
        
        // Check if this is the Host
        // Host connection is always approved by Netcode (cannot reject itself)
        if (request.ClientNetworkId == NetworkManager.ServerClientId)
        {
            // host should spawn at default position (0) or first spawn point
            response.Position = GetSpawnPosition(request.ClientNetworkId);
            SetupApprovedResponse(response, request.ClientNetworkId, incomingName);
            return;
        }
        
        // 1. Check for duplicate name (Lab 2)
        if (_connectedNames.Contains(incomingName))
        {
            response.Approved = false;
            response.Reason = "Name already in use";
            response.Pending = false;
            return;
        }
        
        // 2. Check player count limit (NEW LOGIC)
        // Enforce maximum player limit to prevent server overload
        if (_connectedNames.Count >= maxPlayers)
        {
            response.Approved = false;
            response.Reason = "Server Full (Maximum players reached)";
            response.Pending = false;
            Debug.Log($"Connection rejected: Server is full. Current players: {_connectedNames.Count}, Max: {maxPlayers}");
            return;
        }
        
        // 3. Check for duplicate name (Lab 2)
        if (_connectedNames.Contains(incomingName))
        {
            response.Approved = false;
            response.Reason = "Name already in use";
            response.Pending = false;
            return;
        }
        
        // 4. Check approval based on the selected mode
        switch (approvalMode)
        {
            case ApprovalMode.AlwaysApprove:
                // Always approve in this mode
                break;
                
            case ApprovalMode.ManualApprove:
                // Check for console approval (Lab 1)
                // If isApproveConnection is false then reject immediately
                if (!isApproveConnection)
                {
                    response.Approved = false;
                    response.Reason = "Connection not approved by server (Use set-approve command)";
                    response.Pending = false;
                    return;
                }
                break;
        }
        
        // 4. If passed all checks then approve
        response.Position = GetSpawnPosition(request.ClientNetworkId);
        // assign prefab hash based on character choice
        uint? hash = GetPlayerPrefabHashFromCharacterId(characterId);
        if (hash.HasValue)
            response.PlayerPrefabHash = hash.Value;
        SetupApprovedResponse(response, request.ClientNetworkId, incomingName);
    }
    
    private void SetupApprovedResponse(NetworkManager.ConnectionApprovalResponse response, ulong clientId, string incomingName)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;
        // PlayerPrefabHash may have been set prior; leave it alone
        response.Rotation = Quaternion.identity;
        response.Reason = string.Empty;
        
        TrackNameOnServer(clientId, incomingName);
        response.Pending = false;
    }
    
    /// <summary>
    /// Determine a spawn position for a given clientId using a round-robin
    /// or random selection from the configured array of spawn points.
    /// </summary>
    private Vector3 GetSpawnPosition(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Vector3.zero;

        if (useRandomSpawn)
        {
            int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].position;
        }

        int index = (int)(clientId % (ulong)spawnPoints.Length);
        return spawnPoints[index].position;
    }

    /// <summary>
    /// Parse a connection payload of the form "username|characterId".
    /// Returns false if format is invalid.
    /// </summary>
    private bool TryParseConnectionPayload(ArraySegment<byte> payload, out string username, out int characterId)
    {
        username = string.Empty;
        characterId = 0;

        string decoded = DecodePayloadToString(payload);
        if (string.IsNullOrWhiteSpace(decoded))
            return false;

        string[] parts = decoded.Split('|');
        if (parts.Length < 1)
            return false;

        username = parts[0].Trim();
        if (parts.Length >= 2)
        {
            int.TryParse(parts[1], out characterId);
        }
        return true;
    }

    /// <summary>
    /// Return a prefab hash based on the chosen character id.
    /// The list should contain hashes in the same order as the dropdown options.
    /// </summary>
    private uint? GetPlayerPrefabHashFromCharacterId(int characterId)
    {
        if (alternatePlayerPrefabHashes == null || alternatePlayerPrefabHashes.Count == 0)
            return null;

        if (characterId < 0 || characterId >= alternatePlayerPrefabHashes.Count)
            return alternatePlayerPrefabHashes[0];

        return alternatePlayerPrefabHashes[characterId];
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }
    
    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        
        NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }
    
    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            SetUIConnected(true);
        }
    }
    
    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetUIConnected(true);
        }
    }
    
    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetUIConnected(false);

            string reason = NetworkManager.Singleton.DisconnectReason;

            if (!string.IsNullOrEmpty(reason))
            {
                // Simplify technical disconnect reasons for better UX
                string displayReason = SimplifyDisconnectReason(reason);
                SetError(displayReason);
            }
        }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            UntrackNameOnServer(clientId);
        }
    }

    private string SimplifyDisconnectReason(string reason)
    {
        // Convert technical messages to user-friendly ones
        if (reason.Contains("TransportShutdown"))
            return "Transport was shutdown.";
        if (reason.Contains("ClosedByRemote"))
            return "Connection closed by remote.";
        if (reason.Contains("NetworkConnectionManager was shutdown"))
            return "Connection manager was shutdown.";
        
        // Return original if no simplification needed
        return reason;
    }

    private void SetUIConnected(bool connected)
    {
        loginPanel.SetActive(!connected);
        leaveButton.SetActive(connected);

        if (connected)
            ClearError();
    }
    
    private void SetError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }

        Debug.LogWarning(message);
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = "";
    }
    
    public void OnLeaveButtonClick()
    {
        ClearError();

        if (NetworkManager.Singleton == null)
            return;

        // Shutdown works for both host and client
        NetworkManager.Singleton.Shutdown();
        
        SetUIConnected(false);
    }
}
