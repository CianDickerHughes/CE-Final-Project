using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Class is meant to serve to populate the UI item for combatants like i bg3 in the gameplay scene
public class CharInitToken : MonoBehaviour
{
    //UI Fields
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image tokenImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    //Method to set up the combatant token with the relevant data
    public void Setup(Token token)
    {
        if (hpText != null)
        {
            if(token.getCharacterType() == CharacterType.Player && token.getCharacterData() != null)
            {
                hpText.text = $"HP: {token.getCharacterData().HP}";
            }
            else if(token.getCharacterType() == CharacterType.Enemy && token.getEnemyData() != null)
            {
                hpText.text = $"HP: {token.getEnemyData().HP}";
            }
            else
            {
                hpText.text = "HP: N/A";
            }
        }
    }
}
