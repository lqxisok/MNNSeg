using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class StandaloneControl : MonoBehaviour {

    // TODO: obtain width and height from web camera automatically
    const int width = 640;
    const int height = 368;

    private Material backgroundMaterial;
    private Material resultMaterial;
    public RenderTexture rtTex;
    public RenderTexture destnationTex;

	public GameObject background;
    public ComputeShader shader;
    public bool flip = true;
    public DualKawaseBlur dualKawaseBlur;
	
	// Use this for initialization
	void Start () {

        backgroundMaterial = background.GetComponent<Renderer>().material;

        rtTex = new RenderTexture(width, height, 0);
        rtTex.enableRandomWrite = true;
        rtTex.Create();

        destnationTex = new RenderTexture(width, height, 0);
        destnationTex.enableRandomWrite = true;
        destnationTex.Create();
        backgroundMaterial.SetTexture("_SegTex", destnationTex);

        SegmentToolkit.Init(width, height, shader);

        dualKawaseBlur.Init(rtTex, destnationTex);
	}
	
	// Update is called once per frame
	void Update () {
        SegmentToolkit.Segment(backgroundMaterial.mainTexture, rtTex, flip);
	}

    // private void OnPreRender() {
    //     DualKawaseBlur.Render(rtTex, destnationTex);   
    // }

    void OnApplicationQuit()
    {
        SegmentToolkit.ReleaseSession();
    }
}