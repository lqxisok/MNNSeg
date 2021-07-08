using UnityEngine;
using UnityEngine.Rendering;

public static class GuideFilter 
{
    private static bool testMeanA = false;
    private static bool testMeanB = false;

    private static Material meanFilterMat;
    private static Material texDotMat;
    private static Material guideFilterMat;

    private static void Mean(CommandBuffer _cb, Texture source, int dest)
    {
        _cb.Blit(source, dest, meanFilterMat);
    }

    private static void Mean(CommandBuffer _cb, int source, int dest)
    {
        _cb.Blit(source, dest, meanFilterMat);
    }

    private static void Dot(CommandBuffer _cb, Texture source1,Texture source2, int dest)
    {
        _cb.SetGlobalTexture("_SubTex", source2);
        _cb.Blit(source1, dest, texDotMat);
    }

    private static void Dot(CommandBuffer _cb, int source1,int source2, int dest)
    {
        _cb.SetGlobalTexture("_SubTex", source2);
        _cb.Blit(source1, dest, texDotMat);
    }

    public static void Setup(CommandBuffer _cb,
                    Shader _meanFilterShader, 
                    Shader _texDotShader, 
                    Shader _guideFilterShader, 
                    int _width, int _height,
                    int _radius, float _regularization,
                    Texture _sourceTex, Texture _guideTex, int _destTexID)
    {
        // init material
        meanFilterMat = new Material(_meanFilterShader);
        texDotMat = new Material(_texDotShader);
        guideFilterMat = new Material(_guideFilterShader);

        // P ---> Source
        // I ---> guide
        // 设置值
        meanFilterMat.SetInt("_Radius", _radius);
        guideFilterMat.SetFloat("_Regular", _regularization);

        // 分配RT
        // 1 ===
        int meanI = Shader.PropertyToID("_MeanITex");
        int meanP = Shader.PropertyToID("_MeanPTex");
        int dotII = Shader.PropertyToID("_DotIITex");
        int dotIP = Shader.PropertyToID("_DotIPTex");
        int corrI = Shader.PropertyToID("_CorrITex");
        int corrIP = Shader.PropertyToID("_CorrIPTex");
        _cb.GetTemporaryRT(meanI, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(meanP, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(dotII, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(dotIP, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(corrI, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(corrIP, _width, _height, 0, FilterMode.Bilinear);

        // 2 ===
        int varI = Shader.PropertyToID("_VarITex");
        int covIP = Shader.PropertyToID("_CovIPTex");
        _cb.GetTemporaryRT(varI, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(covIP, _width, _height, 0, FilterMode.Bilinear);

        // 3 ===
        int aTex = Shader.PropertyToID("_ATex");
        int bTex = Shader.PropertyToID("_BTex");
        _cb.GetTemporaryRT(aTex, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(bTex, _width, _height, 0, FilterMode.Bilinear);

        // 4 ===
        int meanA = Shader.PropertyToID("_MeanATex");
        int meanB = Shader.PropertyToID("_MeanBTex");
        _cb.GetTemporaryRT(meanA, _width, _height, 0, FilterMode.Bilinear);
        _cb.GetTemporaryRT(meanB, _width, _height, 0, FilterMode.Bilinear);

        // 计算
        // 0 ===
        Mean(_cb, _guideTex, meanI);
        Mean(_cb, _sourceTex, meanP);
        Dot(_cb, _guideTex, _guideTex,dotII);
        Dot(_cb, _guideTex, _sourceTex, dotIP);
        Mean(_cb, dotII, corrI); 
        Mean(_cb, dotIP, corrIP);  

        // 1. ===
        _cb.SetGlobalTexture("_MeanITex",meanI);
        _cb.SetGlobalTexture("_MeanPTex",meanP);
        _cb.SetGlobalTexture("_CorrITex",corrI);
        _cb.SetGlobalTexture("_CorrIPTex",corrIP);
        _cb.Blit(_sourceTex, varI, guideFilterMat, 0);
        
        // 2. ===
        _cb.Blit(_sourceTex, covIP, guideFilterMat, 1);

        // 3. ===
        _cb.SetGlobalTexture("_CovIPTex", covIP);
        _cb.SetGlobalTexture("_VarITex", varI);
        _cb.Blit(_sourceTex, aTex, guideFilterMat, 2);

        // 4. ===
        _cb.SetGlobalTexture("_ATex", aTex);
        _cb.Blit(_sourceTex, bTex, guideFilterMat, 3);
        _cb.SetGlobalTexture("_BTex", bTex);

        Mean(_cb, aTex, meanA);
        Mean(_cb, bTex, meanB);
        _cb.SetGlobalTexture("_MeanATex", meanA);
        _cb.SetGlobalTexture("_MeanBTex", meanB);
      

        // 4. 最终!!===
        _cb.Blit(_guideTex, _destTexID, guideFilterMat, 4);

        // Problem : CovIP
        if (testMeanA)
            _cb.Blit(meanA, _destTexID);
        else if(testMeanB)
            _cb.Blit(meanB,_destTexID);

        //RenderTexture.ReleaseTemporary(guide);
        _cb.ReleaseTemporaryRT(meanI);
        _cb.ReleaseTemporaryRT(meanP);
        _cb.ReleaseTemporaryRT(dotII);
        _cb.ReleaseTemporaryRT(dotIP);

        _cb.ReleaseTemporaryRT(corrI);
        _cb.ReleaseTemporaryRT(corrIP);
        _cb.ReleaseTemporaryRT(varI);
        _cb.ReleaseTemporaryRT(covIP);
        _cb.ReleaseTemporaryRT(aTex);

        _cb.ReleaseTemporaryRT(bTex);
        _cb.ReleaseTemporaryRT(meanA);
        _cb.ReleaseTemporaryRT(meanB);
    }
}