using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStateSync : NetworkBehaviour
{
    [Header("Name UI")]
    [SerializeField] private TMP_Text namePrefab;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    [Header("Status Visual")]
    [SerializeField] private Renderer statusRenderer;

    private TMP_Text nameLabel;
    private GameObject noUsesPanel;
    private TextMeshProUGUI noUsesText;
    private CanvasGroup noUsesCanvasGroup;
    private Coroutine noUsesFlashCoroutine;
    private Camera mainCam;

    private PlayerRpcDemo rpcDemo;

    private const float NoUsesWarningPadding = 10f;
    private const string NoUsesTextString = "NO USES LEFT";
    private const float NoUsesFlashDuration = 0.6f;
    private const float NoUsesFlashMinAlpha = 0.25f;
    private const float NoUsesFlashMaxAlpha = 1f;


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
        CreateNoUsesWarningLabel();

        UpdateNameUI(PlayerName.Value.ToString());
        UpdateStatusVisual(IsSpecialStatus.Value);

        rpcDemo = GetComponent<PlayerRpcDemo>();
        if (rpcDemo != null)
        {
            rpcDemo.ItemUseCount.OnValueChanged += OnItemUseCountChanged;
            // Ensure the warning label matches the initial synchronized value.
            UpdateNameUI(PlayerName.Value.ToString());
        }

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

        if (rpcDemo != null)
        {
            rpcDemo.ItemUseCount.OnValueChanged -= OnItemUseCountChanged;
        }

        DestroyNameLabel();
        DestroyNoUsesWarningLabel();
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

    private void CreateNoUsesWarningLabel()
    {
        GameObject canvas = GameObject.FindWithTag("MainCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("MainCanvas with tag 'MainCanvas' not found.");
            return;
        }

        noUsesPanel = new GameObject("NoUsesWarningPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        noUsesPanel.transform.SetParent(canvas.transform, false);

        var panelRect = noUsesPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-NoUsesWarningPadding, NoUsesWarningPadding);
        panelRect.sizeDelta = new Vector2(280f, 60f);

        var panelImage = noUsesPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);

        noUsesCanvasGroup = noUsesPanel.AddComponent<CanvasGroup>();
        noUsesCanvasGroup.alpha = 0f;

        GameObject textGO = new GameObject("NoUsesWarningText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(noUsesPanel.transform, false);

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(10f, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);

        noUsesText = textGO.GetComponent<TextMeshProUGUI>();
        noUsesText.text = NoUsesTextString;
        noUsesText.alignment = TextAlignmentOptions.Center;
        noUsesText.fontSize = 22;
        noUsesText.color = Color.red;
        noUsesText.enableAutoSizing = true;

        noUsesPanel.SetActive(false);
    }

    private void DestroyNameLabel()
    {
        if (nameLabel != null)
        {
            Destroy(nameLabel.gameObject);
            nameLabel = null;
        }
    }

    private void DestroyNoUsesWarningLabel()
    {
        if (noUsesPanel != null)
        {
            Destroy(noUsesPanel);
            noUsesPanel = null;
            noUsesText = null;
            noUsesCanvasGroup = null;
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

    private void OnItemUseCountChanged(int oldValue, int newValue)
    {
        // Update the UI label when the synchronized item use count changes
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

        if (rpcDemo != null)
        {
            int uses = rpcDemo.ItemUseCount.Value;
            display += $" - Uses: {uses}";

            UpdateNoUsesWarning(uses);
        }

        nameLabel.text = display;
    }

    private void UpdateNoUsesWarning(int remainingUses)
    {
        if (noUsesPanel == null) return;

        bool showWarning = remainingUses <= 0;
        if (showWarning)
        {
            if (!noUsesPanel.activeSelf)
            {
                noUsesPanel.SetActive(true);
                StartNoUsesFlash();
            }
        }
        else
        {
            StopNoUsesFlash();
            noUsesPanel.SetActive(false);
        }
    }

    private void StartNoUsesFlash()
    {
        if (noUsesCanvasGroup == null) return;
        if (noUsesFlashCoroutine != null) return;

        noUsesFlashCoroutine = StartCoroutine(NoUsesFlashRoutine());
    }

    private void StopNoUsesFlash()
    {
        if (noUsesFlashCoroutine != null)
        {
            StopCoroutine(noUsesFlashCoroutine);
            noUsesFlashCoroutine = null;
        }

        if (noUsesCanvasGroup != null)
        {
            noUsesCanvasGroup.alpha = 0f;
        }
    }

    private IEnumerator NoUsesFlashRoutine()
    {
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.PingPong(elapsed, NoUsesFlashDuration) / NoUsesFlashDuration;
            float alpha = Mathf.Lerp(NoUsesFlashMinAlpha, NoUsesFlashMaxAlpha, t);
            if (noUsesCanvasGroup != null)
                noUsesCanvasGroup.alpha = alpha;
            yield return null;
        }
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