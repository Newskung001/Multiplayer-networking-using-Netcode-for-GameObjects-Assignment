using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Character Buttons")]
    [SerializeField] private Button[] characterButtons;  // 3 buttons

    private int selectedIndex = 0;

    public int SelectedCharacterIndex => selectedIndex;

    private void Start()
    {
        // Wire up button clicks
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i; // capture for lambda
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        SelectCharacter(0); // default selection
    }

    public void SelectCharacter(int index)
    {
        selectedIndex = index;

        // Update button visuals - selected stays normal, others are grayed out
        for (int i = 0; i < characterButtons.Length; i++)
        {
            Image img = characterButtons[i].image;
            if (img != null)
            {
                img.color = (i == index) ? Color.white : Color.gray;
            }
        }
    }
}
