using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ObjectProperties : MonoBehaviour
{
    public float height = 1.0f;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Handles.zTest = CompareFunction.LessEqual;
        Vector3 upPosition = transform.position + Vector3.up * height;
        Handles.DrawAAPolyLine(transform.position, upPosition);
        float size = HandleUtility.GetHandleSize(upPosition) * 0.3f;
        Gizmos.DrawSphere(upPosition, size);
    }
#endif

}
