using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneCreatorUI : MonoBehaviour
{
    //Variables for scene creation UI elements
    [SerializeField] private TMP_InputField sceneNameInput;
    [SerializeField] private TMP_InputField sceneTypeInput;
    [SerializeField] private Button createSceneButton;

    void Start()
    {
        if (createSceneButton != null)
            createSceneButton.onClick.AddListener(OnCreateSceneButtonClicked);
    }

    private void OnCreateSceneButtonClicked()
    {
        string sceneName = sceneNameInput.text;
        string sceneType = sceneTypeInput.text;
        
        
    }
}