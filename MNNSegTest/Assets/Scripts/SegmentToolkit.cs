using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public static class SegmentToolkit
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

    private static int width, height;
    private static bool initialized = false;

    private static ComputeShader flipShader;
    private static ComputeShader flipAndSplitShader;

    private static byte[] retArray;
    private static Texture2D retTex;
    private static Texture2D flipTex;

    public static bool Init(int _width, int _height, ComputeShader _flipShader, ComputeShader _flipAndSplitShader)
    {
        width = _width;
        height = _height;
        flipShader = _flipShader;
        flipAndSplitShader = _flipAndSplitShader;

        // alloc texture memory
        retArray = new byte[width * height];
        retTex = new Texture2D(width, height, TextureFormat.R8, false);
        flipTex = new Texture2D(width, height, TextureFormat.RGB24, false);

        string path = Application.streamingAssetsPath + "/pcnet_softmax.mnn";
        initialized = initializeModel(path, 4, width, height, 3) == 0;

        return initialized;
    }

    public static void Segment(Texture inputTex, RenderTexture outputTex, bool flip = true)
    {
        FlipImage(inputTex, outputTex, flipShader, flip);
        
        RenderTexture.active = outputTex;
        flipTex.ReadPixels(new Rect(0, 0, outputTex.width, outputTex.height), 0, 0);
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

    public static void ReleaseSession()
    {
        if (initialized)
            releaseSession();
    }

}
