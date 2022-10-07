using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


//Execute always call all the function also during the editor

[ExecuteAlways]
public class Barrel : MonoBehaviour
{
    //    [Range(1.0f,4.0f)]
    //    public float explosionRadius = 1.0f;
    //    public Color barrelColor;
    public BarrelType barrelType;

    MaterialPropertyBlock mpb;                                                 // doesn't duplicate
    static readonly int shPropColor = Shader.PropertyToID("_Color");

    MaterialPropertyBlock Mpb
    {
        get
        {
            if(mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
            return mpb;
        }
    }

    public void ApplyColor()
    {
        if (barrelType == null) return;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mpb.SetColor(shPropColor, barrelType.barrelColor);
        mr.SetPropertyBlock(Mpb);
    }

    private void OnValidate()
    {
        ApplyColor();
    }

    private void OnEnable()
    {
        ApplyColor();
        BarrelManager.barrelList.Add(this);
    }

    private void OnDisable()
    {
        BarrelManager.barrelList.Remove(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (barrelType == null) return;

//        Handles.color = barrelColor;
        Handles.DrawWireDisc(transform.position, transform.up, barrelType.explosionRadius);
//        Handles.color = Color.white;
    }
#endif
}
