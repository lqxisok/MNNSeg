using UnityEngine;
using UnityEngine.Rendering;

public static class DualKawaseBlur
{
    private struct Level
    {
        internal int down;
        internal int up;
    }

    public static void Setup(CommandBuffer _cb,
                            Shader shader,
                            Texture _sourceTex, Texture _destTex,
                            float _blurRadius, int _iteration, float _downScaling)
    {
        Material material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetFloat(Shader.PropertyToID("_Offset"), _blurRadius);

        int tw = (int)(_sourceTex.width / _downScaling);
        int th = (int)(_sourceTex.height / _downScaling);

        Level[] m_pyramid = new Level[_iteration];
        for (int i = 0; i < _iteration; i++)
        {
            m_pyramid[i] = new Level
            {
                down = Shader.PropertyToID("_BlurDown" + i),
                up = Shader.PropertyToID("_BlurUp" + i)
            };
        }

        // Downsample
        int lastDown = 0;
        for (int i = 0; i < _iteration; i++)
        {
            int mipDown = m_pyramid[i].down;
            int mipUp = m_pyramid[i].up;
            _cb.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear);
            _cb.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear);

            if (i == 0)
                _cb.Blit(_sourceTex, mipDown, material, 0);
            else
                _cb.Blit(lastDown, mipDown, material, 0);

            lastDown = mipDown;
            tw = Mathf.Max(tw / 2, 1);
            th = Mathf.Max(th / 2, 1);
        }

        // Upsample
        int lastUp = lastDown;
        for (int i = _iteration - 2; i >= 0; i--)
        {
            int mipUp = m_pyramid[i].up;

            _cb.Blit(lastUp, mipUp, material, 1);
            lastUp = mipUp;
        }

        // Cleanup
        // Render blurred texture in blend pass
        _cb.Blit(lastUp, _destTex, material, 1);

        for (int i = 0; i < _iteration; i++)
        {
            _cb.ReleaseTemporaryRT(m_pyramid[i].down);
            _cb.ReleaseTemporaryRT(m_pyramid[i].up);
        }
    }
}
