using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class BarrelManager : MonoBehaviour
{

    public static List<Barrel> barrelList = new List<Barrel>();


    public static void changeBarrelsColors()
    {
        foreach(Barrel b in barrelList)
        {
            b.ApplyColor();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        foreach(Barrel b in barrelList)
        {

            if (b.barrelType == null) continue;

            Vector3 managerPos = transform.position;
            Vector3 barrelPos = b.transform.position;
            float halfPos = (managerPos.y - barrelPos.y) / 2;
            Vector3 tangentOff = Vector3.up * halfPos;
            Vector3 startTangent = managerPos - tangentOff;
            Vector3 endTangent = barrelPos + tangentOff;

            Handles.zTest = CompareFunction.LessEqual;
            Handles.DrawBezier(managerPos, barrelPos, startTangent, endTangent, Color.white, EditorGUIUtility.whiteTexture, 1.0f);
            
//            Gizmos.DrawLine(transform.position, b.transform.position);

            
        }
    }
#endif


}
