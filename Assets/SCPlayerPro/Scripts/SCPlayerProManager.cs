using Sttplay.MediaPlayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCPlayerProContext
{
    public bool used;
    public SCPlayerPro player;
    public SCVideoRenderer renderer;
}
public class SCPlayerProManager
{
    private static List<SCPlayerProContext> contextList = new List<SCPlayerProContext>();
    public static SCPlayerProContext CreatePlayer()
    {
        bool needCreate = true;
        SCPlayerProContext context = null;
        foreach (var item in contextList)
        {
            if(!item.used)
            {
                context = item;
                needCreate = false;
                break;
            }
        }
        if(needCreate)
        {
            context = new SCPlayerProContext();
            context.player = new SCPlayerPro();
            context.renderer = new SCVideoRenderer();
            contextList.Add(context);
        }
        context.used = true;
        return context;
    }
    public static void ReleasePlayer(SCPlayerProContext context)
    {
        context.player.Close();
        context.renderer.TerminateRenderer();
        context.used = false;
    }

    public static void ReleaseAll()
    {
        foreach (var item in contextList)
        {
            item.player.Release();
            item.renderer.Dispose();
        }
        contextList.Clear();
    }
}
