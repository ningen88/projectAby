using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BarrelType : ScriptableObject
{
    [Range(1.0f, 4.0f)]
    public float explosionRadius = 1.0f;
    public Color barrelColor;
}
