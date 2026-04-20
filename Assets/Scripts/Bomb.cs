using System.Collections.Generic;
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

    [SerializeField] private int damage = 1;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private LayerMask playerLayerMask = -1;

    private bool exploded = false;

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
        exploded = false;

        Debug.Log(
            $"[Server] Bomb OnNetworkSpawn | NetworkObjectId: {NetworkObjectId} "
                + $"| RequestedByClientId: {requestedByClientId} "
                + $"| Damage: {damage} "
                + $"| Radius: {explosionRadius:F1} "
                + $"| OwnerClientId: {OwnerClientId} "
                + $"| TriggerMode: {(triggerByCollision ? "Collision" : "Timer")} "
                + $"| Lifetime: {lifetime:F1}s "
                + $"| Position: {transform.position}"
        );
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
            Explode();
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

        Explode();
    }

    private void Explode()
    {
        if (!IsServer)
            return;

        if (exploded)
            return;

        exploded = true;

        Debug.Log(
            $"[Server] Bomb explode | NetworkObjectId: {NetworkObjectId} "
                + $"| RequestedByClientId: {requestedByClientId} "
                + $"| OwnerClientId: {OwnerClientId} "
                + $"| Position: {transform.position}"
        );

        ApplyExplosionDamage();
        SpawnExplosionEffect();

        Debug.Log(
            $"[Server] Bomb despawning | NetworkObjectId: {NetworkObjectId} "
                + $"| RequestedByClientId: {requestedByClientId} "
                + $"| OwnerClientId: {OwnerClientId} "
                + $"| Final Position: {transform.position}"
        );

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    private void ApplyExplosionDamage()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            playerLayerMask,
            QueryTriggerInteraction.Collide
        );

        HashSet<ulong> damagedPlayerIds = new HashSet<ulong>();

        Debug.Log(
            $"[Server] Bomb damage check | NetworkObjectId: {NetworkObjectId} "
                + $"| HitColliders: {hits.Length}"
        );

        foreach (Collider hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                continue;
            }

            ulong targetOwnerId = playerHealth.OwnerClientId;
            if (damagedPlayerIds.Contains(targetOwnerId))
            {
                continue;
            }

            damagedPlayerIds.Add(targetOwnerId);

            Debug.Log(
                $"[Server] Bomb hit player | NetworkObjectId: {NetworkObjectId} "
                    + $"| TargetOwnerClientId: {targetOwnerId} "
                    + $"| Damage: {damage}"
            );

            playerHealth.ApplyDamage(damage);
        }

        // Fallback for cases where the collider query misses a player because of
        // hierarchy, trigger, or prefab-layer setup differences.
        DamageConnectedPlayersInRange(damagedPlayerIds);
    }

    private void DamageConnectedPlayersInRange(HashSet<ulong> damagedPlayerIds)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients == null)
        {
            return;
        }

        float radiusSqr = explosionRadius * explosionRadius;

        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client?.PlayerObject == null)
            {
                continue;
            }

            PlayerHealth playerHealth = client.PlayerObject.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                continue;
            }

            ulong targetOwnerId = playerHealth.OwnerClientId;
            if (damagedPlayerIds.Contains(targetOwnerId))
            {
                continue;
            }

            if (!IsPlayerWithinExplosionRadius(playerHealth, radiusSqr))
            {
                continue;
            }

            damagedPlayerIds.Add(targetOwnerId);

            Debug.Log(
                $"[Server] Bomb fallback hit player | NetworkObjectId: {NetworkObjectId} "
                    + $"| TargetOwnerClientId: {targetOwnerId} "
                    + $"| Damage: {damage}"
            );

            playerHealth.ApplyDamage(damage);
        }
    }

    private bool IsPlayerWithinExplosionRadius(PlayerHealth playerHealth, float radiusSqr)
    {
        if (playerHealth == null)
        {
            return false;
        }

        Collider[] colliders = playerHealth.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            Vector3 closestPoint = collider.ClosestPoint(transform.position);
            if ((closestPoint - transform.position).sqrMagnitude <= radiusSqr)
            {
                return true;
            }
        }

        float playerRootDistanceSqr = (playerHealth.transform.position - transform.position)
            .sqrMagnitude;
        return playerRootDistanceSqr <= radiusSqr;
    }

    private void SpawnExplosionEffect()
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning("Explosion prefab is not assigned on Bomb.");
            return;
        }

        GameObject explosionInstance = Instantiate(
            explosionPrefab,
            transform.position,
            Quaternion.identity
        );

        NetworkObject explosionNetworkObject = explosionInstance.GetComponent<NetworkObject>();
        ExplosionEffect explosionEffect = explosionInstance.GetComponent<ExplosionEffect>();

        if (explosionNetworkObject != null)
        {
            explosionNetworkObject.Spawn();

            if (explosionEffect != null)
            {
                explosionEffect.SetRadiusServer(explosionRadius);
            }

            Debug.Log(
                $"[Server] Explosion spawned | FromBombId: {NetworkObjectId} "
                    + $"| ExplosionNetworkObjectId: {explosionNetworkObject.NetworkObjectId} "
                    + $"| Position: {transform.position}"
            );
        }
        else
        {
            Debug.LogError("Explosion prefab is missing NetworkObject.");
            Destroy(explosionInstance);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
