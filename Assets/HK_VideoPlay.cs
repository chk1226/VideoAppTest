using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class HK_VideoPlay : MonoBehaviour
{
    protected UnityEngine.Video.VideoPlayer videoPlayer = null;
    protected RenderTexture videoRenderTexture = null;
    protected RawImage planeImage = null;

    public String VideoName;
    public VideoClip VideoClipData = null;
    public bool IsStreamingAssert = true;
    public bool UseRenderTexture = true;
    // Start is called before the first frame update
    void Start()
    {
#if (UNITY_EDITOR_WIN || UNITY_EDITOR)
        string videoUrl = Application.streamingAssetsPath + "/" + VideoName + ".mp4";
#elif UNITY_ANDROID
        string videoUrl = "jar:file://" + Application.dataPath + "!/assets/" + VideoName + ".mp4";
#endif
        Debug.Log($"video url: {videoUrl}");

        planeImage = GetComponent<RawImage>();

        videoPlayer = GetComponent<VideoPlayer>();
        if (IsStreamingAssert)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoUrl;
        }
        else
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = VideoClipData;
        }
        videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
        videoPlayer.Prepare();


    }

    protected void VideoPlayer_prepareCompleted(UnityEngine.Video.VideoPlayer source)
    {
        if(source.isPrepared)
        {
            Debug.Log("prepare complete");
            Debug.Log($"video w({source.width}) h({source.height})");

            if(UseRenderTexture)
            {
                if (!videoRenderTexture)
                {
                    videoRenderTexture = new RenderTexture((int)source.width, (int)source.height, 0, RenderTextureFormat.ARGB32);
                }
                planeImage.texture = videoRenderTexture;
                videoPlayer.targetTexture = videoRenderTexture;
            }

            source.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
