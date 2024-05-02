using Sttplay.MediaPlayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sttplay.MediaPlayer
{

    /// <summary>
    /// Change the video display mode of special layout. 
    /// This kind of video may contain transparent channel data on the right side, 
    /// or may contain transparent channel data below.
    /// </summary>
    public class SCSplitTexture : MonoBehaviour
    {
        public enum AlphaVideoType
        {
            LeftRight = 0,
            TopBottom
        }
        private Material mat;
        private RenderTexture dest;
        public AlphaVideoType alphaType = AlphaVideoType.LeftRight;
        private AlphaVideoType lastAlphaType;

        private void OnEnable()
        {
            var target = GetComponent<SCRenderTarget>();
            if (target == null)
                return;
            if(mat == null)
            {
                mat = new Material(Shader.Find("Sttplay/Split"));
                lastAlphaType = alphaType;
            }
            target.RegisterSplit(this);
        }

        private void OnDisable()
        {
            var target = GetComponent<SCRenderTarget>();
            if (target == null)
                return;
            target.UnregisterSplit();
        }

        private void OnDestroy()
        {
            dest = null;
        }

        private void Update()
        {
            if (lastAlphaType != alphaType)
                OnEnable();
            lastAlphaType = alphaType;
        }

        public RenderTexture TransformRenderTexture(RenderTexture src)
        {
            retry:
            if(dest == null)
            {
                dest = new RenderTexture(src.width, src.height, 0);
                dest.wrapMode = TextureWrapMode.Clamp;
            }
            if (dest.width != src.width || dest.height != src.height)
            {
                OnDestroy();
                goto retry;
            }

            mat.SetTexture("_Tex", src);
            mat.SetInt("_AlphaDir", (int)alphaType);
            Graphics.Blit(null, dest, mat);
            return dest;
        }
    }
}