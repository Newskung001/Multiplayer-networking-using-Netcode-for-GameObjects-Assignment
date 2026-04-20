using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHP = 3;

    public NetworkVariable<int> CurrentHP = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int MaxHP => maxHP;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHP.Value = maxHP;
            IsAlive.Value = true;

            Debug.Log($"[Server] Initialize HP for player {OwnerClientId} = {CurrentHP.Value}");
        }

        CurrentHP.OnValueChanged += OnHPChanged;
        IsAlive.OnValueChanged += OnAliveChanged;

        Debug.Log($"[OnNetworkSpawn] Player {OwnerClientId} spawned with HP = " +
                  $"{CurrentHP.Value}, Alive = {IsAlive.Value}");
    }

    public override void OnNetworkDespawn()
    {
        CurrentHP.OnValueChanged -= OnHPChanged;
        IsAlive.OnValueChanged -= OnAliveChanged;
    }

    private void OnHPChanged(int previousValue, int newValue)
    {
        Debug.Log($"[HP Changed] Player {OwnerClientId}: {previousValue} -> {newValue}");
    }

    private void OnAliveChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"[Alive Changed] Player {OwnerClientId}: {previousValue} -> {newValue}");
    }

    public void ApplyDamage(int damageAmount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[ApplyDamage] Only the server should change HP.");
            return;
        }

        if (!IsAlive.Value)
        {
            Debug.Log($"[Server] Player {OwnerClientId} is already defeated. Ignore damage.");
            return;
        }

        int newHP = Mathf.Max(CurrentHP.Value - damageAmount, 0);
        CurrentHP.Value = newHP;

        Debug.Log($"[Server] Damage applied to player {OwnerClientId}, damage = {damageAmount}, HP now = {CurrentHP.Value}");

        if (CurrentHP.Value <= 0)
        {
            IsAlive.Value = false;
            Debug.Log($"[Server] Player {OwnerClientId} is defeated.");
        }
    }

    public void RestoreFullHealth()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[RestoreFullHealth] Only the server should restore HP.");
            return;
        }

        CurrentHP.Value = maxHP;
        IsAlive.Value = true;

        Debug.Log($"[Server] Restore full health for player {OwnerClientId}");
    }
    
    [ContextMenu("Test Damage -1 (Server Only)")]
    private void TestDamage()
    {
        if (IsServer)
        {
            ApplyDamage(1);
        }
    }
}
