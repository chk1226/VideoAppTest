using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotate gameobject
/// </summary>
public class RotateSelf : MonoBehaviour
{
    public float speed = 1.0f;
    public Vector3 rotateAxis = Vector3.forward;
    void Update()
    {
        transform.Rotate(rotateAxis * Time.deltaTime * speed);
    }
}
