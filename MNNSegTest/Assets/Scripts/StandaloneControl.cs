using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
public class StandaloneControl : MonoBehaviour {

    const int width = 640;
    const int height = 368;

    private Material backgroundMaterial;
    private Material resultMaterial;
    private bool initialized = false;

	public GameObject background;
    public ComputeShader shader;
	
	// Use this for initialization
	void Start () {

        backgroundMaterial = background.GetComponent<Renderer>().material;

        initialized = Init();
	}
	
	// Update is called once per frame
	void Update () {
        Segment();
	}

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


    [StructLayout(LayoutKind.Explicit)]
    private struct Color32Array
    {
        [FieldOffset(0)]
        public byte[] byteArray;

        [FieldOffset(0)]
        public Color32[] colors;
    }

    private Color32Array colorArray;
    private byte[] retArray;
    private Texture2D retTex;
    private Texture2D flipTex;
    private RenderTexture rTex;


    bool Init()
    {
        // alloc texture memory
        colorArray = new Color32Array();
        colorArray.colors = new Color32[width * height]; 
        retArray = new byte[width * height];
        retTex = new Texture2D(width, height, TextureFormat.R8, false);

        #if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
        flipTex = new Texture2D(width, height, TextureFormat.RGB24, false);
        #endif

        rTex = new RenderTexture(width, height, 0);
        rTex.enableRandomWrite = true;
        rTex.Create();
        backgroundMaterial.SetTexture("_SegTex", rTex);

        // init segment
        string path = Application.streamingAssetsPath + "/pcnet.mnn";
        return initializeModel(path, 4, width, height, 3) == 0;
    }
    void Segment()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
            FlipImage(backgroundMaterial.mainTexture, flipTex);
            processImage(flipTex.GetRawTextureData());
        #else
            cameraTexture.GetPixels32(colorArray.colors);
            processImage(colorArray.byteArray);
        #endif

        runSession();
        getOutput(retArray);
    
        retTex.LoadRawTextureData(retArray);
        // retTex.SetPixels32(colorArray.colors);
        retTex.Apply(false);

        #if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
            FlipImage(retTex, rTex);
        #endif
    }


    void FlipImage(Texture2D inputTex, RenderTexture outputTex)
    {
        int numThread = 8;
        int kernelHandle = shader.FindKernel("CSMain");

        shader.SetInt("x", inputTex.width);
        shader.SetInt("y", inputTex.height);
        shader.SetTexture(kernelHandle, "Result", outputTex);
        shader.SetTexture(kernelHandle, "ImageInput", inputTex);
        shader.Dispatch(kernelHandle, inputTex.width / numThread , inputTex.height / numThread, 1);

    }

    void FlipImage(Texture inputTex, Texture2D outputTex)
    {
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

    void OnApplicationQuit()
    {
        releaseSession();
    }
}