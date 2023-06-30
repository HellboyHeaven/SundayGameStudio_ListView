using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class LoadingUI : MonoBehaviour
{
    private ProgressBar _progressBar;
    private void OnEnable() 
    {
        var uiDocument = GetComponent<UIDocument>();
        _progressBar = uiDocument.rootVisualElement.Q<ProgressBar>();
    }
    
    public void Load(float time)
    {
        gameObject.SetActive(true);
        StartCoroutine(LoadCoroutine(time));
    }

    private IEnumerator LoadCoroutine(float duration)
    {
       
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            _progressBar.value = 100 * time / duration;
            yield return null;
        }
        gameObject.SetActive(false);
    }

  
     
     
}