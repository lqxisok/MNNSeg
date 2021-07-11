using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video;

[RequireComponent(typeof(Camera))]
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
    public bool ActiveStableFilter = true;

    public Shader StableFilterShader;

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

    private RenderTexture m_segmentResultTex;

    private RenderTexture m_cameraRT;
    private RenderTexture m_cameraResizeRT;

    private List<RenderTexture> m_texIDList;

    private Quaternion m_OldOriention;

    private Camera cam;

    void Start()
    {
        m_OldOriention = transform.rotation;
        cam = GetComponent<Camera>();

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

            m_texIDList = new List<RenderTexture>();
            int currentID = 0;
            m_texIDList.Add(m_segmentResultTex);

            if (ActiveRefine)
            {
                m_texIDList.Add(new RenderTexture(m_width, m_height, 0));
                currentID ++;
                DownSampling.Setup(m_commandBuffer, RefineShader, m_cameraRT, m_texIDList[currentID - 1], m_texIDList[currentID], DistanceThreshold, ConfidenceThreshold);
            }
            
            if (ActiveBlur)
            {
                m_texIDList.Add(new RenderTexture(m_width, m_height, 0));
                currentID ++;
                DualKawaseBlur.Setup(m_commandBuffer, BlurShader, m_texIDList[currentID - 1], m_texIDList[currentID], BlurRadius, Iteration, BlurDownScaling);
            }

            if (ActiveStableFilter)
            {
                m_texIDList.Add(new RenderTexture(m_width, m_height, 0));
                currentID ++;
                StableFilter.Setup(m_commandBuffer, StableFilterShader, m_texIDList[currentID - 1], m_texIDList[currentID]);
            }

            if (ActiveGuideFilter)
            {
                m_texIDList.Add(new RenderTexture(m_width, m_height, 0));
                currentID ++;
                GuideFilter.Setup(m_commandBuffer,
                                MeanFilterShader, TexDotShader, GuideFilterShader,
                                m_width, m_height,
                                GuideFilterRadius, GuideFilterRegularization,
                                m_texIDList[currentID - 1], m_cameraRT, m_texIDList[currentID]);
            }

            m_commandBuffer.SetGlobalTexture("_SegTex", m_texIDList[currentID]);

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

        Quaternion delta = transform.rotation * Quaternion.Inverse(m_OldOriention);
        m_commandBuffer.SetGlobalMatrix("_HomographyMatrix", (cam.projectionMatrix.inverse * Matrix4x4.Rotate(delta) * cam.projectionMatrix).transpose);
        m_OldOriention = transform.rotation;
    }

    private void OnApplicationQuit()
    {
        if (m_initialized)
        {
            foreach (var item in m_texIDList)
                item.Release();

            SegmentToolkit.ReleaseSession();
        }
    }

}