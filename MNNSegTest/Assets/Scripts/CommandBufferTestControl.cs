using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

public class CommandBufferTestControl : MonoBehaviour {

    public VideoPlayer CameraVideo;

    public ComputeShader FlipShader;

    public ComputeShader FlipAndSplitShader;

    [Range(1, 4)]
    public int VideoDownScaling = 1;

    public bool ActiveRefine = true;

    public bool ActiveBlur = true;

    [Range(0.0f, 15.0f)]
    public float BlurRadius = 5.0f;

    [Range(1.0f, 10.0f)]
    public int Iteration = 3;

    [Range(1, 10)]
    public float BlurDownScaling = 1.0f;

    public bool flip = true;

    public Material ARBackgroundMat;

    private int m_width, m_height;
    private bool m_initialized = false;
    
    private CommandBuffer m_commandBuffer;

    private RenderTexture m_segmentResultTex;

    private RenderTexture m_cameraRT;

    void Start()
    {
        m_cameraRT = new RenderTexture((int)CameraVideo.clip.width, (int)CameraVideo.clip.height, 0);
        CameraVideo.targetTexture = m_cameraRT;

        if (!m_initialized)
        {
            m_width = m_cameraRT.width / VideoDownScaling;
            m_height = m_cameraRT.height / VideoDownScaling;

            m_segmentResultTex = new RenderTexture(m_width, m_height, 0);
            m_segmentResultTex.enableRandomWrite = true;
            m_segmentResultTex.Create();

            SegmentToolkit.Init(m_width, m_height, FlipShader, FlipAndSplitShader);

            // Command buffer setup
            m_commandBuffer = new CommandBuffer();

            if (!ActiveRefine && !ActiveBlur)
            {
                m_commandBuffer.SetGlobalTexture("_SegTex", m_segmentResultTex);
            }
            else
            {
                int downSamplingResultTexID = Shader.PropertyToID("_DownSamplingResultTex");
                m_commandBuffer.GetTemporaryRT(downSamplingResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                DownSampling.Setup(m_commandBuffer, m_cameraRT, m_segmentResultTex, downSamplingResultTexID);

                int blurResultTexID = Shader.PropertyToID("_BlurResultTexID");
                m_commandBuffer.GetTemporaryRT(blurResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                DualKawaseBlur.Setup(m_width, m_height, m_commandBuffer, downSamplingResultTexID, blurResultTexID, BlurRadius, Iteration, BlurDownScaling);
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
        SegmentToolkit.Segment(m_cameraRT, m_segmentResultTex, flip);
    }

    private void OnApplicationQuit()
    {
        if (m_initialized)
            SegmentToolkit.ReleaseSession();
    }

}