using Unity.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;

public class PlayerStateSync : NetworkBehaviour
{
    [Header("Name UI")]
    [SerializeField] private TMP_Text namePrefab;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    [Header("Status Visual")]
    [SerializeField] private Renderer statusRenderer;

    private TMP_Text nameLabel;
    private Camera mainCam;

    public NetworkVariable<FixedString64Bytes> PlayerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> IsSpecialStatus =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    // additional networked property to demonstrate a team or color index
    public NetworkVariable<int> TeamIndex =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        mainCam = Camera.main;

        PlayerName.OnValueChanged += OnPlayerNameChanged;
        IsSpecialStatus.OnValueChanged += OnStatusChanged;
        TeamIndex.OnValueChanged += OnTeamChanged;

        CreateNameLabel();
        UpdateNameUI(PlayerName.Value.ToString());
        UpdateStatusVisual(IsSpecialStatus.Value);

        if (IsOwner && ConnectionManager.Instance != null)
        {
            string localName = ConnectionManager.Instance.LocalUsername;

            if (!string.IsNullOrWhiteSpace(localName))
            {
                PlayerName.Value = localName;
            }

            // initialize team based on local character selection (could be mapped differently)
            TeamIndex.Value = ConnectionManager.Instance.LocalCharacterId;

            // ensure color is applied immediately (OnTeamChanged may not fire if value stays at default)
            ApplyTeamColor();
            UpdateNameUI(PlayerName.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnPlayerNameChanged;
        IsSpecialStatus.OnValueChanged -= OnStatusChanged;
        TeamIndex.OnValueChanged -= OnTeamChanged;

        DestroyNameLabel();
    }

    private void Update()
    {
        UpdateNameLabelPosition();
    }

    public void OnAttack()
    {
        if (!IsOwner) return;
        IsSpecialStatus.Value = !IsSpecialStatus.Value;
    }

    private void CreateNameLabel()
    {
        if (namePrefab == null) return;

        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("MainCanvas with tag 'MainCanvas' not found.");
            return;
        }

        nameLabel = Instantiate(namePrefab, Vector3.zero, Quaternion.identity);
        nameLabel.transform.SetParent(canvas.transform, false);
    }

    private void DestroyNameLabel()
    {
        if (nameLabel != null)
        {
            Destroy(nameLabel.gameObject);
            nameLabel = null;
        }
    }

    private void UpdateNameLabelPosition()
    {
        if (nameLabel == null) return;

        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam == null) return;

        Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + worldOffset);
        
        bool visible = screenPos.z > 0f;
        nameLabel.gameObject.SetActive(visible);

        if (visible)
        {
            nameLabel.transform.position = screenPos;
        }
    }

    private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        UpdateNameUI(newValue.ToString());
    }

    private void OnStatusChanged(bool oldValue, bool newValue)
    {
        UpdateStatusVisual(newValue);
    }

    private void OnTeamChanged(int oldValue, int newValue)
    {
        ApplyTeamColor();
        // also refresh the displayed name text to include the team and host tag
        UpdateNameUI(PlayerName.Value.ToString());
    }

    private void UpdateNameUI(string newName)
    {
        if (nameLabel == null) return;

        string teamText = GetTeamName(TeamIndex.Value);
        bool isHostPlayer = OwnerClientId == NetworkManager.ServerClientId;

        // build the display text
        string display = newName;
        if (!string.IsNullOrEmpty(teamText))
            display += $" [{teamText}]";
        if (isHostPlayer)
            display += " (Host)";

        nameLabel.text = display;
        //nameLabel.color = Color.red;
    }

    private string GetTeamName(int index)
    {
        // map the integer index to a human-readable team name
        switch (index)
        {
            case 1: return "Yellow";
            case 2: return "Pink";
            default: return "Gray"; // index 0 or unknown
        }
    }

    private void ApplyTeamColor()
    {
        if (nameLabel == null) return;

        bool isHostPlayer = OwnerClientId == NetworkManager.ServerClientId;
        if (isHostPlayer)
        {
            nameLabel.color = Color.green;
            return;
        }

        switch (TeamIndex.Value)
        {
            case 1:
                nameLabel.color = Color.yellow;
                break;
            case 2:
                nameLabel.color = Color.magenta; // pinkish
                break;
            default:
                nameLabel.color = Color.white;
                break;
        }
    }

    private void UpdateStatusVisual(bool active)
    {
        if (statusRenderer == null) return;

        statusRenderer.material.color = active ? Color.red : Color.white;
    }
}