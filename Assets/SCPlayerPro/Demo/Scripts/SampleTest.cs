using Sttplay.MediaPlayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple script to test the player
/// In this script, you can see some basic commonly used control methods
/// </summary>
public class SampleTest : MonoBehaviour
{
    /// <summary>
    /// Media type
    /// </summary>
    public MediaType openMode = MediaType.LocalFile;
    /// <summary>
    /// This variable is just for testing convenience and has no other effect,
    /// Mark whether to use the StreamAssets folder 
    /// If true, then the file in the StreamingAssets folder will be selected, otherwise it will be an absolute path or relative path
    /// </summary>
    public bool isSCAssetsFloder = true;

    /// <summary>
    /// path
    /// </summary>
    public string url = "SCPlayerProVideo/BigBuckBunny_720p30.mp4";

    /// <summary>
    /// all player
    /// </summary>
    public UnitySCPlayerPro[] player;

    /// <summary>
    /// open options
    /// </summary>
    public string options;

    /// <summary>
    /// open media
    /// </summary>
    public void Open()
    {
        for (int i = 0; i < player.Length; i++)
        {
            if (openMode == MediaType.LocalFile)
                player[i].Open(openMode, isSCAssetsFloder ? SCMGR.GetUrlFromSCSCAssets(url) : url);
            else
                player[i].Open(openMode, url);
        }

    }

    /// <summary>
    /// close media
    /// </summary>
    public void Close()
    {
        for (int i = 0; i < player.Length; i++)
        {
            player[i].Close();
        }
    }

    /// <summary>
    /// play media
    /// </summary>
    public void Play()
    {
        for (int i = 0; i < player.Length; i++)
        {
            player[i].Play();
        }
    }

    /// <summary>
    /// pause media
    /// </summary>
    public void Pause()
    {
        for (int i = 0; i < player.Length; i++)
        {
            player[i].Pause();
        }
    }

    /// <summary>
    /// open and play
    /// </summary>
    public void OpenAndPlay()
    {

        Open();
        Play();

    }

    /// <summary>
    /// replay and paused
    /// </summary>
    public void ReplayPaused()
    {
        for (int i = 0; i < player.Length; i++)
        {
            player[i].Replay(true);
        }
    }

    /// <summary>
    /// replay and play
    /// </summary>
    public void ReplayPlay()
    {
        for (int i = 0; i < player.Length; i++)
        {
            player[i].Replay(false);
        }
    }


    private void Start()
    {
        for(int i = 0; i < player.Length; i++)
        {
            player[i].options = options;
        }
        //ReplayPlay();
        //Close();
        Open();
        Play();
    }
}
