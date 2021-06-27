using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;


public class StandaloneControl : MonoBehaviour {

    // TODO: obtain width and height from web camera automatically
    int width = 640;
    int height = 368;

    private Material backgroundMaterial;
    private Material resultMaterial;
    private RenderTexture rtTex;
    private RenderTexture destnationTex;

	public GameObject background;
    public ComputeShader FlipShader;
    public ComputeShader FlipAndSplitShader;
    public bool flip = true;

    public DualKawaseBlur dualKawaseBlur;

    public DownSampling downsampling;
	
	// Use this for initialization
	void Start () {

        VideoPlayer videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
        width = (int)videoPlayer.clip.width;
        height = (int)videoPlayer.clip.height;
        
        backgroundMaterial = background.GetComponent<Renderer>().material;

        rtTex = new RenderTexture(width, height, 0);
        rtTex.enableRandomWrite = true;
        rtTex.Create();

        destnationTex = new RenderTexture(width, height, 0);
        destnationTex.enableRandomWrite = true;
        destnationTex.Create();

        backgroundMaterial.SetTexture("_SegTex", rtTex);

        SegmentToolkit.Init(width, height, FlipShader, FlipAndSplitShader);

        downsampling.Init(backgroundMaterial.mainTexture, rtTex, destnationTex);

        dualKawaseBlur.Init(destnationTex, rtTex);

	}
	
	// Update is called once per frame
	void Update () {

        // 1. get segmention image
        SegmentToolkit.Segment(backgroundMaterial.mainTexture, rtTex, flip);

        // 2. get average color of sky by using rtex.confidence(.y)
        // in DownSampling.OnRenderImage

        // 3. blur is in DualKawaseBlur.OnRenderImage call back -- destnationTex
	}

    void OnApplicationQuit()
    {
        SegmentToolkit.ReleaseSession();
    }
}