using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;

    private void Start()
    {
        if (resultText == null)
        {
            resultText = GetComponent<TMP_Text>();
        }

        if (MatchManager.Instance != null)
        {
            // Initial update
            UpdateResultText("", MatchManager.Instance.MatchStatusMessage.Value);

            // Subscribe to changes
            MatchManager.Instance.MatchStatusMessage.OnValueChanged += UpdateResultText;
        }
    }

    private void OnDestroy()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.MatchStatusMessage.OnValueChanged -= UpdateResultText;
        }
    }

    private void UpdateResultText(Unity.Collections.FixedString64Bytes previousValue, Unity.Collections.FixedString64Bytes newValue)
    {
        if (resultText != null)
        {
            resultText.text = newValue.ToString();
        }
    }
}
