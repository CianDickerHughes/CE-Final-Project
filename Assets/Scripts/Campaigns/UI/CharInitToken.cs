using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Class is meant to serve to populate the UI item for combatants like i bg3 in the gameplay scene
public class CharInitToken : MonoBehaviour
{
    //UI Fields
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image tokenImage;
    [SerializeField] private Image backgroundImage;

    //Highligting UI fields
    [Header("Highlight Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.8f, 0.4f, 1f);

    private CombatParticipant participant;

    //Method to set up the combatant token with the relevant data
    public void Setup(CombatParticipant combatParticipant)
    {
        participant = combatParticipant;
        UpdateHP();
        //Setting up the token image
        SetupTokenImage();
        //Setting up the background color to normal by default
        SetHighlight(false);
    }

    //UI method for highlighting and Unhighlighting an item in the initiative order when its that players turn
    public void SetHighlight(bool isActive)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isActive ? highlightColor : normalColor;
        }
    }

    private void SetupTokenImage()
    {
        if (tokenImage != null && participant.token != null)
        {
            // Get the sprite from the token's SpriteRenderer
            SpriteRenderer spriteRenderer = participant.token.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                tokenImage.sprite = spriteRenderer.sprite;
                tokenImage.preserveAspect = true;
            }
        }
    }

    public void UpdateHP()
    {
        if(hpText != null)
        {
            hpText.text = $"{participant.currentHP}/{participant.maxHP}";
        }
    }
}
