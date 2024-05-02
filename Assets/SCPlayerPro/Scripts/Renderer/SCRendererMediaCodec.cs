using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// Renderer instance 
    /// </summary>
    public class SCRendererMediaCodec : SCRenderer
    {
        private bool isFirst = true;
        public override void InitRenderer(SCFrame frame)
        {
            SourceTextures = new Texture2D[1];
            RenderMaterial = new Material(Shader.Find("Sttplay/MediaCodec"));
            SourceTextures[0] = Texture2D.CreateExternalTexture(frame.width, frame.height, TextureFormat.RGBA32, false, false, fboId);
            base.InitRenderer(frame);
        }

        public override void Renderer(SCFrame frame)
        {
            if (frame.format != (int)PixelFormat.PIX_FMT_MEDIACODEC)
            {
                Debug.LogError("The pixel format is different from the canvas type");
                return;
            }
            if (isFirst)
            {
                if ((int)nativeRenderer.SendSignal(NativeRenderer.SIGNAL_UPDATE, frame) < 0)
                    Debug.LogError("Media codec frame render failed");
                nativeRenderer.SendSignal(NativeRenderer.SIGNAL_DRAW);
                isFirst = false;
            }
            if ((int)nativeRenderer.SendSignal(NativeRenderer.SIGNAL_UPDATE, frame) < 0)
                Debug.LogError("Media codec frame render failed");

            this.frame = frame;
        }

        public override void Apply()
        {
            nativeRenderer.SendSignal(NativeRenderer.SIGNAL_DRAW);
            if (!isInit)
            {
                isInit = true;
                InitRenderer(frame);
                SetColorSpace(frame);
            }
            RenderMaterial.SetTexture("_Tex", SourceTextures[0]);
            base.Apply();
        }

        public override void TerminateRenderer()
        {
            base.TerminateRenderer();
            Object.Destroy(SourceTextures[0]);
            SourceTextures[0] = null;
        }
    }
}
