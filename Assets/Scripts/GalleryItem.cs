using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GalleryItem
{
    public GalleryItem(Texture2D image)
    {
        Image = image;
    }
    
    public GalleryItem(Task<Texture2D> imageTask)
    {
        LoadImage(imageTask);
    }
    
    public Texture2D Image { get; private set; }

    private async void LoadImage(Task<Texture2D> imageTask)
    {
        await imageTask;
        Image = imageTask.Result;
    }
}
