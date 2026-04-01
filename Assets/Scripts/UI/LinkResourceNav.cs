using UnityEngine;

//Bascially this script is meant to use the behavior of navigating to external links/web pages outside the game itself
//Mainly to be used by the DM
public class LinkResourceNav : MonoBehaviour
{
    //Method to link to a specified URL
    public void OpenLink(string specificURL)
    {
        Application.OpenURL("https://5e.tools/" + specificURL);
    }
}
