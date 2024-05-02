using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// Renderer instance 
    /// The pixel format is RGB24, so 1 texture is needed
    /// format:(4x4)
    /// R G B R G B R G B
    /// R G B R G B R G B
    /// R G B R G B R G B
    /// R G B R G B R G B
    /// </summary>
    public class SCRendererRGB24 : SCRenderer
    {

        public override void InitRenderer(SCFrame frame)
        {
            SourceTextures = new Texture2D[1];
            RenderMaterial = new Material(Shader.Find("Sttplay/RGB"));
            SourceTextures[0] = new Texture2D(frame.width, frame.height, TextureFormat.RGB24, false);
            base.InitRenderer(frame);
        }

        public override void Renderer(SCFrame frame)
        {
            if (frame.format != (int)PixelFormat.RGB24)
            {
                Debug.LogError("The pixel format is different from the canvas type");
                return;
            }

            if (frame.linesize[0] != frame.width * 3)
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

            SourceTextures[0].LoadRawTextureData(frame.data[0], frame.width * frame.height * 3);

            SourceTextures[0].Apply();

            RenderMaterial.SetTexture("_Tex", SourceTextures[0]);
            base.Apply();
        }
    }
}