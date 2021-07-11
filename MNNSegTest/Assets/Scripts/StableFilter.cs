using UnityEngine;
using UnityEngine.Rendering;

public static class StableFilter
{
    // private static CustomRenderTexture m_ExactorTex;
    private static RenderTexture m_ExactorTex;

    private static RenderTexture m_lastTex;

    public static void Setup(CommandBuffer _cb,
                            Shader _shader,
                            Texture _sourceTex,
                            Texture _destTex)
    {
        Material material = new Material(_shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetTexture("_SourceTex", _sourceTex);

        // m_ExactorTex = new CustomRenderTexture(_segTex.width, _segTex.height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        m_ExactorTex = new RenderTexture(_sourceTex.width, _sourceTex.height, 0);
        m_ExactorTex.filterMode = FilterMode.Point;
        m_lastTex = new RenderTexture(_sourceTex.width, _sourceTex.height, 0);
        m_lastTex.filterMode = FilterMode.Point;
        // m_ExactorTex.enableRandomWrite = true;
        // m_lastTex.Create();
        // m_ExactorTex.Create();
        
        _cb.Blit(m_lastTex, m_ExactorTex, material, 0);
        _cb.Blit(m_ExactorTex, _destTex, material, 1);

        _cb.Blit(m_ExactorTex, m_lastTex);
    }
}
