using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class IconGenerator : MonoBehaviour
{

    private Camera cam;

    public string Prefix;

    public string PathFolder;

    //public string filePath;
    private void Awake()
    {

    }

    private void Start()
    {
    }

    public void TakeScreenshot(string fullPath) 
    {
        if (cam == null) 
        {
            cam = GetComponent<Camera>();    
        
        }

        RenderTexture rt = new RenderTexture(256, 256, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null;

        if (Application.isEditor)
        {
            DestroyImmediate(rt);
        }
        else 
        {
            Destroy(rt);
        }

        byte[] bytes = screenShot.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bytes);
        
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }
}
