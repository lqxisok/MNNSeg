using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class test : MonoBehaviour
{
    public Texture2D originTex;
    private GameObject origin;
    private GameObject result;

    public ComputeShader shader;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

    [DllImport("segexp")]
    private static extern byte fun(byte[] image, int w, int h, int channel, int x, int y);
    [DllImport("segexp")]
    private static extern int initializeModel(string path, int numThread, int width, int height, int channel);
    [DllImport("segexp")]
    private static extern int processImage(byte[] imageData);
    [DllImport("segexp")]
    private static extern int runSession();
    [DllImport("segexp")]
    private static extern int getOutput(byte[] outputArray);
    [DllImport("segexp")]
    private static extern int releaseSession();
#elif UNITY_IPHONE
    // TODO Android and iOS
    [DllImport ("__Internal")]
    private static extern byte fun(byte[] image, int w, int h, int channel, int x, int y);
    [DllImport ("__Internal")]
    private static extern int initializeModel(string path, int numThread, int width, int height, int channel);
    [DllImport ("__Internal")]
    private static extern int processImage(byte[] imageData);
    [DllImport ("__Internal")]
    private static extern int runSession();
    [DllImport ("__Internal")]
    private static extern int getOutput(byte[] outputArray);
    [DllImport ("__Internal")]
    private static extern int releaseSession();
#endif

    // Start is called before the first frame update
    void Start()
    {
        origin = GameObject.Find("origin");
        result = GameObject.Find("result");
        origin.GetComponent<Renderer>().material.mainTexture = originTex;

        int width = originTex.width;
        int height = originTex.height;

        string path = Application.dataPath + "/Resources/pcnet.mnn";
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
            FlipImage(retTex, flipTex);
            result.GetComponent<Renderer>().material.mainTexture = flipTex;
        #else
            result.GetComponent<Renderer>().material.mainTexture = retTex;
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
