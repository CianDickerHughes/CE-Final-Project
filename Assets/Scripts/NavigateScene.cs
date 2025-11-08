using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenu : MonoBehaviour
{
    /*
    EXPLANATION OF CODE FOR FUTURE USE
    This script will only help us with navigation between the various pages
    To add new navigation features:
    - Add the scene you want to travel to, to the build path
        - File -> Build Settings -> Scenes In Build -> Add Open Scenes
    - Add a button to the relevant menu page
    - Add an OnClick() event to the button
    - Choose the MainMenu script as the object to call the function from
    - Choose the navigateScene(string sceneName) method and change the string to the name of the scene you want to go to
    - Go back to the project & hook up the button to this method & drag across whatever controller/manager we may be using
    */

    public void navigateScene(string sceneName){
        //Initially check that the sceneName is not null or empty
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("navigateScene called with empty sceneName.");
            return;
        }

        // Check that the scene is included in Build Settings before trying to load it
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' not found in Build Settings.");
            return;
        }

        //If all checks are passed, load the scene asynchronously
        SceneManager.LoadSceneAsync(sceneName);
    }
    
}
