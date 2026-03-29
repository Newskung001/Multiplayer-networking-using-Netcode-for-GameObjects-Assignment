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
}
