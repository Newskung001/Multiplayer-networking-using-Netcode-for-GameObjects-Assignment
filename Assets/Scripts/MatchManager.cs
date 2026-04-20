using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    public NetworkVariable<bool> IsMatchOver = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString64Bytes> WinnerName = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString64Bytes> MatchStatusMessage =
        new NetworkVariable<FixedString64Bytes>(
            "Waiting for players...",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [SerializeField]
    private float checkInterval = 0.2f;

    [SerializeField]
    private int requiredPlayerCount = 2;

    private float checkTimer;
    private readonly System.Collections.Generic.HashSet<ulong> rematchVotes = 
        new System.Collections.Generic.HashSet<ulong>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        ResetMatchState("Waiting for players...");
        checkTimer = checkInterval;

        Debug.Log("[Server] MatchManager started.");
    }

    private void Update()
    {
        if (!IsServer)
            return;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            CheckMatchFlow();
        }
    }

    private void CheckMatchFlow()
    {
        // Use SpawnManager to find player objects accurately on the server.
        var playerHealths = new System.Collections.Generic.List<PlayerHealth>();
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.IsPlayerObject)
            {
                PlayerHealth ph = netObj.GetComponent<PlayerHealth>();
                if (ph != null) playerHealths.Add(ph);
            }
        }
        
        int totalPlayers = playerHealths.Count;

        if (totalPlayers < requiredPlayerCount)
        {
            if (MatchStarted.Value || IsMatchOver.Value)
            {
                Debug.Log("[Server] Player count dropped below required amount. Resetting match.");
            }

            ResetMatchState($"Waiting for players... {totalPlayers}/{requiredPlayerCount}");
            return;
        }

        if (!MatchStarted.Value)
        {
            ResetAllPlayersForNewMatch(playerHealths);

            MatchStarted.Value = true;
            IsMatchOver.Value = false;
            WinnerName.Value = "";
            MatchStatusMessage.Value = "Match Started!";

            Debug.Log($"[Server] Match started with {totalPlayers} players.");
            return;
        }

        if (IsMatchOver.Value)
        {
            return;
        }

        CheckMatchResult(playerHealths);
    }

    private void ResetAllPlayersForNewMatch(System.Collections.Generic.List<PlayerHealth> players)
    {
        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;
            player.RestoreFullHealth();
        }
    }

    private void CheckMatchResult(System.Collections.Generic.List<PlayerHealth> players)
    {
        int aliveCount = 0;
        PlayerHealth lastAlivePlayer = null;

        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            if (player.IsAlive.Value)
            {
                aliveCount++;
                lastAlivePlayer = player;
            }
        }

        if (aliveCount == 1 && lastAlivePlayer != null)
        {
            string winner = $"Player {lastAlivePlayer.OwnerClientId}";

            PlayerStateSync playerStateSync = lastAlivePlayer.GetComponent<PlayerStateSync>();
            if (playerStateSync != null)
            {
                string syncedName = playerStateSync.PlayerName.Value.ToString();
                if (!string.IsNullOrWhiteSpace(syncedName))
                {
                    winner = syncedName;
                }
            }

            IsMatchOver.Value = true;
            WinnerName.Value = winner;
            MatchStatusMessage.Value = $"{winner} Wins!";

            Debug.Log($"[Server] Match Over. Winner = {winner}");
        }
        else if (aliveCount == 0)
        {
            IsMatchOver.Value = true;
            WinnerName.Value = "";
            MatchStatusMessage.Value = "Draw!";

            Debug.Log("[Server] Match Over. Draw.");
        }
    }

    private void ResetMatchState(string statusMessage)
    {
        MatchStarted.Value = false;
        IsMatchOver.Value = false;
        WinnerName.Value = "";
        MatchStatusMessage.Value = statusMessage;
        
        if (IsServer)
        {
            rematchVotes.Clear();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void VoteRematchServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsMatchOver.Value)
            return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        if (rematchVotes.Add(clientId))
        {
            Debug.Log($"[Server] Client {clientId} voted for rematch. Total: {rematchVotes.Count}");

            if (rematchVotes.Count >= requiredPlayerCount)
            {
                Debug.Log("[Server] All players voted for rematch. Restarting...");
                // ResetMatchState already clears rematchVotes, but clear explicitly for safety
                rematchVotes.Clear();
                ResetMatchState("Rematch Starting...");
            }
        }
    }

    public bool CanPlayerMove(ulong clientId)
    {
        // 4 Test Procedure A & 5 Test Procedure B: Host/Client cannot move during the Waiting state.
        if (!MatchStarted.Value)
        {
            return false;
        }

        // 6 Test Procedure C: Host or Client cannot move after the match is over.
        if (IsMatchOver.Value)
        {
            return false;
        }

        // Use SpawnManager to find the specific player object.
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.IsPlayerObject && netObj.OwnerClientId == clientId)
            {
                PlayerHealth ph = netObj.GetComponent<PlayerHealth>();
                if (ph != null) return ph.IsAlive.Value;
            }
        }

        return false;
    }
}
