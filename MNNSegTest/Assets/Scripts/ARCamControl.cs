using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Rendering;

public class ARCamControl : MonoBehaviour {

    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager m_CameraManager;

     /// <summary>
    /// Get or set the <c>ARCameraManager</c>.
    /// </summary>
    public ARCameraManager cameraManager
    {
        get { return m_CameraManager; }
        set { m_CameraManager = value; }
    }

    public Material ARBackgroundMat;

    [Range(1, 8)]
    public int CameraDownScaling = 2;
    
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

    private XRCameraImageConversionParams m_conversionParamsForSeg;

    private XRCameraImageConversionParams m_conversionParamsOriginal;

    private Texture2D m_textureForSeg;

    private Texture2D m_textureOriginal;
    
    private CommandBuffer m_commandBuffer;

    private RenderTexture m_segmentResultTex;

    private List<RenderTexture> m_texIDList;

    private Quaternion m_OldOriention;

    private Camera cam;
	
    void OnEnable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // Attempt to get the latest camera image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        XRCameraImage image;
        if (!cameraManager.TryGetLatestImage(out image))
        {
            return;
        }

        // Once we have a valid XRCameraImage, we can access the individual image "planes"
        // (the separate channels in the image). XRCameraImage.GetPlane provides
        // low-overhead access to this data. This could then be passed to a
        // computer vision algorithm. Here, we will convert the camera image
        // to an RGBA texture and draw it on the screen.

        // Choose an RGBA format.
        // See XRCameraImage.FormatSupported for a complete list of supported formats.

        if (!m_initialized)
        {
            m_OldOriention = transform.rotation;
            cam = GetComponent<Camera>();

            m_width = image.width / CameraDownScaling;
            m_height = image.height / CameraDownScaling;
            // Convert the image to format, flipping the image across the Y axis.
            // We can also get a sub rectangle, but we'll get the full image here.
            var format = TextureFormat.RGB24;
            m_conversionParamsOriginal = new XRCameraImageConversionParams(image, format, CameraImageTransformation.None);
            m_textureOriginal = new Texture2D(image.width, image.height, format, false);

            m_conversionParamsForSeg = new XRCameraImageConversionParams(image, format, CameraImageTransformation.MirrorX);
            m_conversionParamsForSeg.outputDimensions = new Vector2Int(m_width, m_height);
            m_textureForSeg = new Texture2D(m_width, m_height, format, false);
            
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
                DownSampling.Setup(m_commandBuffer, RefineShader, m_textureOriginal, m_texIDList[currentID - 1], m_texIDList[currentID], DistanceThreshold, ConfidenceThreshold);
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
                                m_texIDList[currentID - 1], m_textureOriginal, m_texIDList[currentID]);
            }

            m_commandBuffer.SetGlobalTexture("_SegTex", m_texIDList[currentID]);

            cam.AddCommandBuffer(CameraEvent.AfterSkybox, m_commandBuffer);

            m_initialized = true;
        }


        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureDataForSeg = m_textureForSeg.GetRawTextureData<byte>();
        var rawTextureDataOriginal = m_textureOriginal.GetRawTextureData<byte>();
        try
        {
            image.Convert(m_conversionParamsForSeg, new IntPtr(rawTextureDataForSeg.GetUnsafePtr()), rawTextureDataForSeg.Length);
            image.Convert(m_conversionParamsOriginal, new IntPtr(rawTextureDataOriginal.GetUnsafePtr()), rawTextureDataOriginal.Length);
        }
        finally
        {
            // We must dispose of the XRCameraImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        m_textureForSeg.Apply();
        m_textureOriginal.Apply();

        SegmentToolkit.Segment(m_textureForSeg, m_segmentResultTex, flip);

        // Update camera homography matrix
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