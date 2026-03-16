using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerRpcDemo : NetworkBehaviour
{
    private NetworkVariable<int> interactCount = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[Spawn State] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"object owner={OwnerClientId}, current interactCount={interactCount.Value}, " +
            $"object={gameObject.name}");

        interactCount.OnValueChanged += OnInteractCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        interactCount.OnValueChanged -= OnInteractCountChanged;
        base.OnNetworkDespawn();
    }

    private void OnInteractCountChanged(int oldValue, int newValue)
    {
        Debug.Log($"[State] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"object owner={OwnerClientId}, interact count changed: {oldValue} -> {newValue}");
    }

    public void PingServer(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!context.performed) return;
        Debug.Log($"[Local] Interact pressed by client: {NetworkManager.Singleton.LocalClientId}");
        SendPingServerRpc();
    }

    [ServerRpc]
    private void SendPingServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[Server] Received request from clientId: {senderId}");

        interactCount.Value++;
        Debug.Log($"[Server] interactCount for owner {OwnerClientId} is now: {interactCount.Value}");

        var targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { senderId }
            }
        };

        ShowTargetedResponseClientRpc(targetParams);
    }

    [ClientRpc]
    private void ShowPingResponseClientRpc(ulong originalSenderClientId, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[Client] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"this object owner={OwnerClientId}, object name={gameObject.name}, " +
            $"original sender clientId={originalSenderClientId}");
    }

    [ClientRpc]
    private void ShowTargetedResponseClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[Client:Targeted] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"this object owner={OwnerClientId}, object name={gameObject.name}");
    }
}

