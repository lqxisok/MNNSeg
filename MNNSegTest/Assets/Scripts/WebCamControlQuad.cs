using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class WebCamControlQuad : MonoBehaviour {

    // TODO: obtain width and height from web camera automatically
    private int width = 1920;
    private int height = 1440;

	private bool systemAvailable = false;
	private WebCamTexture cameraTexture;
    private Material backgroundMaterial;
    private RenderTexture destnationTex;

    [Range(1, 4)]
    public int downScale = 2;
	public GameObject background;
	public bool frontFacing;
    public ComputeShader FlipShader;
    public ComputeShader FlipAndSplitShader;
    public bool flip = true;

    public Camera postCam;
	
	// Use this for initialization
	IEnumerator Start () {

        backgroundMaterial = background.GetComponent<Renderer>().material;

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

                width /= downScale;
                height /= downScale;
                
                destnationTex = new RenderTexture(width, height, 0);
                destnationTex.enableRandomWrite = true;
                destnationTex.Create();

                backgroundMaterial.SetTexture("_SegTex", destnationTex);
                postCam.targetTexture = destnationTex;

                systemAvailable = postCam.GetComponent<SegmentToolkit>().Init(width, height, backgroundMaterial.mainTexture, FlipShader, FlipAndSplitShader);
                postCam.GetComponent<DownSampling>().Init(width, height, backgroundMaterial.mainTexture);
                postCam.GetComponent<DualKawaseBlur>().Init(width, height);

            }

        }

	}
	
	// Update is called once per frame
	void Update () {
		if (!systemAvailable)
			return;

        if (cameraTexture.width < 100)
        {
            Debug.Log("Still waiting frames for correct info ...");
            return;
        }
        
        Rotation();
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

        background.transform.localRotation = Quaternion.AngleAxis(cameraTexture.videoRotationAngle, -Vector3.forward);

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
    }
}