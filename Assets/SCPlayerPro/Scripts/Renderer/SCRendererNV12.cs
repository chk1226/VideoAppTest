using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// Renderer instance 
    /// The pixel format is NV12, so 2 textures are required
    /// format:(4x4)
    /// Y Y Y Y
    /// Y Y Y Y
    /// Y Y Y Y
    /// Y Y Y Y
    /// U V U V
    /// U V U V
    /// U V U V
    /// U V U V
    /// </summary>
    public class SCRendererNV12 : SCRenderer
    {

        public override void InitRenderer(SCFrame frame)
        {
            SourceTextures = new Texture2D[2];
            RenderMaterial = new Material(Shader.Find("Sttplay/NV12"));
            SourceTextures[0] = new Texture2D(frame.linesize[0], frame.height, GraphicsFormat.R8_UNorm, TextureCreationFlags.None);
            SourceTextures[1] = new Texture2D(frame.linesize[1] / 2, frame.height / 2, GraphicsFormat.R8G8_UNorm, TextureCreationFlags.None);
            base.InitRenderer(frame);
        }

        public override void Renderer(SCFrame frame)
        {
            if (frame.format != (int)PixelFormat.NV12)
            {
                Debug.LogError("The pixel format is different from the canvas type");
                return;
            }

            if (frame.linesize[0] != frame.width || frame.linesize[1] != frame.width)
                CopyImageData(frame);
            else
                this.frame = frame;

        }

        public override void Apply()
        {
            if (!isInit)
            {
                isInit = true;
                InitRenderer(frame);
                SetColorSpace(frame);
            }

            SourceTextures[0].LoadRawTextureData(frame.data[0], frame.linesize[0] * frame.height);
            SourceTextures[1].LoadRawTextureData(frame.data[1], frame.linesize[1] * frame.height / 2);

            SourceTextures[0].Apply();
            SourceTextures[1].Apply();

            RenderMaterial.SetTexture("_YTex", SourceTextures[0]);
            RenderMaterial.SetTexture("_UVTex", SourceTextures[1]);
            base.Apply();
        }
    }
}