// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Video;


// public class StandaloneControl : MonoBehaviour {

//     // TODO: obtain width and height from web camera automatically
//     private int width = 640;
//     private int height = 368;

//     private Material backgroundMaterial;
//     private RenderTexture destnationTex;
//     private RenderTexture videoRT;

//     [Range(1, 4)]
//     public int downScale = 2;
// 	public GameObject background;
//     public ComputeShader FlipShader;
//     public ComputeShader FlipAndSplitShader;
//     public bool flip = true;

//     public Camera postCam;
	
// 	// Use this for initialization
// 	void Start () {

//         VideoPlayer videoPlayer = this.GetComponent<UnityEngine.Video.VideoPlayer>();
//         videoRT = new RenderTexture((int)videoPlayer.clip.width, (int)videoPlayer.clip.height, 0);
//         videoPlayer.targetTexture = videoRT;

//         width = (int)videoPlayer.clip.width / downScale;
//         height = (int)videoPlayer.clip.height / downScale;
        
//         backgroundMaterial = background.GetComponent<Renderer>().material;

//         destnationTex = new RenderTexture(width, height, 0);
//         // destnationTex.enableRandomWrite = true;
//         // destnationTex.Create();

//         backgroundMaterial.mainTexture = videoRT;
//         backgroundMaterial.SetTexture("_SegTex", destnationTex);
//         postCam.targetTexture = destnationTex;

//         postCam.GetComponent<SegmentToolkit>().Init(width, height, backgroundMaterial.mainTexture, FlipShader, FlipAndSplitShader);
//         postCam.GetComponent<DownSampling>().Init(width, height, backgroundMaterial.mainTexture);
//         postCam.GetComponent<DualKawaseBlur>().Init(width, height);

// 	}
	
// 	// Update is called once per frame
// 	// void Update () {

//         // 1. get segmention image
//         // SegmentToolkit.Segment(backgroundMaterial.mainTexture, rtTex, flip);

//         // 2. get average color of sky by using rtex.confidence(.y), and refine segment result
//         // in DownSampling.OnRenderImage

//         // 3. blur
//         // in DualKawaseBlur.OnRenderImage
// 	// }


// }