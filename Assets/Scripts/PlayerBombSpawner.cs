using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBombSpawner : NetworkBehaviour
{
    [Header("Bomb Spawn")]
    [SerializeField]
    private GameObject bombPrefab;

    [SerializeField]
    private float bombSpawnOffset = 1.5f;

    private PlayerInput playerInput;
    private InputAction placeBombAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;
        placeBombAction = playerInput.actions["PlaceBomb"];
        if (placeBombAction != null)
        {
            placeBombAction.performed += OnPlaceBombPerformed;
        }
        else
        {
            Debug.LogError("PlaceBomb action not found in PlayerInput actions.");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;
        if (placeBombAction != null)
        {
            placeBombAction.performed -= OnPlaceBombPerformed;
        }
    }

    private void OnPlaceBombPerformed(InputAction.CallbackContext context)
    {
        RequestBombSpawn();
    }

    private void RequestBombSpawn()
    {
        PlaceBombServerRpc();
    }

    private Vector3 GetBombSpawnPosition()
    {
        return transform.position + transform.forward * bombSpawnOffset;
    }

    [ServerRpc]
    private void PlaceBombServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log(
            $"[Server] PlaceBombServerRpc received "
                + $"| SenderClientId: {senderClientId} "
                + $"| PlayerObjectOwnerClientId: {OwnerClientId} "
                + $"| PlayerObjectId: {NetworkObjectId}"
        );

        if (bombPrefab == null)
        {
            Debug.LogError("Bomb prefab is not assigned.");
            return;
        }

        Vector3 spawnPosition = GetBombSpawnPosition();

        GameObject bombInstance = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);

        NetworkObject bombNetworkObject = bombInstance.GetComponent<NetworkObject>();

        if (bombNetworkObject == null)
        {
            Debug.LogError("Bomb prefab is missing NetworkObject.");
            Destroy(bombInstance);
            return;
        }

        Bomb bomb = bombInstance.GetComponent<Bomb>();
        if (bomb != null)
        {
            bomb.SetRequestedByClientId(senderClientId);
        }
        else
        {
            Debug.LogWarning(
                "Bomb component not found on bomb prefab. RequestedByClientId will not be stored."
            );
        }

        bombNetworkObject.Spawn();

        Debug.Log(
            $"[Server] Bomb spawned from request | SenderClientId: {senderClientId} "
                + $"| BombNetworkObjectId: {bombNetworkObject.NetworkObjectId} "
                + $"| BombOwnerClientId: {bombNetworkObject.OwnerClientId} "
                + $"| SpawnPosition: {spawnPosition}"
        );
    }
}
