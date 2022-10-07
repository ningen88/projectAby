using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Wally : EditorWindow
{
    [MenuItem("Tool/Wally")]
    public static void Walls()
    {
        GetWindow<Wally>("Wally");
    }

    public struct Point
    {
        public Vector3 position;
    }

    // variable
    public float height = 1.0f;                                      // the height of the wall
    public Material material;


    // Serialized variable
    SerializedObject so;
    SerializedProperty heightProp;
    SerializedProperty materialProp;

    // private variable
    private Point[] points;                                          // hold start position and end position


    private void OnEnable()
    {
        so = new SerializedObject(this);
        heightProp = so.FindProperty("height");
        materialProp = so.FindProperty("material");
        points = new Point[2];
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }


    void DrawPreview(RaycastHit hit)
    {
        if (points[0].position != null && points[0].position != Vector3.zero)
        {
            Handles.DrawAAPolyLine(points[0].position, hit.point);
        }
    }

    void CreateWall(Vector3 start, Vector3 end)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        float x = Mathf.Pow(end.x - start.x, 2);
        float z = Mathf.Pow(end.z - start.z, 2);
        float d = Mathf.Sqrt(x + z);
        wall.transform.localScale = new Vector3(d, height, 1);
        wall.transform.right = end - start;
        float xd = (end.x - start.x) * 0.5f;
        float zd = (end.z - start.z) * 0.5f;
        wall.transform.position = new Vector3(start.x + xd, start.y + wall.transform.localScale.y * 0.5f, start.z + zd);

        if(material != null)
        {
            wall.GetComponent<MeshRenderer>().sharedMaterial = material;
        }


        Undo.RegisterCreatedObjectUndo(wall, "wall");       
    }


    void DuringSceneGUI(SceneView view)
    {
        bool holdingCTRL = (Event.current.modifiers & EventModifiers.Control) != 0;
        bool holdingShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
        if (Event.current.type == EventType.ScrollWheel && holdingCTRL)
        {
            float scrollDir = Mathf.Sign(Event.current.delta.y);
            so.Update();
            heightProp.floatValue -= scrollDir*0.5f;
            so.ApplyModifiedProperties();
            Repaint();
            Event.current.Use();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && holdingCTRL)
            {
                points[0].position = hit.point;
            }
            if(Event.current.type == EventType.MouseDown && Event.current.button == 1 && holdingShift)
            {
                points[1].position = hit.point;
                CreateWall(points[0].position, points[1].position);
                points[0].position = points[1].position;
            }
            DrawPreview(hit);                       
        }
    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(heightProp);
        heightProp.floatValue = Mathf.Max(1.0f, heightProp.floatValue);
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(materialProp); 
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Hold ctrl + left mouse click to begin a wall sequence,");
        EditorGUILayout.LabelField("then hold shift + right mouse click to create a wall.");


        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
    }
}
