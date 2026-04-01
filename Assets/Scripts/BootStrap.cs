using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootStrap : MonoBehaviour
{
    // Start is called before the first frame update    
    void Start()
    {
        Application.targetFrameRate = 60;
       SceneManager.LoadScene("MainMenu");
    }
   
}
