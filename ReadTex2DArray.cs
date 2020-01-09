using UnityEngine;
using UnityEngine.Rendering;
using System;

public class ReadTex2DArray : MonoBehaviour
{
    [SerializeField] ComputeShader compute;
    Texture2DArray tex2DArray;
    int width = 128;
    int height = 128;
    int layerCount = 10;
    int kernel;
    RenderTexture rtArr;
    Texture2DArray tex2DArrRead;
    Texture2D layer;
    [SerializeField] int layerID = 0;


    void Start()
    {
        // Find kernel.
        kernel = compute.FindKernel("ReadTex2DArray");
        // Find define reference to the texture.
        int propResultTex2DArray = Shader.PropertyToID("ResultTexture");
        // Create a 3-dimensional RenderTexture. Note - don't confuse depth with .volumeDepth parameter! It's depth buffer format here!
        rtArr = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        // Allow compute program to write into this texture.
        rtArr.enableRandomWrite = true;
        // Init the rendertexture as a Tex2DArray.
        rtArr.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        // Set the slice count.
        rtArr.volumeDepth = layerCount;
        // Create the texture.
        rtArr.Create();

        // Set texture to the compute program.
        compute.SetTexture(kernel, propResultTex2DArray, rtArr);

        // Calculate dispatch dimensions.
        int xDim = Mathf.Max(8, rtArr.width       /8);
        int yDim = Mathf.Max(8, rtArr.height      /8);
        int zDim = Mathf.Max(8, rtArr.volumeDepth /8);

        // For layer visualization.
        compute.SetInt("_LayerCount", layerCount);

        // Dispatch compute program.
        compute.Dispatch(kernel, xDim, yDim, zDim);

        // Create a request and pass in a method to capture the callback.
        AsyncGPUReadback.Request(rtArr, 0, 0, width, 0, height, 0, layerCount, new Action<AsyncGPUReadbackRequest>
        (
            (AsyncGPUReadbackRequest request) =>
            {
                if (!request.hasError)
                {
                    // Create CPU-side texture array.
                    tex2DArrRead = new Texture2DArray(width, height, request.layerCount, TextureFormat.ARGB32, false);

                    // Copy the data.
                    for (var i = 0; i < request.layerCount; i++)
                    {
                        tex2DArrRead.SetPixels32(request.GetData<Color32>(i).ToArray(), i);
                    }

                    // Save.
                    tex2DArrRead.Apply();
                }
            }
        ));
    }


    // Draw the texture on the screen to see it.
    void OnGUI()
    {
        if (tex2DArrRead != null)
        {
            layer = new Texture2D(width, height, TextureFormat.ARGB32, true);

            // Just copy one layer.
            layerID = Mathf.Clamp(layerID, 0, layerCount-1);
            layer.SetPixels32(tex2DArrRead.GetPixels32(layerID, 0), 0);
            layer.Apply();

            int drawWidth = 1024;
            int drawHeight = 1024;
            int w = drawWidth;
            int h = drawHeight;
            int sw = Screen.width/2;
            int sh = Screen.height/2;

            // Draw the texture on the screen to test array contents.
            Rect rect = new Rect (sw-w/2, sh-h/2, drawWidth, drawHeight);
            GUI.DrawTexture (rect, layer, ScaleMode.ScaleToFit, false);
        }
    }
}
