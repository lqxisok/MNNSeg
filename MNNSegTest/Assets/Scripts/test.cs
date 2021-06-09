using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
public class test : MonoBehaviour
{
    public Texture2D originTex;
    private GameObject origin;
    private GameObject result;

    public ComputeShader shader;

// for Windows
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    const string platformPrefix = "segexp";
// for MAC
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    const string platformPrefix = "MNN";
// for Android and iOS
// #elif UNIYT_IOS || UNITY_ANDROID
#else
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

    // Start is called before the first frame update
    void Start()
    {
        // Debug.Log(fun(1, 2));
        
        origin = GameObject.Find("origin");
        result = GameObject.Find("result");
        origin.GetComponent<RawImage>().texture = originTex;

        int width = originTex.width;
        int height = originTex.height;

        string path = Application.streamingAssetsPath + "/pcnet.mnn";
        initializeModel(path, 4, width, height, 3);

        #if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
            Texture2D flipTex = new Texture2D(width, height, TextureFormat.RGB24, false);
            FlipImage(originTex, flipTex);
            processImage(flipTex.GetRawTextureData());
        #else
            processImage(originTex.GetRawTextureData());
        #endif
        
        runSession();

        byte[] retArray = new byte[width * height];
        getOutput(retArray);

        Texture2D retTex = new Texture2D(width, height, TextureFormat.R8, false);
        retTex.LoadRawTextureData(retArray);
        retTex.Apply();
        
        #if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
            Texture2D flipTex1 = new Texture2D(width, height, TextureFormat.R8, false);
            FlipImage(retTex, flipTex1);
            result.GetComponent<RawImage>().texture = flipTex1;
        #else
            result.GetComponent<RawImage>().texture = retTex;
        #endif

        releaseSession();
    }

    void FlipImage(Texture2D inputTex, Texture2D outputTex)
    {
        RenderTexture rTex = new RenderTexture(inputTex.width, inputTex.height, 0);
        rTex.enableRandomWrite = true;
        rTex.Create();

        int numThread = 8;
        int kernelHandle = shader.FindKernel("CSMain");

        shader.SetInt("x", inputTex.width);
        shader.SetInt("y", inputTex.height);
        shader.SetTexture(kernelHandle, "Result", rTex);
        shader.SetTexture(kernelHandle, "ImageInput", inputTex);
        shader.Dispatch(kernelHandle, inputTex.width / numThread , inputTex.height / numThread, 1);

        RenderTexture.active = rTex;
        outputTex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        outputTex.Apply();
    }
}
