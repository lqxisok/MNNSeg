using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class DownSampling
{
    public static void Setup(int _width, int _height, CommandBuffer cb, Texture videoTex, Texture segTex, int downSamplingResultTexID)
    {
        Shader shader = Shader.Find("Hidden/DownSampling");

        Material material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        
        material.SetFloat(Shader.PropertyToID("_Offset"), 1.0f);
        material.SetTexture(Shader.PropertyToID("_RGBTex"), videoTex);

        int tw = _width;
        int th = _height;

        int iteration = Mathf.CeilToInt(Mathf.Log(Mathf.Max(tw, th), 2));

        // Blend
        int blendTexID = Shader.PropertyToID("_BlendTex");
        cb.GetTemporaryRT(blendTexID, tw, th, 0, FilterMode.Bilinear);
        cb.Blit(segTex, blendTexID, material, 0);

        // Downsample to find average sky color
        int[] m_Pyramid = new int[iteration];
        for (int i = 0; i < iteration; i++)
        {
            m_Pyramid[i] = Shader.PropertyToID("_AverageDown" + i);
        }
        int lastDown = blendTexID;
        for (int i = 0; i < iteration; i++)
        {
            cb.GetTemporaryRT(m_Pyramid[i], tw, th, 0, FilterMode.Bilinear);
            cb.Blit(lastDown, m_Pyramid[i], material, 1);
            lastDown = m_Pyramid[i];

            tw = Mathf.Max(tw / 2, 1);
            th = Mathf.Max(th / 2, 1);
        }

        // Refine sky segment result
        cb.SetGlobalTexture("_AverageTex", lastDown);
        cb.Blit(segTex, downSamplingResultTexID, material, 2);

        // Cleanup
        for (int i = 0; i < iteration; i++)
        {
            cb.ReleaseTemporaryRT(m_Pyramid[i]);
        }
        cb.ReleaseTemporaryRT(blendTexID);
    }

}
