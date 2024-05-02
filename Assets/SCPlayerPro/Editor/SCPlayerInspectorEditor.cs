using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sttplay.MediaPlayer;

[CanEditMultipleObjects]
[CustomEditor(typeof(UnitySCPlayerPro))]
public class SCPlayerInspectorEditor : Editor
{
    private SerializedProperty _propMediaType;
    private SerializedProperty _propAutoOpen;
    private SerializedProperty _propOpenAndPlay;
    private SerializedProperty _propLoop;
    private SerializedProperty _propVolume;
    private SerializedProperty _propSpeed;
    private SerializedProperty _propPath;
    private SerializedProperty _propDisableVideo;
    private SerializedProperty _propDisableAudio;
    private SerializedProperty _propEnableHWAccel;
    private SerializedProperty _propExtractHWFrame;
    private SerializedProperty _propOutputFmt;
    private SerializedProperty _propVideoTrackIndex;
    private SerializedProperty _propAudioTrackIndex;
    private SerializedProperty _propHWAccelType;
    private SerializedProperty _propCameraWidth;
    private SerializedProperty _propCameraHeight;
    private SerializedProperty _propCameraFPS;
    private SerializedProperty _propOptions;
    private SerializedProperty _propOnCaptureOpenCallbackEvent;
    private SerializedProperty _propOnStreamFinishedEvent;
    private SerializedProperty _propOnFirstFrameRenderEvent;
    private SerializedProperty _propOnRendererVideoFrameEvent;

    [MenuItem("GameObject/SCPlayerPro/SCPlayerPro", false, 10)]
    public static void CreateSCPlayerEditor()
    {
        GameObject go = new GameObject("SCPlayerPro");
        go.AddComponent<UnitySCPlayerPro>();
        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create SCUGUICanvas");
    }

    [MenuItem("GameObject/UI/SCUGUICanvas", false, 10000)]
    public static void CreateSCPlayerCanvasEditor()
    {
        GameObject parent = Selection.activeGameObject;
        RectTransform parentRect = (parent != null) ? parent.GetComponent<RectTransform>() : null;
        if (parentRect)
        {
            GameObject canvas = new GameObject("SCUGUICanvas");
            canvas.transform.SetParent(parent.transform, false);
            canvas.AddComponent<RectTransform>();
            canvas.AddComponent<CanvasRenderer>();
            canvas.AddComponent<SCUGUIRenderer>();
            Selection.activeGameObject = canvas;
            Undo.RegisterCreatedObjectUndo(canvas, "Create SCUGUICanvas");
        }
        else
        {
            EditorUtility.DisplayDialog("SCPlayerPro", "SCUGUICanvas must be a child of Canvas.", "Ok");
        }
    }

    private void OnEnable()
    {
        _propMediaType = serializedObject.FindProperty("openMode");
        _propAutoOpen = serializedObject.FindProperty("autoOpen");
        _propOpenAndPlay = serializedObject.FindProperty("openAndPlay");
        _propLoop = serializedObject.FindProperty("loop");
        _propVolume = serializedObject.FindProperty("volume");
        _propSpeed = serializedObject.FindProperty("speed");
        _propPath = serializedObject.FindProperty("url");
        _propDisableVideo = serializedObject.FindProperty("disableVideo");
        _propDisableAudio = serializedObject.FindProperty("disableAudio");
        _propEnableHWAccel = serializedObject.FindProperty("enableHWAccel");
        _propExtractHWFrame = serializedObject.FindProperty("extractHWFrame");
        _propOutputFmt = serializedObject.FindProperty("outputPixelFormat");
        _propVideoTrackIndex = serializedObject.FindProperty("defaultVideoTrack");
        _propAudioTrackIndex = serializedObject.FindProperty("defaultAudioTrack");
        _propHWAccelType = serializedObject.FindProperty("HWAccelType");

        _propCameraWidth = serializedObject.FindProperty("cameraWidth");
        _propCameraHeight = serializedObject.FindProperty("cameraHeight");
        _propCameraFPS = serializedObject.FindProperty("cameraFPS");

        _propOptions = serializedObject.FindProperty("options");

        _propOnCaptureOpenCallbackEvent = serializedObject.FindProperty("onCaptureOpenCallbackEvent");
        _propOnStreamFinishedEvent = serializedObject.FindProperty("onStreamFinishedEvent");
        _propOnFirstFrameRenderEvent = serializedObject.FindProperty("onFirstFrameRenderEvent");
        _propOnRendererVideoFrameEvent = serializedObject.FindProperty("onRenderVideoFrameEvent");
    }


    private static bool Browse(ref string fullPath)
    {
        bool result = false;

        string path = EditorUtility.OpenFilePanel("Browse Video File", "", "");
        if (!string.IsNullOrEmpty(path))
        {
            fullPath = path;
            result = true;
        }

        return result;
    }

