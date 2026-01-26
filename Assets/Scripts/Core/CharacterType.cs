//Tracking what "Type" of character this token represents - who are they
public enum CharacterType
{
    //Player characters - who are controlled by the actual users
    Player,
    //NPC - Controlled by the GM but not hostile
    NPC,
    //Enemy - Hostile characters controlled by the GM or by automatic enemy behaviour
    Enemy
}
