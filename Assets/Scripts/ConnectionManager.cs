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
    
    private void SetConnectionData(string username)
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            System.Text.Encoding.UTF8.GetBytes(username);
    }

    public void StartHostWithUsername()
    {
        string userName = usernameInput.GetComponent<TMP_InputField>().text;
        SetConnectionData(userName);
        // Host connection is always approved by Netcode (cannot reject itself),
        // but we still send payload for consistent logic and tracking.
        NetworkManager.Singleton.StartHost();
    }

    public void StartClientWithUsername()
    {
        string userName = usernameInput.GetComponent<TMP_InputField>().text;
        SetConnectionData(userName);
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
        string incomingName = DecodePayloadToString(request.Payload);
        Debug.Log("incoming Name =  " + incomingName);
        
        // Check if this is the Host
        // Host connection is always approved by Netcode (cannot reject itself)
        if (request.ClientNetworkId == NetworkManager.ServerClientId)
        {
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
        SetupApprovedResponse(response, request.ClientNetworkId, incomingName);
    }
    
    private void SetupApprovedResponse(NetworkManager.ConnectionApprovalResponse response, ulong clientId, string incomingName)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.PlayerPrefabHash = null;
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;
        response.Reason = string.Empty;
        
        TrackNameOnServer(clientId, incomingName);
        response.Pending = false;
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
