using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchStatusUI : MonoBehaviour
{
    public enum UIMode
    {
        PlayerList,    // Displays the list of players with HP/Alive status
        MatchResult    // Displays the MatchStatusMessage (Waiting, Started, Winner, etc.)
    }

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private float refreshInterval = 0.2f;

    [SerializeField]
    private UIMode mode = UIMode.PlayerList;

    private float refreshTimer;

    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            RefreshBoard();
        }
    }

    private void RefreshBoard()
    {
        if (statusText == null)
        {
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            statusText.text = "Players\nNot Connected";
            return;
        }

        if (mode == UIMode.MatchResult)
        {
            if (MatchManager.Instance != null)
            {
                statusText.text = MatchManager.Instance.MatchStatusMessage.Value.ToString();
            }
            return;
        }

        // --- PlayerList Mode ---
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Players");

        // Use SpawnManager to find player objects since ConnectedClientsList is server-side only.
        var playerObjects = new System.Collections.Generic.List<NetworkObject>();
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj.IsPlayerObject)
            {
                playerObjects.Add(netObj);
            }
        }

        // Sort by OwnerClientId to keep a consistent display order.
        playerObjects.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

        foreach (var playerNetObj in playerObjects)
        {
            GameObject playerObject = playerNetObj.gameObject;

            PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
            PlayerStateSync playerStateSync = playerObject.GetComponent<PlayerStateSync>();

            string playerName = $"Player {playerNetObj.OwnerClientId}";
            if (playerStateSync != null)
            {
                playerName = playerStateSync.PlayerName.Value.ToString();
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    playerName = $"Player {playerNetObj.OwnerClientId}";
                }
            }

            string hpText = "HP: ?";
            string stateText = "Unknown";

            if (playerHealth != null)
            {
                hpText = $"HP: {playerHealth.CurrentHP.Value}";
                stateText = playerHealth.IsAlive.Value ? "Alive" : "Defeated";
            }

            sb.AppendLine($"{playerName} | {hpText} | {stateText}");
        }

        statusText.text = sb.ToString();
    }
}