    private void GetCameraInfoCallback(string info)
    {
        Debug.Log(info);
    }
    public override void OnInspectorGUI()
    {
        UnitySCPlayerPro player = (this.target) as UnitySCPlayerPro;
        serializedObject.Update();

        BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

        EditorGUILayout.Space(10);
        if (targetGroup != BuildTargetGroup.Standalone && targetGroup != BuildTargetGroup.Android)
        {
            GUILayout.Label("The current version of SCPlayerPro does not support " + targetGroup, EditorStyles.boldLabel);
            return;
        }
        EditorGUILayout.LabelField("Current Platform", targetGroup.ToString(), EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_propMediaType);

        EditorGUILayout.Space(10);

        if (_propMediaType.enumValueIndex == (int)MediaType.LocalFile)
        {
            EditorGUILayout.LabelField("Source Path     (eg.local file, http, https)", EditorStyles.boldLabel);
            {
                string oldPath = _propPath.stringValue;
                string newPath = EditorGUILayout.TextField(string.Empty, _propPath.stringValue);
                if (newPath != oldPath)
                {
                    // Check for invalid characters
                    if (0 > newPath.IndexOfAny(System.IO.Path.GetInvalidPathChars()))
                    {
                        _propPath.stringValue = newPath.Replace("\\", "/");
                        EditorUtility.SetDirty(target);
                    }
                }
            }

            GUILayout.BeginHorizontal();

            GUI.color = Color.green;
            if (GUILayout.Button("BROWSE"))
            {

                string fullPath = "";
                if (Browse(ref fullPath))
                {
                    _propPath.stringValue = fullPath;
                    EditorUtility.SetDirty(target);

                }
            }

            if (GUILayout.Button("      Clear       ", GUILayout.ExpandWidth(false)))
            {
                _propPath.stringValue = "";
            }

            GUI.color = Color.white;

            GUILayout.EndHorizontal();
        }
        else if (_propMediaType.enumValueIndex == (int)MediaType.Link)
        {
            EditorGUILayout.LabelField("URL link     (eg.local file, http, https, rtp, rtsp, rtmp, hls ...)", EditorStyles.boldLabel);
            {
                string oldPath = _propPath.stringValue;
                string newPath = EditorGUILayout.TextField(string.Empty, _propPath.stringValue);
                if (newPath != oldPath)
                {
                    // Check for invalid characters
                    if (0 > newPath.IndexOfAny(System.IO.Path.GetInvalidPathChars()))
                    {
                        _propPath.stringValue = newPath.Replace("\\", "/");
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }
        else if (_propMediaType.enumValueIndex == (int)MediaType.Camera)
        {
            EditorGUILayout.LabelField("Camera Name", EditorStyles.boldLabel);
            {
                string oldPath = _propPath.stringValue;
                string newPath = EditorGUILayout.TextField(string.Empty, _propPath.stringValue);
                if (newPath != oldPath)
                {
                    // Check for invalid characters
                    if (0 > newPath.IndexOfAny(System.IO.Path.GetInvalidPathChars()))
                    {
                        _propPath.stringValue = newPath.Replace("\\", "/");
                        EditorUtility.SetDirty(target);
                    }
                }
            }

            EditorGUILayout.PropertyField(_propCameraWidth);
            EditorGUILayout.PropertyField(_propCameraHeight);
            EditorGUILayout.PropertyField(_propCameraFPS);

            GUILayout.BeginHorizontal();

            GUI.color = Color.green;
            if (GUILayout.Button("Get Device List"))
            {
                var list = SCPlayerPro.GetDeviceList(Sttplay.MediaPlayer.DeviceType.VideoInput);
                string listStr = "Camera name list \n" + string.Join("\n", list);
                Debug.Log(listStr);
            }

            if (GUILayout.Button("Get Device Params"))
                SCPlayerPro.GetCameraInfomation(_propPath.stringValue.Trim(), GetCameraInfoCallback);

            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        {

            GUI.color = Color.white;

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Main", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_propAutoOpen);
            EditorGUILayout.PropertyField(_propOpenAndPlay);
            if (_propMediaType.enumValueIndex != (int)MediaType.Camera)
            {
                EditorGUILayout.PropertyField(_propLoop);
                EditorGUILayout.PropertyField(_propVolume);
            }

            EditorGUILayout.PropertyField(_propSpeed);

            EditorGUILayout.EndVertical();

            if (_propMediaType.enumValueIndex != (int)MediaType.Camera)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Stream", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_propDisableVideo);
                EditorGUILayout.PropertyField(_propDisableAudio);
                EditorGUILayout.EndVertical();
            }

            if (!_propDisableVideo.boolValue)
            {
                EditorGUILayout.Space(10);

                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Video Setting", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_propVideoTrackIndex);
                EditorGUILayout.PropertyField(_propOutputFmt);
                EditorGUILayout.PropertyField(_propEnableHWAccel);
                if (_propEnableHWAccel.boolValue)
                {
                    EditorGUILayout.PropertyField(_propExtractHWFrame);
                    EditorGUILayout.PropertyField(_propHWAccelType);
                }
                EditorGUILayout.EndVertical();
            }

            if (_propMediaType.enumValueIndex != (int)MediaType.Camera)
            {
                if (!_propDisableAudio.boolValue)
                {
                    EditorGUILayout.Space(10);

                    EditorGUILayout.BeginVertical("box");
                    GUILayout.Label("Audio Setting", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_propAudioTrackIndex);
                    EditorGUILayout.EndVertical();
                }
            }


            if (_propMediaType.enumValueIndex != (int)MediaType.LocalFile)
            {
                EditorGUILayout.Space(10);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(_propOptions);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_propOnCaptureOpenCallbackEvent);
            EditorGUILayout.PropertyField(_propOnFirstFrameRenderEvent);
            EditorGUILayout.PropertyField(_propOnRendererVideoFrameEvent);
            EditorGUILayout.PropertyField(_propOnStreamFinishedEvent);
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
