using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GallantGames.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Zenject;
using Task = System.Threading.Tasks.Task;

public class GallerryUI : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset itemsListTemplate;
    [SerializeField] private VisualTreeAsset previewTemplate;
    [SerializeField] private string baseUrl = "http://data.ikppbb.com/test-task-unity-data/pics";
    [SerializeField] private int maxImageCount = 66;
    [SerializeField] private SceneReference scene;
    private List<GalleryItem> _items = new();
    private GridView _itemGridView;
    private bool _isLoading;
    private TemplateContainer _preview;
    private SceneLoader _sceneLoader;
    private bool _itemGridStarted;
    private TemplateContainer _radialbar;

    [Inject]
    private void Construct(SceneLoader loader)
    {
        _sceneLoader = loader;
    }

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        _itemGridView = uiDocument.rootVisualElement.Q<GridView>();
        SetUpGridView();
        SetUpPreview();
        uiDocument.rootVisualElement.Add(_preview);
       
    }

    private void SetUpGridView()
    {

        _itemGridView.makeItem = () => { return itemsListTemplate.Instantiate().Q<VisualElement>("Element"); };
        _itemGridView.bindItem = (itemElement, itemSource) =>
        {
            itemElement.style.backgroundImage = (itemSource as GalleryItem).Image;
        }; 
        UpdateList();
        _itemGridView.onSelect += ItemsGridView_OnSelect;
        _itemGridView.reachEnd += LoadImages;
        
        _itemGridView.RegisterCallback<GeometryChangedEvent>(ItemGridStart);
        _itemGridView.waitToLoad = true;
    }

    private void SetUpPreview()
    {
        _preview = new TemplateContainer()
        {
            style =
            {
                height = Length.Percent(100), width = Length.Percent(100),
                position = Position.Absolute, display = DisplayStyle.None
            },
        };
        _preview.Add( previewTemplate.Instantiate().Q<VisualElement>("Panel"));

        _preview.Q<Button>().clicked += ClosePreview;
    }

    private void ItemGridStart(GeometryChangedEvent evt)
    {
        if (_itemGridStarted) return;
        _itemGridStarted = true;
        LoadImages();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            if (_preview.resolvedStyle.display == DisplayStyle.Flex)
            {
                ClosePreview();
            }
            else
            {
                _sceneLoader.Load(scene);
            }
        }
    }

    private void ClosePreview()
    {
        _preview.style.display = DisplayStyle.None;
        Screen.orientation = ScreenOrientation.Portrait;
    }
    
    private void ItemsGridView_OnSelect(VisualElement ev, object element)
    {
        var item = element as GalleryItem;
        _preview.Q<AspectRatioPanel>().style.backgroundImage = item.Image;
        _preview.style.display = DisplayStyle.Flex;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    private void LoadImages()
    {
        int start = _items.Count + 1;
        int end = Math.Clamp(_items.Count + _itemGridView.itemCountOnScreen, 0, maxImageCount);
        if (end == maxImageCount) _itemGridView.waitToLoad = false;

        for (int i = start; i <=end; i++)
        {
            LoadImage($"{baseUrl}/{i}.jpg");
        }
    }

    private async Task LoadImage(string url)
    {
        var task =  ExtensionTexture.GetUrlImage(url);
        _items.Add(new GalleryItem(task));
        
        await task;

        if(!task.IsCompletedSuccessfully)
        {
            Debug.LogError("Task failed or canceled!");
            return;
        }
        task.Result.name = url;
        UpdateList();
    }

    private void UpdateList()
    {
        _itemGridView.itemsSource = _items;
    }
}
