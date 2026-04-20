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

    [Header("Range Visual")]
    [SerializeField] private Transform rangeIndicator;
    [SerializeField] private float indicatorHeight = 0.05f;

    private float timer;

    private NetworkVariable<float> syncedRadius = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

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

        syncedRadius.OnValueChanged += OnRadiusChanged;
        ApplyRadiusVisual(syncedRadius.Value);

        if (!IsServer) return;

        timer = lifetime;
        Debug.Log($"[Server] ExplosionEffect spawned " +
                  $"| NetworkObjectId: {NetworkObjectId} " +
                  $"| Lifetime: {lifetime:F1}s");
    }

    public override void OnNetworkDespawn()
    {
        syncedRadius.OnValueChanged -= OnRadiusChanged;
    }

    // TODO: Call SetRadiusServer from the server-side spawning logic.
    public void SetRadiusServer(float radius)
    {
        if (!IsServer) return;

        syncedRadius.Value = radius;
        ApplyRadiusVisual(radius);
    }

    private void OnRadiusChanged(float previousValue, float newValue)
    {
        ApplyRadiusVisual(newValue);
    }

    private void ApplyRadiusVisual(float radius)
    {
        if (rangeIndicator == null) return;

        float diameter = radius * 2f;
        Vector3 scale = rangeIndicator.localScale;
        scale.x = diameter;
        scale.z = diameter;
        scale.y = indicatorHeight;
        rangeIndicator.localScale = scale;
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