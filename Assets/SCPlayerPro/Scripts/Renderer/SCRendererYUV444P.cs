using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// Renderer instance 
    /// The pixel format is YUV444P, so 3 textures are required
    /// format:(4x4)
    /// Y Y Y Y
    /// Y Y Y Y
    /// Y Y Y Y
    /// Y Y Y Y
    /// U U U U
    /// U U U U
    /// U U U U
    /// V V V V
    /// V V V V
    /// V V V V
    /// V V V V
    /// </summary>
    public class SCRendererYUV444P : SCRenderer
    {

        public override void InitRenderer(SCFrame frame)
        {
            SourceTextures = new Texture2D[3];
            RenderMaterial = new Material(Shader.Find("Sttplay/XYUVX"));
            SourceTextures[0] = new Texture2D(frame.linesize[0], frame.height, GraphicsFormat.R8_UNorm, TextureCreationFlags.None);
            SourceTextures[1] = new Texture2D(frame.linesize[1], frame.height, GraphicsFormat.R8_UNorm, TextureCreationFlags.None);
            SourceTextures[2] = new Texture2D(frame.linesize[2], frame.height, GraphicsFormat.R8_UNorm, TextureCreationFlags.None);
            base.InitRenderer(frame);
        }

        public override void Renderer(SCFrame frame)
        {
            if (frame.format != (int)PixelFormat.YUV444P && frame.format != (int)PixelFormat.YUVJ444P)
            {
                Debug.LogError("The pixel format is different from the canvas type");
                return;
            }

            if (frame.linesize[0] != frame.width || frame.linesize[1] != frame.width / 2 || frame.linesize[2] != frame.width / 2)
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
            SourceTextures[1].LoadRawTextureData(frame.data[1], frame.linesize[1] * frame.height);
            SourceTextures[2].LoadRawTextureData(frame.data[2], frame.linesize[2] * frame.height);

            SourceTextures[0].Apply();
            SourceTextures[1].Apply();
            SourceTextures[2].Apply();

            RenderMaterial.SetTexture("_YTex", SourceTextures[0]);
            RenderMaterial.SetTexture("_UTex", SourceTextures[1]);
            RenderMaterial.SetTexture("_VTex", SourceTextures[2]);
            base.Apply();
        }
    }
}
