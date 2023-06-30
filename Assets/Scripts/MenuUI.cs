using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Zenject;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private SceneReference scene;
    private SceneLoader _sceneLoader;

    [Inject]
    private void Constructor(SceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }
    private void OnEnable() 
    {
       var uiDocument = GetComponent<UIDocument>();
       uiDocument.rootVisualElement.Q<Button>("Gallery").RegisterCallback<ClickEvent>(Gallery_OnClick); 
    }

    private void Gallery_OnClick(ClickEvent e)
    {
        _sceneLoader.Load(scene);
    }
}
