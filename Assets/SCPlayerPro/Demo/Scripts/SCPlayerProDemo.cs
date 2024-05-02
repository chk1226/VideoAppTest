using System;
using System.Collections;
using System.Collections.Generic;
using Sttplay.MediaPlayer;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// A simple video player based on SCPlayerPro 
/// Here is how to set the output pixel format and set the hardware acceleration type,
/// disable or enable audio and video ,
/// pause on first frame, loop media, reopen, pause, play
/// </summary>
public class SCPlayerProDemo : MonoBehaviour
{
   
    /// <summary>
    /// player
    /// </summary>
    public UnitySCPlayerPro player;

    /// <summary>
    /// This variable is just for testing convenience and has no other effect,
    /// Mark whether to use the StreamAssets folder 
    /// If true, then the file in the StreamingAssets folder will be selected, otherwise it will be an absolute path or relative path
    public bool isSCAssetsFloder = true;

    public Text infoText;

    public MediaType mediaType;

    /// <summary>
    /// url
    /// </summary>
    public string url = "SCPlayerProVideo/BigBuckBunny_720p30.mp4";

    #region ui
    public Button settingBtn;
    public GameObject settingPanel;
    public Button playBtn;
    public Button closeBtn;
    public Button reOpenBtn;
    public Button pauseOnOneFrameBtn;
    public Button loopBtn;
    public InputField urlInputField;
    public Toggle disableVideo;
    public Toggle disableAudio;
    public Toggle enableHWAccel;

    public Dropdown outputPixelFormat;
    public Dropdown hwaccelDeviceType;

    public Text realTime;
    public Text totalTime;

    public SliderPro seekSlider;
    public SliderPro volumeSlider;

    public Sprite playSprite, pauseSprite;
    #endregion
    private bool needhour;
    private int fps;
    private long ts;
    
    /// <summary>
    /// set ui and player
    /// </summary>
    void Start()
    {
        settingBtn.onClick.AddListener(OnSettingBtnClicked);
        settingBtn.onClick.Invoke();
        urlInputField.text = isSCAssetsFloder ? SCMGR.GetUrlFromSCSCAssets(url) : url;
        playBtn.onClick.AddListener(OnPlayBtnClicked);
        closeBtn.onClick.AddListener(OnCloseBtnClicked);
        reOpenBtn.onClick.AddListener(ReOpenBtnClicked);
        disableVideo.SetIsOnWithoutNotify(player.disableVideo);
        disableVideo.onValueChanged.AddListener((value) => { player.disableVideo = value; });
        disableAudio.SetIsOnWithoutNotify(player.disableAudio);
        disableAudio.onValueChanged.AddListener((value) => { player.disableAudio = value; });
        enableHWAccel.SetIsOnWithoutNotify(player.enableHWAccel);
        hwaccelDeviceType.interactable = player.enableHWAccel;
        enableHWAccel.onValueChanged.AddListener((value) =>
        {
            player.enableHWAccel = value;
            hwaccelDeviceType.interactable = player.enableHWAccel;
        });
        outputPixelFormat.captionText.text = player.outputPixelFormat.ToString();
        outputPixelFormat.onValueChanged.AddListener((value) =>
        {
            var type = (PixelFormat)System.Enum.Parse(typeof(PixelFormat), outputPixelFormat.options[value].text);
            player.outputPixelFormat = type;
        });
        hwaccelDeviceType.captionText.text = player.HWAccelType.ToString();
        hwaccelDeviceType.onValueChanged.AddListener((value) =>
        {
            var type = (HWDeviceType)System.Enum.Parse(typeof(HWDeviceType), hwaccelDeviceType.options[value].text);
            player.HWAccelType = type;
        });
        pauseOnOneFrameBtn.onClick.AddListener(() => { OnPauseOnOneFrameImplemente(false); });
        loopBtn.onClick.AddListener(() => { OnLoopImplemente(false); });
        OnPauseOnOneFrameImplemente(true);
        OnLoopImplemente(true);

        seekSlider.onSliderRelease.AddListener(OnSeekSliderRelease);
        volumeSlider.SetValueWithoutNotify(player.volume);
        volumeSlider.onValueChanged.AddListener((value) => { player.volume = value; });
        player.onCaptureOpenCallbackEvent.AddListener(OnCaptureOpenCallback);
        //player.onFirstFrameRenderEvent.AddListener(OnFristFrameRenderer);
        player.onStreamFinishedEvent.AddListener(OnStreamFinished);
        player.onRenderVideoFrameEvent.AddListener(OnVideoFrameRender);
        ts = ISCNative.GetTimestamp();
    }

