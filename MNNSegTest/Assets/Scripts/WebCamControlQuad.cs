using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
public class WebCamControlQuad : MonoBehaviour {

    const int width = 1920 / 4;
    const int height = 1440 / 4;

	private bool camAvailable;
	private WebCamTexture cameraTexture;
    private Material backgroundMaterial;
    private Material resultMaterial;

	public GameObject background;
    public GameObject result;
	// public AspectRatioFitter fit;
	public bool frontFacing;
    public ComputeShader shader;
	
	// Use this for initialization
	IEnumerator Start () {

        backgroundMaterial = background.GetComponent<Renderer>().material;
        resultMaterial = result.GetComponent<Renderer>().material;

        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length > 0)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    var curr = devices[i];
                    if (curr.isFrontFacing == frontFacing)
                    {
                        cameraTexture = new WebCamTexture(curr.name, width, height);
                        break;
                    }
                }	
            }

            if (cameraTexture != null)
            {
                cameraTexture.Play (); // Start the camera
                backgroundMaterial.mainTexture = cameraTexture; // Set the texture

                camAvailable = Init(); // Set the camAvailable for future purposes.
            }

        }

	}
	
	// Update is called once per frame
	void Update () {
		if (!camAvailable)
			return;

        if (cameraTexture.width < 100)
        {
            Debug.Log("Still waiting frames for correct info ...");
            return;
        }
        
 
        Rotation();
        Segment();
	}

    Vector2 offset = new Vector2(0, 0);
    Vector2 offsetMirror = new Vector2(0, 1);
    Vector2 scale = new Vector2(1, 1);
    Vector2 scaleMirror = new Vector2(1, -1);
    void Rotation()
    {
        // Debug.Log("screen size = " + Screen.width + " x " + Screen.height);
        // Debug.Log("web cam size = " + cameraTexture.width + " x " + cameraTexture.height);
        // Debug.Log("rotation = " + cameraTexture.videoRotationAngle);

        background.transform.rotation = Quaternion.AngleAxis(cameraTexture.videoRotationAngle, -Vector3.forward);

        float screenAspect = (float)Screen.width / (float)Screen.height;
        float webCamAspect = (float)cameraTexture.width / (float)cameraTexture.height;

        var rot90 = (cameraTexture.videoRotationAngle / 90) % 2 != 0;
        if (rot90) webCamAspect = 1.0f / webCamAspect;

        float sx, sy;
        if (webCamAspect < screenAspect)
        {
            sx = webCamAspect;
            sy = 1.0f;
        }
        else
        {
            sx = screenAspect;
            sy = screenAspect / webCamAspect;
        }

        if (rot90)
            background.transform.localScale = new Vector3(sy, sx, 1);
        else
            background.transform.localScale = new Vector3(sx, sy, 1);

		bool mirror = cameraTexture.videoVerticallyMirrored;
        
        backgroundMaterial.mainTextureOffset = mirror ? offsetMirror : offset;
        backgroundMaterial.mainTextureScale = mirror ? scaleMirror : scale;

        resultMaterial.mainTextureOffset = mirror ? offsetMirror : offset;
        resultMaterial.mainTextureScale = mirror ? scaleMirror : scale;
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

        // init segment
        string path = Application.streamingAssetsPath + "/pcnet.mnn";
        return initializeModel(path, 4, width, height, 3) == 0;
    }
    void Segment()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
            FlipImage(cameraTexture, flipTex);
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
            resultMaterial.mainTexture = rTex;
        #else
            resultMaterial.mainTexture = retTex;
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

    void FlipImage(WebCamTexture inputTex, Texture2D outputTex)
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
        if (camAvailable)
            releaseSession();
    }
}