using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// MeshRenderer update based on SCRenderer in SCPlayerPro
    /// Through this class, you can easily switch the video source
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class SCMeshRenderer : SCRenderTarget
    {

        /// <summary>
        /// renderer target
        /// </summary>
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            if (meshRenderer != null)
                return;
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material.mainTexture = defaultTexture;
        }

        protected override void OnRendererChanged()
        {
            try
            {
                if (split == null)
                {
                    if (player.OpenSuccessed)
                        meshRenderer.material.mainTexture = player.VideoRenderer.SCRenderer.SyntheticTexture;
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
            meshRenderer.material.mainTexture = split.TransformRenderTexture(player.VideoRenderer.SCRenderer.SyntheticTexture);
        }
        protected override void OnCloseClicked()
        {
            if (!switchNotUpdate)
                meshRenderer.material.mainTexture = defaultTexture;
        }

        private void OnEnable()
        {
            OnRendererChanged();
        }
    }
}