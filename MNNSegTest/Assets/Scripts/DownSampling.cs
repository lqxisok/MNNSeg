using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownSampling : MonoBehaviour
{
    // private struct Level
    // {
    //     internal RenderTexture down;
    //     internal  up;
    // }

    [Range(0.0f, 15.0f)]
    public float SampleRadius = 0.0f;

    private RenderTexture[] pyramid;

    private int iteration;

    private Material material;

    private Texture inputRGB;
    private RenderTexture inputSegRT, outputRT;

    private RenderTexture skyColorRT;

    public void Init(Texture _inputRGB, RenderTexture _inputSeg, RenderTexture _output)
    {
        inputRGB = _inputRGB;
        inputSegRT = _inputSeg;
        outputRT = _output;

        Shader shader = Shader.Find("Hidden/DownSampling");

        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetFloat(Shader.PropertyToID("_Offset"), SampleRadius);
        material.SetTexture(Shader.PropertyToID("_RGBTex"), inputRGB);

        this.GetComponent<Camera>().targetTexture = outputRT;

        int tw = inputSegRT.width;
        int th = inputSegRT.height;

        skyColorRT = new RenderTexture(tw, th, 0);

        iteration = Mathf.CeilToInt(Mathf.Log(Mathf.Max(tw, th), 2));
        pyramid = new RenderTexture[iteration];
        for (int i = 0; i < iteration; i++)
            {
                pyramid[i] = new RenderTexture(tw, th, 0);

                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }
        material.SetTexture(Shader.PropertyToID("_AverageTex"), pyramid[iteration - 1]);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Blend
        Graphics.Blit(inputSegRT, skyColorRT, material, 0);
        // Graphics.Blit(skyColorRT, outputRT);

        // Downsample
        RenderTexture lastDown = skyColorRT;
        for (int i = 0; i < iteration; i++)
        {
            Graphics.Blit(lastDown, pyramid[i], material, 1);
            lastDown = pyramid[i];
        }

        Graphics.Blit(inputSegRT, outputRT, material, 2);
    }

}
