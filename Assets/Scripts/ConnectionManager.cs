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
    
    private bool isApproveConnection = false;
    [Command("set-approve")]
    public bool SetIsApproveConnection()
    {
        isApproveConnection = !isApproveConnection;
        return isApproveConnection;
    }
    
    private void Start()
    {
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
        print("incoming Name = " + incomingName);
        
        if (_connectedNames.Contains(incomingName))
        {
            response.Approved = false;
            response.Reason = "Name already in use";
            response.Pending = false;
            return;
        }
        
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        // Your approval logic determines the following values
        response.Approved = true;
        response.CreatePlayerObject = true;

        // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
        response.PlayerPrefabHash = null;

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        // If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
        // On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
        response.Reason = "Some reason for not approving the client";
        
        TrackNameOnServer(request.ClientNetworkId, incomingName);

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
    }
}
