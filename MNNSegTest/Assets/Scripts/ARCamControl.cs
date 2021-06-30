﻿using System;
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

    public ComputeShader FlipShader;

    public ComputeShader FlipAndSplitShader;

    [Range(1, 4)]
    public int CameraDownScaling = 2;

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

    private XRCameraImageConversionParams m_conversionParams;

    private Texture2D m_texture;
    
    private CommandBuffer m_commandBuffer;

    private RenderTexture m_segmentResultTex;
	
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
            m_width = image.width / CameraDownScaling;
            m_height = image.height / CameraDownScaling;
            // Convert the image to format, flipping the image across the Y axis.
            // We can also get a sub rectangle, but we'll get the full image here.
            var format = TextureFormat.RGB24;
            m_conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.MirrorX);
            m_conversionParams.outputDimensions = new Vector2Int(m_width, m_height);

            m_texture = new Texture2D(m_width, m_height, format, false);
            
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
            else if (!ActiveBlur)
            {
                int downSamplingResultTexID = Shader.PropertyToID("_DownSamplingResultTex");
                m_commandBuffer.GetTemporaryRT(downSamplingResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                DownSampling.Setup(m_commandBuffer, m_texture, m_segmentResultTex, downSamplingResultTexID);

                m_commandBuffer.SetGlobalTexture("_SegTex", downSamplingResultTexID);
                
                m_commandBuffer.ReleaseTemporaryRT(downSamplingResultTexID);
            }
            else if (!ActiveRefine)
            {
                int blurResultTexID = Shader.PropertyToID("_BlurResultTexID");
                m_commandBuffer.GetTemporaryRT(blurResultTexID, m_width, m_height, 0, FilterMode.Bilinear);
                int cameraTexID = Shader.PropertyToID("_CameraTexID");
                m_commandBuffer.GetTemporaryRT(cameraTexID, m_width, m_height, 0, FilterMode.Bilinear);

                m_commandBuffer.Blit(m_texture, cameraTexID);
                DualKawaseBlur.Setup(m_width, m_height, m_commandBuffer, cameraTexID, blurResultTexID, BlurRadius, Iteration, BlurDownScaling);
                m_commandBuffer.SetGlobalTexture("_SegTex", blurResultTexID);
                
                m_commandBuffer.ReleaseTemporaryRT(blurResultTexID);
                m_commandBuffer.ReleaseTemporaryRT(cameraTexID);
            }
            else
            {
                int downSamplingResultTexID = Shader.PropertyToID("_DownSamplingResultTex");
                m_commandBuffer.GetTemporaryRT(downSamplingResultTexID, m_width, m_height, 0, FilterMode.Bilinear);
                
                DownSampling.Setup(m_commandBuffer, m_texture, m_segmentResultTex, downSamplingResultTexID);
                
                int blurResultTexID = Shader.PropertyToID("_BlurResultTexID");
                m_commandBuffer.GetTemporaryRT(blurResultTexID, m_width, m_height, 0, FilterMode.Bilinear);

                DualKawaseBlur.Setup(m_width, m_height, m_commandBuffer, downSamplingResultTexID, blurResultTexID, BlurRadius, Iteration, BlurDownScaling);
                m_commandBuffer.SetGlobalTexture("_SegTex", blurResultTexID);

                m_commandBuffer.ReleaseTemporaryRT(downSamplingResultTexID);
                m_commandBuffer.ReleaseTemporaryRT(blurResultTexID);
            }

            GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterSkybox, m_commandBuffer);

            m_initialized = true;
        }


        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = m_texture.GetRawTextureData<byte>();
        try
        {
            image.Convert(m_conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // We must dispose of the XRCameraImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        m_texture.Apply();

        SegmentToolkit.Segment(m_texture, m_segmentResultTex, flip);
    }

    private void OnApplicationQuit()
    {
        if (m_initialized)
            SegmentToolkit.ReleaseSession();
    }
}