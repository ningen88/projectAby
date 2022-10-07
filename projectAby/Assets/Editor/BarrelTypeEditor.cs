using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(BarrelType))]
public class BarrelTypeEditor : Editor
{
    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propColor;

    private void OnEnable()
    {
        so = serializedObject;
        propRadius = so.FindProperty("explosionRadius");
        propColor = so.FindProperty("barrelColor");
    }

    enum Etichette
    {
        ONE, TWO, THREE
    }

    float value;
    Etichette et;

    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();
        // EditorGUILayout
        // GUILayout

        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        EditorGUILayout.PropertyField(propColor);

        if (so.ApplyModifiedProperties())                          // return true if something changed   (UNDO problems)
        {
            BarrelManager.changeBarrelsColors();
        }

        GUILayout.Space(10);

        using(new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("test", EditorStyles.boldLabel);

            if (GUILayout.Button("test button"))
            {
                Debug.Log("You pressed a button");
            }
        }


        GUILayout.Space(10);

//        GUILayout.BeginHorizontal();                                                                             // OLD WAY (return problems)
        using(new GUILayout.HorizontalScope())
        {
            et = (Etichette)EditorGUILayout.EnumPopup(et, GUILayout.Width(100));
            value = GUILayout.HorizontalSlider(value, 0.0f, 1.0f); 
        }

        GUILayout.Space(10);
        EditorGUILayout.ObjectField(null, typeof(GameObject), true);

 //       GUILayout.EndHorizontal();
    }

}
