using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Unity.Services.Core;
using Unity.Services.Authentication;

using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class QuickJoinSessionManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject loginPanel;
    public TMP_InputField usernameInput;
    public TMP_Dropdown characterDropdown;
    public Button startButton;
    public Button leaveButton;
    public TMP_Text statusText;

    [Header("Authentication")]
    public string authProfileName = "default";

    [Header("Relay")]
    public int maxConnections = 4;

    [Header("Lobby")]
    public string lobbyName = "MyLobby";
    public int maxPlayers = 4;

    private Lobby currentLobby;
    private Coroutine heartbeatCoroutine;

    private string currentRelayJoinCode;

    private const string JoinCodeKey = "joinCode";

    private bool isSubscribed = false;

    private void Start()
    {
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(false);
        }

        SetStatus("Not Connected");

        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
    }

    private void TrySubscribe()
    {
        if (!isSubscribed && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            isSubscribed = true;
        }
    }

    private void TryUnsubscribe()
    {
        if (isSubscribed && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            isSubscribed = false;
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                SetStatus("Disconnected from server.");
            }
            currentLobby = null;
            StopLobbyHeartbeat();
            ResetUI();
        }
    }

    public async void StartOnline()
    {
        startButton.interactable = false;

        if (string.IsNullOrWhiteSpace(usernameInput.text))
        {
            SetStatus("Please enter username.");
            startButton.interactable = true;
            return;
        }

        try
        {
            await InitializeAndSignInAsync();

            SetStatus("Searching for available Lobby...");

            try
            {
                await JoinLobbyAsClientAsync();
            }
            catch (LobbyServiceException)
            {
                SetStatus("No available Lobby found. Creating new room...");
                await CreateLobbyAsHostAsync();
            }
        }
        catch (System.Exception e)
        {
            SetStatus("Start Online failed: " + e.Message);
            startButton.interactable = true;
        }
    }

    private async Task InitializeAndSignInAsync()
    {
        SetStatus("Initializing Unity Services...");

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions();

            if (!string.IsNullOrWhiteSpace(authProfileName))
            {
                string profile = authProfileName;
                if (profile == "client")
                {
                    profile = "client1";
                }
                options.SetProfile(profile);
            }

            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            SetStatus("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        string activeProfile = AuthenticationService.Instance.Profile;
        SetStatus($"Signed in: {AuthenticationService.Instance.PlayerId} (Profile: {activeProfile})");
    }

    private async Task JoinLobbyAsClientAsync()
    {
        SetStatus("Finding available Lobby...");

        QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

        currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

        SetStatus("Joined Lobby: " + currentLobby.Id);

        if (currentLobby.Data == null || !currentLobby.Data.ContainsKey(JoinCodeKey))
        {
            throw new System.Exception("Lobby does not contain Relay Join Code.");
        }

        string joinCode = currentLobby.Data[JoinCodeKey].Value;

        SetStatus("Joining Relay with code: " + joinCode);

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

        SetConnectedUI();

        SetStatus("Started as Client.");
    }

    private async Task CreateLobbyAsHostAsync()
    {
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

        SetConnectedUI();

        SetStatus("Started as Host. Lobby ID: " + currentLobby.Id);
    }

    private bool PrepareConnectionPayload()
    {
        string userName = usernameInput.text.Trim();
        int characterId = characterDropdown.value;

        if (string.IsNullOrWhiteSpace(userName))
        {
            SetStatus("Please enter username.");
            return false;
        }

        string payload = $"{userName}|{characterId}";

        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            Encoding.UTF8.GetBytes(payload);

        Debug.Log("[QuickJoinSessionManager] Payload prepared: " + payload);

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

    private void SetConnectedUI()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log("[QuickJoinSessionManager] " + message);
    }
    
    private void StopLobbyHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
    }
    
    private void ResetUI()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(false);
        }
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = true;
        }
        SetStatus("Not Connected");
    }
    
    public async void LeaveSession()
    {
        SetStatus("Leaving session...");
        try
        {
            StopLobbyHeartbeat();
            string playerId = AuthenticationService.Instance.PlayerId;
            if (currentLobby != null)
            {
                if (currentLobby.HostId == playerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                    SetStatus("Host deleted Lobby.");
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
                    SetStatus("Client left Lobby.");
                }
                currentLobby = null;
            }
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            ResetUI();
        }
        catch (System.Exception e)
        {
            SetStatus("Leave failed: " + e.Message);
        }
    }
}