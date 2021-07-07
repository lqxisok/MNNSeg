using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

public class CommandBufferTestControl : MonoBehaviour {

    public VideoPlayer CameraVideo;
    public Material ARBackgroundMat;

    [Range(1, 8)]
    public int VideoDownScaling = 1;
    
    [Space(20)]
    public bool flip = true;

    public ComputeShader FlipShader;

    public ComputeShader FlipAndSplitShader;


    [Space(20)]
    public bool ActiveRefine = true;

    public Shader RefineShader;
    
    [Range(0.0f, 2.0f)]
    public float DistanceThreshold = 0.5f;

    [Range(0.5f, 1.0f)]
    public float ConfidenceThreshold = 0.98f;

    [Space(20)]
    public bool ActiveBlur = true;

    public Shader BlurShader;

    [Range(0.0f, 15.0f)]
    public float BlurRadius = 5.0f;

    [Range(1.0f, 10.0f)]
    public int Iteration = 3;

    [Range(1, 10)]
    public float BlurDownScaling = 1.0f;

    [Space(20)]
    public bool ActiveGuideFilter = true;

    public Shader MeanFilterShader;
    public Shader TexDotShader;
    public Shader GuideFilterShader;

    [Range(1, 40)]
    public int GuideFilterRadius = 20;
    
    [Range(0.01f, 1f)]
    public float GuideFilterRegularization = 0.01f;

    private int m_width, m_height;
    private bool m_initialized = false;
    
    private CommandBuffer m_commandBuffer;

    public RenderTexture m_segmentResultTex;

    private RenderTexture m_cameraRT;
    private RenderTexture m_cameraResizeRT;

    void Start()
    {
        m_cameraRT = new RenderTexture((int)CameraVideo.clip.width, (int)CameraVideo.clip.height, 0);
        CameraVideo.targetTexture = m_cameraRT;

        if (!m_initialized)
        {
            m_width = m_cameraRT.width / VideoDownScaling;
            m_height = m_cameraRT.height / VideoDownScaling;

            m_cameraResizeRT = new RenderTexture(m_width, m_height, 0);
            m_cameraResizeRT.enableRandomWrite = true;
            m_cameraResizeRT.Create();

            m_segmentResultTex = new RenderTexture(m_width, m_height, 0);
            m_segmentResultTex.enableRandomWrite = true;
            m_segmentResultTex.Create();

            SegmentToolkit.Init(m_width, m_height, FlipShader, FlipAndSplitShader);

            // Command buffer setup
            m_commandBuffer = new CommandBuffer();

            if (ActiveGuideFilter)
            {
                int guideFilterResultTexID = Shader.PropertyToID("_GuideFilterResultTex");
                m_commandBuffer.GetTemporaryRT(guideFilterResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                GuideFilter.Setup(m_commandBuffer,
                                MeanFilterShader, TexDotShader, GuideFilterShader,
                                m_width, m_height,
                                GuideFilterRadius, GuideFilterRegularization,
                                m_segmentResultTex, m_cameraRT, guideFilterResultTexID);
                
                m_commandBuffer.SetGlobalTexture("_SegTex", guideFilterResultTexID);
                m_commandBuffer.ReleaseTemporaryRT(guideFilterResultTexID);
            }
            else if (!ActiveRefine && !ActiveBlur)
            {
                m_commandBuffer.SetGlobalTexture("_SegTex", m_segmentResultTex);
            }
            else if (!ActiveBlur)
            {
                int downSamplingResultTexID = Shader.PropertyToID("_DownSamplingResultTex");
                m_commandBuffer.GetTemporaryRT(downSamplingResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                DownSampling.Setup(m_commandBuffer, RefineShader, m_cameraRT, m_segmentResultTex, downSamplingResultTexID, DistanceThreshold, ConfidenceThreshold);

                m_commandBuffer.SetGlobalTexture("_SegTex", downSamplingResultTexID);
                
                m_commandBuffer.ReleaseTemporaryRT(downSamplingResultTexID);
            }
            else if (!ActiveRefine)
            {
                int blurResultTexID = Shader.PropertyToID("_BlurResultTexID");
                m_commandBuffer.GetTemporaryRT(blurResultTexID, m_width, m_height, 0, FilterMode.Bilinear);
                int segmentResultTexID = Shader.PropertyToID("_SegmentResultTexID");
                m_commandBuffer.GetTemporaryRT(segmentResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                m_commandBuffer.Blit(m_segmentResultTex, segmentResultTexID);
                DualKawaseBlur.Setup(m_commandBuffer, BlurShader, m_width, m_height, segmentResultTexID, blurResultTexID, BlurRadius, Iteration, BlurDownScaling);
                m_commandBuffer.SetGlobalTexture("_SegTex", blurResultTexID);
                
                m_commandBuffer.ReleaseTemporaryRT(blurResultTexID);
                m_commandBuffer.ReleaseTemporaryRT(segmentResultTexID);
            }
            else
            {
                int downSamplingResultTexID = Shader.PropertyToID("_DownSamplingResultTex");
                m_commandBuffer.GetTemporaryRT(downSamplingResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                DownSampling.Setup(m_commandBuffer, RefineShader, m_cameraRT, m_segmentResultTex, downSamplingResultTexID, DistanceThreshold, ConfidenceThreshold);

                int blurResultTexID = Shader.PropertyToID("_BlurResultTexID");
                m_commandBuffer.GetTemporaryRT(blurResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                DualKawaseBlur.Setup(m_commandBuffer, BlurShader, m_width, m_height, downSamplingResultTexID, blurResultTexID, BlurRadius, Iteration, BlurDownScaling);
                m_commandBuffer.SetGlobalTexture("_SegTex", blurResultTexID);
                
                m_commandBuffer.ReleaseTemporaryRT(downSamplingResultTexID);
                m_commandBuffer.ReleaseTemporaryRT(blurResultTexID);
            }

            CommandBuffer m_commandBuffer1 = new CommandBuffer();
            m_commandBuffer1.Blit(m_cameraRT, BuiltinRenderTextureType.CameraTarget, ARBackgroundMat);

            Camera cam = GetComponent<Camera>();
            cam.AddCommandBuffer(CameraEvent.AfterSkybox, m_commandBuffer);
            cam.AddCommandBuffer(CameraEvent.AfterEverything, m_commandBuffer1);

            m_initialized = true;
        }
    }

    void Update()
    {
        Graphics.Blit(m_cameraRT, m_cameraResizeRT);
        SegmentToolkit.Segment(m_cameraResizeRT, m_segmentResultTex, flip);
    }

    private void OnApplicationQuit()
    {
        if (m_initialized)
            SegmentToolkit.ReleaseSession();
    }

}