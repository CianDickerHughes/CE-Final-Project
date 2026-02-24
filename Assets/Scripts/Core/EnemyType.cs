//Tracking what "Type" of enemy this is
public enum EnemyType
{
    //Manual - The DM is manually controlling this enemy themselves
    Manual,
    //Boss/Adaptive - Enemy is controlled by the adaptive enemy AI behaviour - learns/has actions informed by the specialised AI model in the system
    Adaptive,
    //Dungeon Master - Potential Highest difficulty enemy type - adaptive and can counter instantly when adapting
    DungeonMaster
}
