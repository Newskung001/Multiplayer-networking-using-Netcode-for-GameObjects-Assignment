using Unity.Netcode;
using UnityEngine;

public class ExplosionEffect : NetworkBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float lifetime = 0.8f;

    [Header("Audio")]
    [Tooltip("AudioClip to play when the explosion spawns. Plays on all clients automatically.")]
    [SerializeField] private AudioClip explosionSound;

    [Tooltip("Volume of the explosion sound (0 = silent, 1 = full).")]
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

    private float timer;

    public override void OnNetworkSpawn()
    {
        // ── Play explosion sound on ALL clients (not just server) ──
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, soundVolume);
        }
        else
        {
            Debug.LogWarning("[ExplosionEffect] No explosion sound assigned.");
        }

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