using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// MeshRenderer update based on SCRenderer in SCPlayerPro
    /// Through this class, you can easily switch the video source
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class SCUGUIRenderer : SCRenderTarget
    {

        /// <summary>
        /// renderer target
        /// </summary>
        private RawImage rawImage;



        private void Awake()
        {
            if (rawImage != null)
                return;
            rawImage = GetComponent<RawImage>();
            rawImage.texture = defaultTexture;
        }

        private void OnValidate()
        {
            if (rawImage == null)
                Awake();
            if (!opening)
                rawImage.texture = defaultTexture;
        }

        protected override void OnRendererChanged()
        {
            try
            {
                if (split == null)
                {
                    if (player.OpenSuccessed)
                        rawImage.texture = player.VideoRenderer.SCRenderer.SyntheticTexture;
                }
                else
                    OnRenderFrame(player.VideoRenderer.SCRenderer);
            }
            catch
            {
            }
        }

        protected override void OnRenderFrame(SCRenderer renderer)
        {
            if (split == null)
                return;
            rawImage.texture = split.TransformRenderTexture(player.VideoRenderer.SCRenderer.SyntheticTexture);
        }

        protected override void OnCloseClicked()
        {
            if (!switchNotUpdate)
                rawImage.texture = defaultTexture;
        }

        private void OnEnable()
        {
            OnRendererChanged();
        }
    }
}
