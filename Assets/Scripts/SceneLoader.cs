using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneLoader : MonoInstaller
{
    [SerializeField] private LoadingUI loadingUI;
    [SerializeField] private float loadingTime = 2;
  
    public override void InstallBindings()
    {
        Container.Bind<SceneLoader>().FromInstance(this).AsSingle();
    }


    public void Load(SceneReference sceneToLoad, LoadSceneMode mode = LoadSceneMode.Single)
    {
        loadingUI.Load(loadingTime);
        SceneManager.LoadSceneAsync(sceneToLoad, mode);
    }
}