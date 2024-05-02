using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace Sttplay.MediaPlayer
{

    public class SCRenderer
    {
        /// <summary>
        /// color space
        /// </summary>
        public ShaderColorSpace colorSpace = ShaderColorSpace.BT601;

        /// <summary>
        /// pixel format
        /// </summary>
        public PixelFormat PixelFmort { get; set; }


        /// <summary>
        /// Set the texture with this material
        /// </summary>
        public Material RenderMaterial { get; protected set; }


        /// <summary>
        /// composition texture
        /// </summary>
        public RenderTexture SyntheticTexture { get; private set; }

        /// <summary>
        /// Different pixel format will use a different number of textures, so here is an array
        /// </summary>
        public Texture2D[] SourceTextures { get; protected set; }

        /// <summary>
        /// Video frame data needs to be backed up, in some cases
        /// </summary>
        protected System.IntPtr framePtr;

        /// <summary>
        /// current frame
        /// </summary>
        protected SCFrame frame;

        /// <summary>
        /// current frame cache
        /// </summary>
        protected byte[] cacheBK;

        protected bool isInit = false;

        /// <summary>
        /// 标记Render是否执行成功
        /// </summary>
        public bool IsVaild { get; set; }

        protected NativeRenderer nativeRenderer;
        protected System.IntPtr fboId;

        /// <summary>
        /// Create render texture
        /// </summary>
        /// <param name="frame"></param>
        public virtual void InitRenderer(SCFrame frame)
        {
            if (SyntheticTexture != null)
                TerminateRenderer();
            //SyntheticTexture = RenderTexture.GetTemporary(frame.width, frame.height);
            SyntheticTexture = new RenderTexture(frame.width, frame.height, 0);
            //SyntheticTexture.wrapMode = TextureWrapMode.Repeat;
        }

        public void SetNativeRenderer(NativeRenderer nativeRenderer, IntPtr fboId)
        {
            this.nativeRenderer = nativeRenderer;
            this.fboId = fboId;
        }

        /// <summary>
        /// Terminate render texture
        /// </summary>
        public virtual void TerminateRenderer()
        {
            ReleaseImageData();
            SyntheticTexture = null;
        }

        /// <summary>
        /// Render frame
        /// There is no real drawing here, but the video frame data is aligned and copied in a one-byte manner.
        /// </summary>
        /// <param name="frame"></param>
        public virtual void Renderer(SCFrame frame) { }

        /// <summary>
        /// Unity real drawing video data is finally filled in RenderTexture
        /// </summary>
        public virtual void Apply()
        {
            Graphics.Blit(null, SyntheticTexture, RenderMaterial);
        }

        /// <summary>
        /// set color space
        /// </summary>
        /// <param name="colorSpace"></param>
        public void SetColorSpace(SCFrame frame)
        {
            if ((SCColorRange)frame.color_range == SCColorRange.JPEG)
                colorSpace = ShaderColorSpace.JPEG;
            else if ((SCColorSpace)frame.color_space == SCColorSpace.BT709)
                colorSpace = ShaderColorSpace.BT709;
            else if ((SCColorSpace)frame.color_range == SCColorSpace.BT470BG ||
                (SCColorSpace)frame.color_range == SCColorSpace.SMPTE170M ||
                (SCColorSpace)frame.color_range == SCColorSpace.SMPTE240M)
                colorSpace = ShaderColorSpace.BT601;

            bool needSet = PixelFmort == PixelFormat.NV12 || PixelFmort == PixelFormat.NV21 || PixelFmort == PixelFormat.UYVY422 || PixelFmort == PixelFormat.YUYV422 || PixelFmort == PixelFormat.YUV420P || PixelFmort == PixelFormat.YUV422P || PixelFmort == PixelFormat.YUV444P || PixelFmort == PixelFormat.YUVJ420P || PixelFmort == PixelFormat.YUVJ422P || PixelFmort == PixelFormat.YUVJ444P;
            if (!needSet)
                return;

            if (colorSpace == ShaderColorSpace.JPEG)
            {
                RenderMaterial.SetVector("_OFFSET", new Vector4(0f, -0.501960814f, -0.501960814f));
                RenderMaterial.SetVector("_RCOEFF", new Vector4(1f, 0.000f, 1.402f));
                RenderMaterial.SetVector("_GCOEFF", new Vector4(1f, -0.3441f, -0.7141f));
                RenderMaterial.SetVector("_BCOEFF", new Vector4(1f, 1.772f, 0.000f));
            }
            else if (colorSpace == ShaderColorSpace.BT709)
            {
                RenderMaterial.SetVector("_OFFSET", new Vector4(-0.0627451017f, -0.501960814f, -0.501960814f));
                RenderMaterial.SetVector("_RCOEFF", new Vector4(1.1644f, 0.000f, 1.7927f));
                RenderMaterial.SetVector("_GCOEFF", new Vector4(1.1644f, -0.2132f, -0.5329f));
                RenderMaterial.SetVector("_BCOEFF", new Vector4(1.1644f, 2.1124f, 0.000f));
            }
            else
            {
                RenderMaterial.SetVector("_OFFSET", new Vector4(-0.0627451017f, -0.501960814f, -0.501960814f));
                RenderMaterial.SetVector("_RCOEFF", new Vector4(1.1644f, 0.000f, 1.596f));
                RenderMaterial.SetVector("_GCOEFF", new Vector4(1.1644f, -0.3918f, -0.813f));
                RenderMaterial.SetVector("_BCOEFF", new Vector4(1.1644f, 2.0172f, 0.000f));
            }
        }

        /// <summary>
        /// backup frame 
        /// </summary>
        /// <param name="srcFrame"></param>
        public void CopyImageData(SCFrame srcFrame)
        {
            if (frame.width != srcFrame.width || frame.height != srcFrame.height || frame.format != srcFrame.format)
                ReleaseImageData();

            if (srcFrame.format == (int)PixelFormat.GRAY8)
            {
                try
                {
                    if (cacheBK == null)
                    {
                        cacheBK = new byte[srcFrame.width * srcFrame.height];
                        frame = new SCFrame();
                        frame.data = new System.IntPtr[1];
                        frame.width = srcFrame.width;
                        frame.height = srcFrame.height;
                    }
                    frame.data[0] = ISCNative.MemoryAlignment(srcFrame.data[0], srcFrame.width, srcFrame.height, srcFrame.linesize[0], cacheBK);
                }
                catch
                {
                    IsVaild = false;
                }
            }
            else
            {
                var ptr = ISCNative.StructureToIntPtr(srcFrame);
                if (framePtr == System.IntPtr.Zero)
                    framePtr = ISCNative.CreateSCFrame(ptr, 1, (int)SCFrameFlag.Copy);
                if(framePtr == IntPtr.Zero)
                {
                    IsVaild = false;
                }
                else
                {
                    ISCNative.ImageCopy(framePtr, ptr);
                    frame = Marshal.PtrToStructure<SCFrame>(framePtr);
                }
                ISCNative.ReleaseStructIntPtr(ptr);
            }

        }

        /// <summary>
        /// Release frame
        /// </summary>
        private void ReleaseImageData()
        {
            ISCNative.ReleaseSCFrame(framePtr);
            framePtr = IntPtr.Zero;
            cacheBK = null;
        }
    }

}