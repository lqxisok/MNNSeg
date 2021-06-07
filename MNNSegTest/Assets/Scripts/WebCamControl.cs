﻿// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;

// public class WebCamControl : MonoBehaviour
// {
//     private WebCamTexture webCam;
//     private GameObject origin;
//     private GameObject result;

//     IEnumerator Start()
//     {
//         yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
//         if (Application.HasUserAuthorization(UserAuthorization.WebCam))
//         {   
//             if (WebCamTexture.devices.Length < 1)
//                 Debug.Log("Error: camera device not found!");
//             else
//             {
//                 WebCamDevice device = WebCamTexture.devices[0];
//                 webCam = new WebCamTexture(device.name);

//                 origin = GameObject.Find("origin");
//                 result = GameObject.Find("result");
//                 origin.GetComponent<RawImage>().texture = webCam;

//                 webCam.Play();
//             }
//         }
//     }

//     void Update()
//     {
//         Rotate();
//     }

//     void Rotate()
//     {
//         origin.transform.rotation = Quaternion.AngleAxis(webCam.videoRotationAngle, -Vector3.forward);

//         var screenAspect = (float)Screen.width / Screen.height;
//         var webCamAspect = (float)webCam.width / webCam.height;

//         var rot90 = (webCam.videoRotationAngle / 90) % 2 != 0;
//         if (rot90) webCamAspect = 1.0f / webCamAspect;

//         float sx, sy;
//         if (webCamAspect < screenAspect)
//         {
//             sx = webCamAspect;
//             sy = 1.0f;
//         }
//         else
//         {
//             sx = screenAspect;
//             sy = screenAspect / webCamAspect;
//         }

//         if (rot90)
//             origin.transform.localScale = new Vector3(sy, sx, 1);
//         else
//             origin.transform.localScale = new Vector3(sx, sy, 1);

//         var mirror = webCam.videoVerticallyMirrored;
//         // material.mainTextureOffset = new Vector2(0, mirror ? 1 : 0);
//         // material.mainTextureScale = new Vector2(1, mirror ? -1 : 1);
//         origin.transform.localScale *= new Vector2(1, mirror ? -1 : 1);
//     }

//     void OnGUI()
//     {
//         var text = "web cam size = " + webCam.width + " x " + webCam.height;
//         text += "\nrotation = " + webCam.videoRotationAngle;
//         text += "\nscreen size = " + Screen.width + " x " + Screen.height;
//         GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text);
//     }
// }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
public class WebCamControl : MonoBehaviour {

	private bool camAvailable;
	private WebCamTexture cameraTexture;
	private Texture defaultBackground;

	public RawImage background;
    public RawImage result;
	// public AspectRatioFitter fit;
	public bool frontFacing;
	
	// Use this for initialization
	IEnumerator Start () {
		defaultBackground = background.texture;

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
                        cameraTexture = new WebCamTexture(curr.name, Screen.width, Screen.height);
                        break;
                    }
                }	
            }

            if (cameraTexture != null)
            {
                cameraTexture.Play (); // Start the camera
                background.texture = cameraTexture; // Set the texture

                camAvailable = true; // Set the camAvailable for future purposes.

                InitSegment();
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

    void Rotation()
    {
       
        // Debug.Log("screen size = " + Screen.width + " x " + Screen.height);
        // Debug.Log("web cam size = " + cameraTexture.width + "x" + cameraTexture.height);
        // Debug.Log("rotation = " + cameraTexture.videoRotationAngle);

        float screenAspect = (float)Screen.width / (float)Screen.height;
        float ratio = (float)cameraTexture.width / (float)cameraTexture.height;

        var rot90 = (cameraTexture.videoRotationAngle / 90) % 2 != 0;
        // if (rot90) ratio = 1.0f / ratio;
        if (rot90)
            background.rectTransform.localScale = new Vector3(ratio / screenAspect, 1.0f, 1);
        else
            background.rectTransform.localScale = new Vector3(1.0f, screenAspect / ratio, 1);

		float scaleY = cameraTexture.videoVerticallyMirrored ? -1f : 1f; // Find if the camera is mirrored or not
        background.rectTransform.localScale = new Vector3(background.rectTransform.localScale.x, background.rectTransform.localScale.y * scaleY, 1f); // Swap the mirrored camera

		int orient = -cameraTexture.videoRotationAngle;
		background.rectTransform.localEulerAngles = new Vector3(0,0, orient);

        // Debug.Log("scale = " + background.rectTransform.localScale);
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

    void InitSegment()
    {
        // TODO use real cam width and height
        int width = 1920;
        int height = 1080;

        string path = Application.streamingAssetsPath + "/pcnet.mnn";
        initializeModel(path, 4, width, height, 3);

        colorArray = new Color32Array();
        colorArray.colors = new Color32[width * height]; 
        retArray = new byte[width * height];
        retTex = new Texture2D(width, height, TextureFormat.R8, false);
    }
    void Segment()
    {
        cameraTexture.GetPixels32(colorArray.colors);
        processImage(colorArray.byteArray);

        runSession();

        getOutput(retArray);
        
        retTex.LoadRawTextureData(retArray);
        retTex.Apply();

        result.texture = retTex;
    }

    void OnApplicationQuit()
    {
        releaseSession();
    }
}