    private void OnVideoFrameRender(SCRenderer renderer)
    {
        fps++;
    }

    private void OnSettingBtnClicked()
    {
        settingPanel.SetActive(!settingPanel.activeSelf);
    }

    private void OnStreamFinished()
    {
        if (!player.loop)
            SetPlayBtnIcon(false);
    }


    private void OnCaptureOpenCallback(CaptureOpenResult result, string error, OpenCallbackContext ctx)
    {
        if (result == CaptureOpenResult.SUCCESS)
        {
            needhour = player.Duration / 3600000 >= 1 ? true : false;
            totalTime.text = ConvertToTime(player.Duration, needhour);
        }
        else
            OnCloseBtnClicked();
    }

    private static string ConvertToTime(long ms, bool needhour)
    {
        System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, (int)ms);
        string timeStr = "";
        if (needhour)
            timeStr = ts.ToString(@"hh\:mm\:ss");
        else
            timeStr = ts.ToString(@"mm\:ss");

        return timeStr;

    }

    // Update is called once per frame
    void Update()
    {
        if (player.OpenSuccessed && !seekSlider.IsPress)
        {
            realTime.text = ConvertToTime(player.CurrentTime, needhour);
            seekSlider.SetValueWithoutNotify((float)player.CurrentTime / player.Duration);
        }
        if(ISCNative.GetTimestamp() - ts >= 1000000)
        {
            ts += 1000000;
            infoText.text = string.Format("format : {0}\nrender FPS : {1}\ndecoder FPS : {2}", player.GetRenderPixelFormat(), fps, player.decoderFps);
            fps = 0;
        }
    }

    private void OnSeekSliderRelease(float value)
    {
        bool usePrecentSeek = false;
        if (usePrecentSeek)
            player.SeekFastPercent(value);
        else
            player.SeekFastMilliSecond((int)(value * player.Duration));
    }
    public void OnPlayBtnClicked()
    {
        if (player.Closed)
        {
            ReOpenBtnClicked();
            return;
        }
        else
        {
            if (player.CurrentTime == player.Duration)
            {
                ReOpenBtnClicked();
                SetPlayBtnIcon(true);
                return;
            }
            SetPlayBtnIcon(player.IsPaused);
            if (player.IsPaused) player.Play();
            else player.Pause();
        }
    }

    public void ReOpenBtnClicked()
    {
        player.url = urlInputField.text;
        player.Open(mediaType);
        SetPlayBtnIcon(player.openAndPlay);
    }

    private void OnCloseBtnClicked()
    {
        player.Close();
        SetPlayBtnIcon(false);
        realTime.text = totalTime.text = "--:--:--";
    }

    private void SetPlayBtnIcon(bool paused)
    {
        playBtn.image.sprite = paused ? pauseSprite : playSprite;
    }

    private void OnPauseOnOneFrameImplemente(bool onlyUpdateIcon)
    {
        if (!onlyUpdateIcon)
            player.openAndPlay = !player.openAndPlay;
        pauseOnOneFrameBtn.transform.parent.GetComponent<Image>().color = !player.openAndPlay ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
    }

    private void OnLoopImplemente(bool onlyUpdateIcon)
    {
        if (!onlyUpdateIcon)
        {
            player.loop = !player.loop;
        }
        loopBtn.transform.parent.GetComponent<Image>().color = player.loop ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
    }

}
