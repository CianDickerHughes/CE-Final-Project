using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{

    //Method we will use to actually switch from main menu to playing the game
    public void PlayGame()
    {
        //This actually switches us to the scene we want to go to - change the name here if we want to go to a different scene
        SceneManager.LoadSceneAsync("SampleScene");
    }

    public void CreateCharacter()
    {
        //This is the exact same as the one above - it only switches to the character creator screen
        SceneManager.LoadSceneAsync("CreateCharacter");
    }

    public void CreateGame()
    {
        //This is the exact same as the one above - it only switches to the character creator screen
        SceneManager.LoadSceneAsync("CreateGame");
    }
    
}
