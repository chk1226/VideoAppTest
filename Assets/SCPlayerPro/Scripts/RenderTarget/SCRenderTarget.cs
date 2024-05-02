using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sttplay.MediaPlayer
{

    /// <summary>
    /// MeshRenderer update based on SCRenderer in SCPlayerPro
    /// Through this class, you can easily switch the video source
    /// </summary>
    public class SCRenderTarget : MonoBehaviour
    {
        /// <summary>
        /// player, can be set at any time  
        /// </summary>
        public UnitySCPlayerPro player;
        private UnitySCPlayerPro lastPlayer;

        /// <summary>
        /// show default texture
        /// </summary>
        public Texture defaultTexture;

        /// <summary>
        /// The screen does not refresh when SCPlayerPro is closed or when the video source is switched
        /// </summary>
        public bool switchNotUpdate;

        protected SCSplitTexture split;

        protected bool opening = false;


        public void Update()
        {
            if (player != lastPlayer)
                OnPlayerChanged();
            lastPlayer = player;
        }

        /// <summary>
        /// Called when the player changes 
        /// </summary>
        private void OnPlayerChanged()
        {
            if (lastPlayer != null)
            {
                lastPlayer.onCloseEvent.RemoveListener(OnCloseClicked);
                lastPlayer.onRendererChangedEvent.RemoveListener(OnRendererChanged);
                lastPlayer.onRenderVideoFrameEvent.RemoveListener(OnRenderFrame);
                opening = false;
                OnCloseClicked();
            }

            if (player == null)
            {
                opening = false;
                OnCloseClicked();
            }
            else
            {
                player.onCloseEvent.AddListener(OnCloseClicked);
                player.onRendererChangedEvent.AddListener(OnRendererChanged);
                player.onRenderVideoFrameEvent.AddListener(OnRenderFrame);
                if (player.VideoRenderer != null && player.VideoRenderer.SCRenderer != null && player.VideoRenderer.SCRenderer.SyntheticTexture != null)
                    OnRendererChanged();
            }
        }

        protected virtual void OnRenderFrame(SCRenderer renderer){ }

        private void OnRendererChanged(SCRenderer renderer)
        {
            opening = true;
            OnRendererChanged();
        }


        protected virtual void OnRendererChanged() { }
        /// <summary>
        /// set default materials 
        /// </summary>
        protected virtual void OnCloseClicked() { }

        public void RegisterSplit(SCSplitTexture split)
        {
            this.split = split;
            OnRendererChanged();
        }

        public void UnregisterSplit()
        {
            this.split = null;
            OnRendererChanged();
        }
    }
}