using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Sttplay.MediaPlayer
{
    public class XRendererEx
    {
        public enum ColorSpace
        {
            BT601 = 0,
            BT709,
            JPEG,
            UNSPECIFIED
        }

        public enum RenderPixelFormat
        {
            AUTO = -1,

            YUV420P = 0,
            YUV422P = 4,
            YUV444P = 5,

            YUYV422 = 1,
            UYVY422 = 15,

            GRAY8 = 8,

            YUVJ420P = 12,
            YUVJ422P = 13,
            YUVJ444P = 14,

            NV12 = 23,
            NV21 = 24,

            RGB24 = 2,
            BGR24 = 3,

            ARGB32 = 25,
            RGBA32 = 26,
            ABGR32 = 27,
            BGRA32 = 28,

            //HW FMT
            DXVA2 = 51,
            CUDA = 117,
            MEDIACODEC = 165,
            D3D11 = 172,
        }

        public const int MODEL_QUAD = 0;
        public const int MODEL_SPHERE = 1;
        public const int NUM_OF_MODEL = 2;

        public const int STRETCH_FILL = 0;
        public const int STRETCH_UNIFORM = 1;

        public const int BEHAVE_NONE = 0;
        public const int BEHAVE_ROTATION_90 = 0x0001;
        public const int BEHAVE_ROTATION_180 = 0x0002;
        public const int BEHAVE_ROTATION_270 = 0x0004;
        public const int BEHAVE_MIRROR_HORIZONTAL = 0x0008;
        public const int BEHAVE_MIRROR_VERTICAL = 0x0010;

        public const int PROJ_ORTHOGRAPHIC = 0;
        public const int PROJ_PERSPECTIVE = 1;

        public const int SPLIT_MODE_NONE = 0;
        public const int SPLIT_MODE_HORIZONTAL = 1;
        public const int SPLIT_MODE_VERTICAL = 2;

        public const string moduleName = "sccore";

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XRendererExVersion();

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetJavaVM(IntPtr jvm, int version);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int XRendererEx_AttachCurrentThread(ref IntPtr env);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_DetachCurrentThread();

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XRendererEx_GetContext(IntPtr renderer);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XRendererEx_GetCurrentThreadContext();

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XRendererEx_Create(IntPtr hwnd, IntPtr eglContext);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XRendererEx_GetUnityRenderEventFuncPointer();

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int XRendererEx_GetUnityContext(ref IntPtr eglContext);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_Destroy(IntPtr renderer);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_Resize(IntPtr renderer, int width, int height);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetProjectionMode(IntPtr renderer, int mode);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetOrthographic(IntPtr renderer, float size, float nearZ, float farZ);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_GetSceneViewportSize(IntPtr renderer, ref float width, ref float height);
        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetCameraPosition(IntPtr renderer, float x, float y, float z);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_GetCameraPosition(IntPtr renderer, ref float x, ref float y, ref float z);
        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetCameraRotation(IntPtr renderer, float pitch, float yaw);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XrendererEx_GetCameraRotation(IntPtr renderer, ref float pitch, ref float yaw);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_Clear(IntPtr renderer, float r, float g, float b, float a);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetPosition(IntPtr renderer, float x, float y, float z);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetRotation(IntPtr renderer, float axisX, float axisY, float axisZ, float angle);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetScaling(IntPtr renderer, float x, float y, float z);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetMixColor(IntPtr renderer, float r, float g, float b, float a);
        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetBehave(IntPtr renderer, int behave);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetModel(IntPtr renderer, int model);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetColorSpace(IntPtr renderer, ColorSpace space);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_SetSplitMode(IntPtr renderer, int mode);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_Draw(IntPtr renderer, IntPtr texture);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_Present(IntPtr renderer, int vsync);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int XRendererEx_RenderTarget(IntPtr renderer, ref IntPtr renderTarget);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XRendererEx_MapBitmapData(IntPtr renderer, ref int width, ref int height, ref int linesize, ref IntPtr data);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XRendererEx_UnmapBitmapData(IntPtr renderer, IntPtr resource);




        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XTextureEx_Create(IntPtr renderer);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XTextureEx_Destroy(IntPtr texture);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr XTextureEx_GetANativeWindow(IntPtr texture);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XTextureEx_Reset(IntPtr texture);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int XTextureEx_Update(IntPtr texture, int width, int height, int pixelFormat, int[] linesize, IntPtr[] data, IntPtr opaque);

        [DllImport(moduleName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ConvertColorSpace(int colorRange, int colorSpace);
    }
}
