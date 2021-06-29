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

    private RenderTexture skyColorRT;
    private bool initialized = false;

    public void Init(int _width, int _height, Texture _inputRGB)
    {
        Shader shader = Shader.Find("Hidden/DownSampling");

        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetFloat(Shader.PropertyToID("_Offset"), SampleRadius);
        material.SetTexture(Shader.PropertyToID("_RGBTex"), _inputRGB);

        int tw = _width;
        int th = _height;

        iteration = Mathf.CeilToInt(Mathf.Log(Mathf.Max(tw, th), 2));
        pyramid = new RenderTexture[iteration];
        for (int i = 0; i < iteration; i++)
            {
                pyramid[i] = new RenderTexture(tw, th, 0);

                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }
        material.SetTexture(Shader.PropertyToID("_AverageTex"), pyramid[iteration - 1]);
        
        initialized = true;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!initialized)
            return;
        // Blend
        Graphics.Blit(source, skyColorRT, material, 0);
        // Graphics.Blit(skyColorRT, outputRT);

        // Downsample to find average sky color
        RenderTexture lastDown = skyColorRT;
        for (int i = 0; i < iteration; i++)
        {
            Graphics.Blit(lastDown, pyramid[i], material, 1);
            lastDown = pyramid[i];
        }

        // refine sky segment result
        Graphics.Blit(source, destination, material, 2);
    }

}
