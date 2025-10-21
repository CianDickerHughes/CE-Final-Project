using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    /*
    EXPLANATION OF CODE FOR FUTURE USE
    This script will only help us with navigation between the various pages
    To add new navigation features:
    - Add the scene you want to travel to, to the build path
    - Add a new method here for moving to the scene in the actual project
    - Change the name in the LoadSceneAsync() method to the scene you want e.g. "SampleScene"
    - Go back to the project & hook up the button to this method & drag across whatever controller/manager we may be using
    */

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
        //This is the exact same as the one above - it only switches to the "Create Game" scene
        SceneManager.LoadSceneAsync("GameScene");
    }
    
}
