using Unity.Netcode;
using UnityEngine;

public class Bomb : NetworkBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField]
    private float lifetime = 3f;

    private float timer;
    private int lastLoggedSecond = -1;
    private ulong requestedByClientId = ulong.MaxValue;

    public void SetRequestedByClientId(ulong clientId)
    {
        requestedByClientId = clientId;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        timer = lifetime;
        lastLoggedSecond = Mathf.CeilToInt(timer);

        Debug.Log(
            $"[Server] Bomb OnNetworkSpawn | NetworkObjectId: {NetworkObjectId} "
                + $"| RequestedByClientId: {requestedByClientId} "
                + $"| OwnerClientId: {OwnerClientId} "
                + $"| IsServer: {IsServer} "
                + $"| IsOwner: {IsOwner} "
                + $"| Lifetime: {lifetime:F1}s "
                + $"| Position: {transform.position}"
        );
    }

    private void Update()
    {
        if (!IsServer)
            return;

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
            Debug.Log(
                $"[Server] Bomb despawning | NetworkObjectId: {NetworkObjectId} "
                    + $"| RequestedByClientId: {requestedByClientId} "
                    + $"| OwnerClientId: {OwnerClientId} "
                    + $"| Final Position: {transform.position}"
            );
            NetworkObject.Despawn();
        }
    }
}
