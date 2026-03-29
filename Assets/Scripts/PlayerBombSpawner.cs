using System.Collections.Generic;
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

    [Header("Cooldown (Feature 1)")]
    [SerializeField]
    private float bombCooldown = 2f;

    [Header("Active Bomb Limit (Feature 2)")]
    [SerializeField]
    private int maxActiveBombs = 3;

    [Header("Spawn Mode (Feature 3)")]
    [Tooltip(
        "When true, uses SpawnWithOwnership so the bomb's OwnerClientId matches the player who placed it."
    )]
    [SerializeField]
    private bool useSpawnWithOwnership = true;

    private PlayerInput playerInput;
    private InputAction placeBombAction;

    // Server-side tracking
    private float lastBombTime = -Mathf.Infinity;
    private List<NetworkObjectReference> activeBombs = new List<NetworkObjectReference>();

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

    /// <summary>
    /// Prune bombs that have been despawned or destroyed from the active list.
    /// </summary>
    private void PruneActiveBombs()
    {
        for (int i = activeBombs.Count - 1; i >= 0; i--)
        {
            if (!activeBombs[i].TryGet(out NetworkObject netObj) || netObj == null)
            {
                activeBombs.RemoveAt(i);
            }
        }
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

        // ── Feature 1: Cooldown check ──────────────────────────────
        float timeSinceLastBomb = Time.time - lastBombTime;
        if (timeSinceLastBomb < bombCooldown)
        {
            float remaining = bombCooldown - timeSinceLastBomb;
            Debug.Log(
                $"[Server] Bomb REJECTED (cooldown) "
                    + $"| SenderClientId: {senderClientId} "
                    + $"| CooldownRemaining: {remaining:F1}s"
            );
            NotifyBombRejectedClientRpc(
                $"Bomb on cooldown! Wait {remaining:F1}s",
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } },
                }
            );
            return;
        }

        // ── Feature 2: Active bomb limit ───────────────────────────
        PruneActiveBombs();
        if (activeBombs.Count >= maxActiveBombs)
        {
            Debug.Log(
                $"[Server] Bomb REJECTED (limit) "
                    + $"| SenderClientId: {senderClientId} "
                    + $"| ActiveBombs: {activeBombs.Count}/{maxActiveBombs}"
            );
            NotifyBombRejectedClientRpc(
                $"Bomb limit reached! ({activeBombs.Count}/{maxActiveBombs} active)",
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } },
                }
            );
            return;
        }

        // ── Spawn the bomb ─────────────────────────────────────────
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

        // ── Feature 3: SpawnWithOwnership toggle ───────────────────
        string spawnMethod;
        if (useSpawnWithOwnership)
        {
            bombNetworkObject.SpawnWithOwnership(senderClientId);
            spawnMethod = "SpawnWithOwnership";
        }
        else
        {
            bombNetworkObject.Spawn();
            spawnMethod = "Spawn (server-owned)";
        }

        // Track active bomb and update cooldown
        activeBombs.Add(new NetworkObjectReference(bombNetworkObject));
        lastBombTime = Time.time;

        Debug.Log(
            $"[Server] Bomb spawned from request | SenderClientId: {senderClientId} "
                + $"| SpawnMethod: {spawnMethod} "
                + $"| BombNetworkObjectId: {bombNetworkObject.NetworkObjectId} "
                + $"| BombOwnerClientId: {bombNetworkObject.OwnerClientId} "
                + $"| ActiveBombs: {activeBombs.Count}/{maxActiveBombs} "
                + $"| SpawnPosition: {spawnPosition}"
        );
    }

    /// <summary>
    /// Notify a specific client that their bomb request was rejected.
    /// </summary>
    [ClientRpc]
    private void NotifyBombRejectedClientRpc(
        string reason,
        ClientRpcParams clientRpcParams = default
    )
    {
        Debug.LogWarning($"[Client] Bomb request rejected: {reason}");
    }
}
