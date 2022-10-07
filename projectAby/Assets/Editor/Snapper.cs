using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public static class Snapper
{
    [MenuItem("/Snapper/Snap selected object %&S")]                                    //% => ctrl & => alt (shortcut ctrl+alt+S)
    public static void Snap()
    {
        const string UNDO_STR = "snap objects";
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, UNDO_STR);
            go.transform.position = go.transform.position.Round();                     // call extension method (implemented in static class)
        }
    }

    public static Vector3 Round(this Vector3 v)                                // extension method
    {
        v.x = Mathf.Round(v.x);
        v.y = Mathf.Round(v.y);
        v.z = Mathf.Round(v.z);

        return v;
    }

    public static Vector3 Round(this Vector3 v, float size)
    {
        return (v / size).Round() * size;
    }

    public static float Round(this float v, float size)
    {
        return Mathf.Round(v/size) * size;
    }
}
