using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DualKawaseBlur : MonoBehaviour
{
    private struct Level
    {
        internal RenderTexture down;
        internal RenderTexture up;
    }

    [Range(0.0f, 15.0f)]
    public float BlurRadius = 2.0f;

    [Range(1.0f, 10.0f)]
    public int Iteration = 3;

    [Range(1, 10)]
    public float RTDownScaling = 1.0f;

    private Level[] pyramid;

    private Material material;

    private RenderTexture inputRT, outputRT;

    public void Init(int _width, int _height)
    {
        Shader shader = Shader.Find("Hidden/DualKawaseBlur");

        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetFloat(Shader.PropertyToID("_Offset"), BlurRadius);

        int tw = (int)(_width / RTDownScaling);
        int th = (int)(_height / RTDownScaling);
        
        pyramid = new Level[Iteration];

        for (int i = 0; i < Iteration; i++)
            {
                pyramid[i] = new Level
                {
                    down = new RenderTexture(tw, th, 0),
                    up = new RenderTexture(tw, th, 0)
                };
                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Downsample
        RenderTexture lastDown = source;
        for (int i = 0; i < Iteration; i++)
        {
            Graphics.Blit(lastDown, pyramid[i].down, material, 0);
            lastDown = pyramid[i].down;
        }

        // Upsample
        RenderTexture lastUp =pyramid[Iteration - 1].down;
        for (int i = Iteration - 2; i >= 0; i--)
        {
            Graphics.Blit(lastUp, pyramid[i].up, material, 1);
            lastUp = pyramid[i].up;
        }

        // put out blur result
        Graphics.Blit(lastUp, destination, material, 1);
    }

}
