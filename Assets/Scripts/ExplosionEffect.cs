using Unity.Netcode;
using UnityEngine;

public class ExplosionEffect : NetworkBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float lifetime = 0.8f;

    private float timer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        timer = lifetime;
        Debug.Log($"[Server] ExplosionEffect spawned " +
                  $"| NetworkObjectId: {NetworkObjectId} " +
                  $"| Lifetime: {lifetime:F1}s");
    }

    private void Update()
    {
        if (!IsServer) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Debug.Log($"[Server] ExplosionEffect despawning " +
                      $"| NetworkObjectId: {NetworkObjectId}");
            NetworkObject.Despawn();
        }
    }
}