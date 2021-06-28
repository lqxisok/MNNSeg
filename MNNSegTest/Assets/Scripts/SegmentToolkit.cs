using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public class SegmentToolkit : MonoBehaviour
{
    #region c_entry

    // for Windows
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string platformPrefix = "segexp";
    // for MAC
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        const string platformPrefix = "MNN";
    // for Android and iOS
    #else // #elif UNIYT_IOS || UNITY_ANDROID
        const string platformPrefix = "__Internal";
    #endif

    [DllImport (platformPrefix)]
    private static extern int fun(int a, int b);
    [DllImport (platformPrefix)]
    private static extern int initializeModel(string path, int numThread, int width, int height, int channel);
    [DllImport (platformPrefix)]
    private static extern int processImage(byte[] imageData);
    [DllImport (platformPrefix)]
    private static extern int runSession();
    [DllImport (platformPrefix)]
    private static extern int getOutput(byte[] outputArray);
    [DllImport (platformPrefix)]
    private static extern int releaseSession();

    #endregion

    private int width, height;
    private bool initialized = false;

    private ComputeShader flipShader;
    private ComputeShader flipAndSplitShader;

    private byte[] retArray;
    private Texture2D retTex;
    private Texture2D flipTex;
    private Rect rect;

    private RenderTexture tmpResizeRT;
    private RenderTexture tmpResultRT;

    private Texture inputRGB;

    public bool Init(int _width, int _height, Texture _inputRGB, ComputeShader _flipShader, ComputeShader _flipAndSplitShader)
    {
        width = _width;
        height = _height;
        inputRGB = _inputRGB;
        flipShader = _flipShader;
        flipAndSplitShader = _flipAndSplitShader;

        // alloc texture memory
        retArray = new byte[width * height];
        retTex = new Texture2D(width, height, TextureFormat.R8, false);
        flipTex = new Texture2D(width, height, TextureFormat.RGB24, false);


        tmpResizeRT = new RenderTexture(width, height, 0);
        tmpResizeRT.enableRandomWrite = true;
        tmpResizeRT.Create();

        tmpResultRT = new RenderTexture(width, height, 0);
        tmpResultRT.enableRandomWrite = true;
        tmpResultRT.Create();

        rect = new Rect(0, 0, width, height);

        string path = Application.streamingAssetsPath + "/pcnet_softmax.mnn";
        initialized = initializeModel(path, 4, width, height, 3) == 0;

        return initialized;
    }

    private void Segment(Texture inputTex, RenderTexture outputTex, bool flip = true)
    {
        FlipImage(inputTex, outputTex, flipShader, flip);
        
        RenderTexture.active = outputTex;
        flipTex.ReadPixels(rect, 0, 0);
        flipTex.Apply(false);
        RenderTexture.active = null;

        processImage(flipTex.GetRawTextureData());

        runSession();
        getOutput(retArray);
    
        retTex.LoadRawTextureData(retArray);
        retTex.Apply(false);

        FlipImage(retTex, outputTex, flipAndSplitShader, flip);
    }


    private static void FlipImage(Texture inputTex, RenderTexture outputTex, ComputeShader shader, bool flip)
    {
        int numThread = 8;
        int kernelHandle = shader.FindKernel("CSMain");

        shader.SetInt("x", inputTex.width);
        shader.SetInt("y", inputTex.height);
        shader.SetTexture(kernelHandle, "Result", outputTex);
        shader.SetTexture(kernelHandle, "ImageInput", inputTex);
        shader.Dispatch(kernelHandle, inputTex.width / numThread , inputTex.height / numThread, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // resize
        Graphics.Blit(inputRGB, tmpResizeRT);
        // segment
        Segment(tmpResizeRT, tmpResultRT, true);
        Graphics.Blit(tmpResultRT, destination);
    }

    void OnApplicationQuit()
    {
        if (initialized)
            releaseSession();
    }

}
