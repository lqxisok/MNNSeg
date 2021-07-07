using UnityEngine;
using UnityEngine.Rendering;

public static class DownSampling
{
    public static void Setup(CommandBuffer _cb,
                            Shader shader,
                            Texture _videoTex, Texture _segTex,
                            int _downSamplingResultTexID, float _distanceThreshold, float _confidenceThreshold)
    {
        Material material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        
        material.SetFloat(Shader.PropertyToID("_Offset"), 1.0f);
        material.SetTexture(Shader.PropertyToID("_RGBTex"), _videoTex);
        material.SetFloat(Shader.PropertyToID("_DistanceThreshold"), _distanceThreshold);
        material.SetFloat(Shader.PropertyToID("_ConfidenceThreshold"), _confidenceThreshold);

        int tw = _segTex.width;
        int th = _segTex.height;

        int iteration = Mathf.CeilToInt(Mathf.Log(Mathf.Max(tw, th), 2));

        // Blend
        int blendTexID = Shader.PropertyToID("_BlendTex");
        _cb.GetTemporaryRT(blendTexID, tw, th, 0, FilterMode.Bilinear);
        _cb.Blit(_segTex, blendTexID, material, 0);

        // Downsample to find average sky color
        int[] m_pyramid = new int[iteration];
        for (int i = 0; i < iteration; i++)
        {
            m_pyramid[i] = Shader.PropertyToID("_AverageDown" + i);
        }

        int lastDown = blendTexID;
        for (int i = 0; i < iteration; i++)
        {
            _cb.GetTemporaryRT(m_pyramid[i], tw, th, 0, FilterMode.Bilinear);
            _cb.Blit(lastDown, m_pyramid[i], material, 1);
            lastDown = m_pyramid[i];

            tw = Mathf.Max(tw / 2, 1);
            th = Mathf.Max(th / 2, 1);
        }

        // Refine sky segment result
        _cb.SetGlobalTexture("_AverageTex", lastDown);
        _cb.Blit(_segTex, _downSamplingResultTexID, material, 2);

        // Cleanup
        for (int i = 0; i < iteration; i++)
        {
            _cb.ReleaseTemporaryRT(m_pyramid[i]);
        }
        _cb.ReleaseTemporaryRT(blendTexID);
    }

}
