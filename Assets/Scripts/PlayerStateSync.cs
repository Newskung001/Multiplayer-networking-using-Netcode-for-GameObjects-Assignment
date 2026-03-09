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

    public override void OnNetworkSpawn()
    {
        mainCam = Camera.main;

        PlayerName.OnValueChanged += OnPlayerNameChanged;
        IsSpecialStatus.OnValueChanged += OnStatusChanged;

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
        }
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnPlayerNameChanged;
        IsSpecialStatus.OnValueChanged -= OnStatusChanged;

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

    private void UpdateNameUI(string newName)
    {
        if (nameLabel != null)
        {
            nameLabel.text = newName;
            //nameLabel.color = Color.red;
        }
    }

    private void UpdateStatusVisual(bool active)
    {
        if (statusRenderer == null) return;

        statusRenderer.material.color = active ? Color.red : Color.white;
    }
}