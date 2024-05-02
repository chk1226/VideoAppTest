using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// An extension to Slider in UnityEngine 
/// </summary>
public class SliderPro : Slider
{
    public bool IsPress { get; private set; }
    public SliderEvent onSliderRelease;
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        IsPress = true;
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        IsPress = false;
        if (onSliderRelease != null)
            onSliderRelease.Invoke(value);
    }
}
