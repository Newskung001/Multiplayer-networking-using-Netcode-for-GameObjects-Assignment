using Unity.Netcode;
using UnityEngine;

public class Bomb : NetworkBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField]
    private float lifetime = 3f;

    [Header("Explosion")]
    [SerializeField]
    private GameObject explosionPrefab;

    [Header("Collision Trigger (Feature 6)")]
    [Tooltip("When true, the bomb detonates on contact with another object instead of using a timer.")]
    [SerializeField]
    private bool triggerByCollision = false;

    [Tooltip("Seconds after spawn before the bomb can be triggered by collision (prevents instant self-detonation).")]
    [SerializeField]
    private float armDelay = 0.5f;

    private float timer;
    private int lastLoggedSecond = -1;
    private ulong requestedByClientId = ulong.MaxValue;
    private bool isArmed = false;
    private float armTimer;

    /// <summary>
    /// (Feature 5) Networked requester ID so all clients can see who placed the bomb.
    /// </summary>
    public NetworkVariable<ulong> NetworkedRequesterId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public void SetRequestedByClientId(ulong clientId)
    {
        requestedByClientId = clientId;
    }

    public override void OnNetworkSpawn()
    {
        // ── Feature 5: Sync requester ID to all clients ────────────
        if (IsServer)
        {
            NetworkedRequesterId.Value = requestedByClientId;
        }

        // All clients log who placed this bomb
        Debug.Log(
            $"[{(IsServer ? "Server" : "Client")}] Bomb OnNetworkSpawn "
                + $"| NetworkObjectId: {NetworkObjectId} "
                + $"| RequestedByClientId: {NetworkedRequesterId.Value} "
                + $"| OwnerClientId: {OwnerClientId} "
                + $"| IsServer: {IsServer} "
                + $"| IsOwner: {IsOwner} "
                + $"| OwnerMatchesRequester: {OwnerClientId == NetworkedRequesterId.Value} "
                + $"| TriggerMode: {(triggerByCollision ? "Collision" : "Timer")} "
                + $"| Lifetime: {lifetime:F1}s "
                + $"| Position: {transform.position}"
        );

        if (!IsServer)
            return;

        timer = lifetime;
        lastLoggedSecond = Mathf.CeilToInt(timer);
        armTimer = armDelay;
        isArmed = false;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        // ── Feature 6: Arm delay countdown ─────────────────────────
        if (!isArmed)
        {
            armTimer -= Time.deltaTime;
            if (armTimer <= 0f)
            {
                isArmed = true;
                if (triggerByCollision)
                {
                    Debug.Log(
                        $"[Server] Bomb ARMED for collision "
                            + $"| NetworkObjectId: {NetworkObjectId} "
                            + $"| RequestedByClientId: {requestedByClientId}"
                    );
                }
            }
        }

        // ── Feature 6: Skip timer countdown when using collision mode
        if (triggerByCollision)
            return;

        // ── Timer-based countdown (original behavior) ──────────────
        timer -= Time.deltaTime;

        int currentSecond = Mathf.CeilToInt(timer);
        if (currentSecond != lastLoggedSecond && currentSecond >= 0)
        {
            lastLoggedSecond = currentSecond;
            Debug.Log(
                $"[Server] Bomb timer | NetworkObjectId: {NetworkObjectId} "
                    + $"| RequestedByClientId: {requestedByClientId} "
                    + $"| OwnerClientId: {OwnerClientId} "
                    + $"| Remaining: {currentSecond}s"
            );
        }

        if (timer <= 0f)
        {
            Detonate();
        }
    }

    /// <summary>
    /// Feature 6: Collision-triggered detonation.
    /// Requires the Bomb prefab's collider to have "Is Trigger" enabled.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (!triggerByCollision)
            return;

        if (!isArmed)
            return;

        // Only detonate on objects that have a NetworkObject (i.e., players or networked entities)
        NetworkObject otherNetObj = other.GetComponentInParent<NetworkObject>();
        if (otherNetObj == null)
            return;

        // Don't detonate from the bomb's own collider interactions
        if (otherNetObj == NetworkObject)
            return;

        Debug.Log(
            $"[Server] Bomb COLLISION triggered "
                + $"| BombNetworkObjectId: {NetworkObjectId} "
                + $"| RequestedByClientId: {requestedByClientId} "
                + $"| CollidedWithNetworkObjectId: {otherNetObj.NetworkObjectId} "
                + $"| CollidedWithOwnerClientId: {otherNetObj.OwnerClientId}"
        );

        Detonate();
    }

    /// <summary>
    /// Spawn the explosion effect and despawn the bomb.
    /// Used by both timer and collision trigger paths.
    /// </summary>
    private void Detonate()
    {
        if (explosionPrefab != null)
        {
            GameObject explosionInstance = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
            NetworkObject explosionNetworkObject =
                explosionInstance.GetComponent<NetworkObject>();
            if (explosionNetworkObject != null)
            {
                explosionNetworkObject.Spawn();
                Debug.Log(
                    $"[Server] Explosion spawned | FromBombId: {NetworkObjectId} "
                        + $"| RequestedByClientId: {requestedByClientId} "
                        + $"| ExplosionNetworkObjectId: {explosionNetworkObject.NetworkObjectId} "
                        + $"| TriggerMode: {(triggerByCollision ? "Collision" : "Timer")} "
                        + $"| Position: {transform.position}"
                );
            }
            else
            {
                Debug.LogError("Explosion prefab is missing NetworkObject.");
                Destroy(explosionInstance);
            }
        }
        else
        {
            Debug.LogWarning("Explosion prefab is not assigned on Bomb.");
        }

        Debug.Log(
            $"[Server] Bomb despawning | NetworkObjectId: {NetworkObjectId} "
                + $"| RequestedByClientId: {requestedByClientId} "
                + $"| OwnerClientId: {OwnerClientId} "
                + $"| TriggerMode: {(triggerByCollision ? "Collision" : "Timer")} "
                + $"| Final Position: {transform.position}"
        );
        NetworkObject.Despawn();
    }
}
