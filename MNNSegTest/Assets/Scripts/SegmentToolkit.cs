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

    private static ComputeShader flipShader;
    private static ComputeShader flipAndSplitShader;

    private static byte[] retArray;
    private static Texture2D retTex;
    private static Texture2D flipTex;
    private static Rect rect;

    public static bool Init(int _width, int _height, ComputeShader _flipShader, ComputeShader _flipAndSplitShader)
    {
        flipShader = _flipShader;
        flipAndSplitShader = _flipAndSplitShader;

        // alloc texture memory
        retArray = new byte[_width * _height];
        retTex = new Texture2D(_width, _height, TextureFormat.R8, false);

        flipTex = new Texture2D(_width, _height, TextureFormat.RGB24, false);

        rect = new Rect(0, 0, _width, _height);

        string path = Application.streamingAssetsPath + "/pcnet_softmax.mnn";
        return initializeModel(path, 4, _width, _height, 3) == 0;
    }

    public static void Segment(RenderTexture inputTex, RenderTexture outputTex, bool flip)
    {
        if (flip)
        {
            FlipImage(inputTex, outputTex, flipShader);
            RenderTexture.active = outputTex;
        }
        else
        {
            RenderTexture.active = inputTex;
        }

        flipTex.ReadPixels(rect, 0, 0);
        flipTex.Apply(false);
        RenderTexture.active = null;

        processImage(flipTex.GetRawTextureData());

        runSession();
        getOutput(retArray);

        retTex.LoadRawTextureData(retArray);
        retTex.Apply(false);

        if (flip)
            FlipImage(retTex, outputTex, flipAndSplitShader);
        else
            Graphics.Blit(retTex, outputTex);
    }

    public static void Segment(Texture2D inputTex, RenderTexture outputTex, bool flip)
    {
        processImage(inputTex.GetRawTextureData());
        runSession();
        getOutput(retArray);

        retTex.LoadRawTextureData(retArray);
        retTex.Apply(false);

        FlipImage(retTex, outputTex, flipAndSplitShader);
    }

    private static void FlipImage(Texture inputTex, RenderTexture outputTex, ComputeShader shader)
    {
        int numThread = 8;
        int kernelHandle = shader.FindKernel("CSMain");

        shader.SetInt("x", inputTex.width);
        shader.SetInt("y", inputTex.height);
        shader.SetTexture(kernelHandle, "Result", outputTex);
        shader.SetTexture(kernelHandle, "ImageInput", inputTex);
        shader.Dispatch(kernelHandle, inputTex.width / numThread + 1, inputTex.height / numThread + 1, 1);
    }

    public static void ReleaseSession()
    {
        releaseSession();
    }
}
