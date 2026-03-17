using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerRpcDemo : NetworkBehaviour
{
    private const int DefaultItemUses = 3;
    private const float UseItemDebounceSeconds = 0.1f;

    private float lastUseItemTime = -Mathf.Infinity;

    private NetworkVariable<int> interactCount = new NetworkVariable<int>(0);

    private NetworkVariable<int> itemUseCount = new NetworkVariable<int>(
        DefaultItemUses,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> ItemUseCount => itemUseCount;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[Spawn State] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"object owner={OwnerClientId}, current interactCount={interactCount.Value}, " +
            $"object={gameObject.name}");

        interactCount.OnValueChanged += OnInteractCountChanged;
        itemUseCount.OnValueChanged += OnItemUseCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        interactCount.OnValueChanged -= OnInteractCountChanged;
        itemUseCount.OnValueChanged -= OnItemUseCountChanged;
        base.OnNetworkDespawn();
    }

    private void OnInteractCountChanged(int oldValue, int newValue)
    {
        Debug.Log($"[State] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"object owner={OwnerClientId}, interact count changed: {oldValue} -> {newValue}");
    }

    private void OnItemUseCountChanged(int oldValue, int newValue)
    {
        Debug.Log($"[State] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"object owner={OwnerClientId}, item uses changed: {oldValue} -> {newValue}");
    }

    public void PingServer(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!context.performed) return;
        Debug.Log($"[Local] Interact pressed by client: {NetworkManager.Singleton.LocalClientId}");
        SendPingServerRpc();
    }

    public void UseItem(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!context.performed) return;

        Debug.Log($"[Local] UseItem pressed by client: {NetworkManager.Singleton.LocalClientId}");
        lastUseItemTime = Time.unscaledTime;
        UseItemServerRpc();
    }

    private void Update()
    {
        // Fallback input check for cases where the PlayerInput binding may not be invoked (e.g. prefab binding mismatch)
        if (!IsOwner) return;

        // Prevent the fallback check from firing immediately after the InputAction callback
        // (which may call UseItemServerRpc via the binding) so we avoid double-using.
        if (Time.unscaledTime - lastUseItemTime < UseItemDebounceSeconds) return;

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            lastUseItemTime = Time.unscaledTime;
            UseItemServerRpc();
        }
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

    [ServerRpc]
    private void UseItemServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        bool succeeded;
        string message;

        if (itemUseCount.Value > 0)
        {
            itemUseCount.Value--;
            succeeded = true;
            message = "Item used successfully.";
        }
        else
        {
            succeeded = false;
            message = "No item uses remaining.";
        }

        var clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { senderId }
            }
        };

        ShowUseItemResultClientRpc(succeeded, itemUseCount.Value, message, clientParams);
    }

    /// <summary>
    /// Resets the item use count to the default value.
    /// Must be called on the server.
    /// </summary>
    public void ResetItemUses()
    {
        if (!IsServer) return;
        itemUseCount.Value = DefaultItemUses;
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

    [ClientRpc]
    private void ShowUseItemResultClientRpc(bool succeeded, int remainingUses, string message, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[Client:UseItem] Local machine clientId={NetworkManager.Singleton.LocalClientId}, " +
            $"success={succeeded}, remainingUses={remainingUses}, message={message}");
    }
}

