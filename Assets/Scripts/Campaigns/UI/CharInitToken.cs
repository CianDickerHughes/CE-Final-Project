using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Class is meant to serve to populate the UI item for combatants like i bg3 in the gameplay scene
public class CharInitToken : MonoBehaviour
{
    //UI Fields
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image tokenImage;

    private CombatParticipant participant;

    //Method to set up the combatant token with the relevant data
    public void Setup(CombatParticipant combatParticipant)
    {
        participant = combatParticipant;
        UpdateHP();
        //Setting up the token image

    }

    public void UpdateHP()
    {
        if(hpText != null)
        {
            hpText.text = $"{participant.currentHP}/{participant.maxHP}";
        }
    }
}
