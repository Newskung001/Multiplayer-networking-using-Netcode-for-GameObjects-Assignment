using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Collections;

public class OnlineSessionManager : MonoBehaviour
{
    public GameObject loginPanel;
    public TMP_InputField usernameInput;
    public CharacterSelectUI characterSelectPanel;
    public Button startButton;
    public Button leaveButton;
    public TMP_Text statusText;
    
    public int maxConnections = 4;
    public TMP_InputField relayJoinCodeInput;
    private string currentRelayJoinCode;
    
    [Header("Lobby")]
    public string lobbyName = "My Lobby";
    public int maxPlayers = 4;
    private Lobby currentLobby;
    private Coroutine heartbeatCoroutine;
    private const string JoinCodeKey = "joinCode";
    
    [Header("Authentication")]
    public string authProfileName = "default";

    private void Start()
    {
        leaveButton.gameObject.SetActive(false);
        SetStatus("Not Connected");
    }
    
    private async Task InitializeAndSignInAsync()
    {
        SetStatus("Initializing Unity Services...");

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                authProfileName = "clone";
            }
#endif

            if (!string.IsNullOrWhiteSpace(authProfileName))
            {
                options.SetProfile(authProfileName);
            }

            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            SetStatus("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        string playerId = AuthenticationService.Instance.PlayerId;
        SetStatus("Signed in: " + playerId);
    }

    public async void StartRelayHost()
    {
        startButton.interactable = false;

        try
        {
            await InitializeAndSignInAsync();

            SetStatus("Creating Relay Allocation...");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            currentRelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            SetStatus("Relay Join Code: " + currentRelayJoinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls")
            );

            string userName = usernameInput.GetComponent<TMP_InputField>().text;
            int characterId = characterSelectPanel != null ? characterSelectPanel.SelectedCharacterIndex : 0;
            SetConnectionData(userName, characterId);
            NetworkManager.Singleton.StartHost();

            SetStatus("Started as Host. Join Code: " + currentRelayJoinCode);
        }
        catch (System.Exception e)
        {
            SetStatus("Relay Host failed: " + e.Message);
            startButton.interactable = true;
        }
    }
    
    public async void StartRelayClient()
    {
        startButton.interactable = false;
        try
        {
            await InitializeAndSignInAsync();
            string joinCode = relayJoinCodeInput.text.Trim();
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                SetStatus("Please enter Relay Join Code.");
                startButton.interactable = true;
                return;
            }

            SetStatus("Joining Relay Allocation...");
            JoinAllocation joinAllocation =
                await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(joinAllocation, "dtls")
            );

            string userName = usernameInput.GetComponent<TMP_InputField>().text;
            int characterId = characterSelectPanel != null ? characterSelectPanel.SelectedCharacterIndex : 0;
            SetConnectionData(userName, characterId);
            NetworkManager.Singleton.StartClient();
            SetStatus("Started as Client.");
        }
        catch (System.Exception e)
        {
            SetStatus("Relay Client failed: " + e.Message);
            startButton.interactable = true;
        }
    }
    
    private void SetConnectionData(string username, int characterId)
    {
        string payload = $"{username}|{characterId}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            Encoding.UTF8.GetBytes(payload);
    }
    
    private bool PrepareConnectionPayload()
    {
        string userName = usernameInput.text.Trim();
        int characterId = characterSelectPanel != null ? characterSelectPanel.SelectedCharacterIndex : 0;
        if (string.IsNullOrWhiteSpace(userName))
        {
            SetStatus("Please enter username.");
            return false;
        }
        SetConnectionData(userName, characterId);
        Debug.Log($"[OnlineSessionManager] Payload prepared: {userName}|{characterId}");
        return true;
    }
    
    private void StartLobbyHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
        }
        heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine());
    }

    private IEnumerator HeartbeatLobbyCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(15f);
        while (currentLobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            yield return wait;
        }
    }
    
    public async void StartOnline()
    {
        string playerName = usernameInput.text;
        int characterId = characterSelectPanel != null ? characterSelectPanel.SelectedCharacterIndex : 0;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetStatus("Please enter username.");
            return;
        }

        startButton.interactable = false;

        try
        {
            await InitializeAndSignInAsync();

            SetStatus($"Ready: {playerName}, Character {characterId}");
        }
        catch (System.Exception e)
        {
            SetStatus("Sign in failed: " + e.Message);
            startButton.interactable = true;
        }
    }
    
    public async void StartLobbyHost()
    {
        startButton.interactable = false;

        try
        {
            await InitializeAndSignInAsync();

            SetStatus("Creating Relay Allocation...");

            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(maxConnections);

            currentRelayJoinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            SetStatus("Creating Lobby...");

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        JoinCodeKey,
                        new DataObject(
                            DataObject.VisibilityOptions.Member,
                            currentRelayJoinCode
                        )
                    }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName,
                maxPlayers,
                options
            );

            StartLobbyHeartbeat();

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls")
            );

            if (!PrepareConnectionPayload())
            {
                startButton.interactable = true;
                return;
            }

            NetworkManager.Singleton.StartHost();

            SetStatus("Started Host with Lobby: " + currentLobby.Id);
        }
        catch (System.Exception e)
        {
            SetStatus("Create Lobby Host failed: " + e.Message);
            startButton.interactable = true;
        }
    }
    
    public async void JoinLobbyClient()
    {
        startButton.interactable = false;
    
        try
        {
            await InitializeAndSignInAsync();
    
            SetStatus("Finding available Lobby...");
    
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();
    
            currentLobby =
                await LobbyService.Instance.QuickJoinLobbyAsync(options);
    
            SetStatus("Joined Lobby: " + currentLobby.Id);
    
            string joinCode =
                currentLobby.Data[JoinCodeKey].Value;
    
            SetStatus("Relay Join Code from Lobby: " + joinCode);
    
            JoinAllocation joinAllocation =
                await RelayService.Instance.JoinAllocationAsync(joinCode);
    
            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();
    
            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(joinAllocation, "dtls")
            );
    
            if (!PrepareConnectionPayload())
            {
                startButton.interactable = true;
                return;
            }
    
            NetworkManager.Singleton.StartClient();
    
            SetStatus("Started Client from Lobby.");
        }
        catch (System.Exception e)
        {
            SetStatus("Join Lobby Client failed: " + e.Message);
            startButton.interactable = true;
        }
    }

    private void SetStatus(string message)
    {
        statusText.text = message;
        Debug.Log("[OnlineSessionManager] " + message);
    }
}

