using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public enum GridType
{
    Cartesian,
    Polar,
    Local
}

public class SnapperTool : EditorWindow
{
    private const float TAU = 6.28318530718f;
    private float snapX;
    private float snapY;
    private float snapZ;
    private Vector3 snapPosition;
    private Vector3 scale;

    public float gridSize = 1.0f;
    public int angularDiv = 24;
    public GridType gridType = GridType.Cartesian;
    SerializedObject so;
    SerializedProperty gridSizeProp;
    SerializedProperty gridTypeProp;
    SerializedProperty angularDivProp;

    [MenuItem("Tool/Snapper")]
    public static void Snap()
    {
        GetWindow<SnapperTool>("Snap");
    }


    private void OnEnable()
    {
        so = new SerializedObject(this);
        gridSizeProp = so.FindProperty("gridSize");
        gridTypeProp = so.FindProperty("gridType");
        angularDivProp = so.FindProperty("angularDiv");

        // load status
        gridSize = EditorPrefs.GetFloat("ST_GRID_SIZE", 1.0f);
        angularDiv = EditorPrefs.GetInt("ST_ANGULAR_DIVISION", 24);
        gridType = (GridType)EditorPrefs.GetInt("ST_GRID_TYPE", 0);

        Selection.selectionChanged += Repaint;                        // repaint the button when a changed in selection occured (fix button don't repaint before we hover with mouse)
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        // save status
        EditorPrefs.SetFloat("ST_GRID_SIZE", gridSize);
        EditorPrefs.SetInt("ST_ANGULAR_DIVISION", angularDiv);
        EditorPrefs.SetInt("ST_GRID_TYPE", (int)gridType);

        Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        if (Event.current.type == EventType.Repaint)
        {
            const float gridCountExtent = 16;
            Handles.zTest = CompareFunction.LessEqual;

            if (gridType == GridType.Cartesian)
            {
                DrawGrid(gridCountExtent);
            }
            else if (gridType == GridType.Local)
            {
                Vector3 center = Selection.activeTransform.position;
                DrawLocalGrid(center);
            }
            else if(gridType == GridType.Polar)
            {
                DrawPolarGrid(gridCountExtent);
            }
        }                       
    }

    void DrawPolarGrid(float gridCountExtent)
    {
        int ringCount = Mathf.RoundToInt(gridCountExtent/gridSize);

        // drawing rings
        for(int i = 1; i < ringCount; i++)
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, gridSize * i);
        }

        // drawing lines
        for(int i = 0; i < angularDiv; i++)
        {
            float t = i / (float)angularDiv;
            float angRad = t * TAU;

            float x = Mathf.Cos(angRad);
            float z = Mathf.Sin(angRad);

            Vector3 dir = new Vector3(x, 0, z);                                   // direction
            float distance = gridSize * (ringCount -1);

            Handles.DrawAAPolyLine(Vector3.zero, distance*dir);
        }
        
    }

    void DrawGrid(float gridCountExtent)
    {
        
        
        int lineCount = Mathf.RoundToInt((gridCountExtent * 2) / gridSize);

        if (lineCount % 2 == 0) lineCount++;

        int halfLineCount = lineCount / 2;


        for(int i = 0; i < lineCount; i++)
        {
            int offset = i - halfLineCount;
            float xCoord = offset * gridSize;
            float zCoord0 = halfLineCount * gridSize;
            float zCoord1 = -halfLineCount * gridSize;

            Vector3 p0 = new Vector3(xCoord, 0, zCoord0);
            Vector3 p1 = new Vector3(xCoord, 0, zCoord1);
            Handles.DrawAAPolyLine(p0, p1);

            p0 = new Vector3(zCoord0, 0, xCoord );
            p1 = new Vector3(zCoord1, 0, xCoord );
            Handles.DrawAAPolyLine(p0, p1);
        }
    }

    void DrawLocalGrid(Vector3 center)
    {
        float radius = scale.x * 0.5f;
        Vector3 rightPos = new Vector3(radius, 0, 0);
        Vector3 forwardPos = new Vector3(0, 0, radius);
        Vector3 leftUp = new Vector3(-radius, 0, radius);
        Vector3 rightUp = new Vector3(radius, 0, radius);
        Vector3 leftDown = new Vector3(-radius, 0, -radius);
        Vector3 rightDown = new Vector3(radius, 0, -radius);

        // DRAW OF THE CENTRAL CROSS
        Handles.DrawLine(center + rightPos, center - rightPos);                       
        Handles.DrawLine(center - forwardPos, center + forwardPos);


        // DRAW OF THE OTHER LINES
        Handles.DrawLine(center + leftUp, center + rightUp);
        Handles.DrawLine(center + leftDown, center + rightDown);
        Handles.DrawLine(center + leftUp, center + leftDown);      
        Handles.DrawLine(center + rightUp, center + rightDown);

    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(gridTypeProp);
        so.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (gridType == GridType.Cartesian)
        {
            so.Update();
            EditorGUILayout.PropertyField(gridSizeProp);
            so.ApplyModifiedProperties();
        }
        else if(gridType == GridType.Local)
        {

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Snap X:");
                snapX = EditorGUILayout.FloatField(snapX);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Snap Y:");
                snapY = EditorGUILayout.FloatField(snapY);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Snap Z:");
                snapZ = EditorGUILayout.FloatField(snapZ);
            }

            CalculateSnap();
        }
        else if(gridType == GridType.Polar)
        {
            so.Update();
            EditorGUILayout.PropertyField(gridSizeProp);
            EditorGUILayout.PropertyField(angularDivProp);
            angularDivProp.intValue = Mathf.Max(4, angularDivProp.intValue);
            so.ApplyModifiedProperties();
        }

        EditorGUILayout.Space(10);
        
        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))                             // disable the button if no object is selected
        {
            if (GUILayout.Button("Snap"))
            {
                SnapSelection();
            }
        }
    }

    void SnapSelection()
    {
        const string UNDO_STR = "snap objects";
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, UNDO_STR);
            if (gridType == GridType.Cartesian)
            {
                go.transform.position = go.transform.position.Round(gridSize);
            }
            else if(gridType == GridType.Local)
            {
                go.transform.position = snapPosition;
            }
            else if(gridType == GridType.Polar)
            {
                go.transform.position = polarSnap(go.transform.position);
            }          
        }
    }

    Vector3 polarSnap(Vector3 originalPos)
    {
        // convert original position to polar
        Vector2 v = new Vector2(originalPos.x, originalPos.z);
        float distance = v.magnitude;                                                   // distance from center
        float distanceSnap = distance.Round(gridSize);                                  // distance from center snapped

        float angle = Mathf.Atan2(v.y, v.x);                                            // 0-TAU
        float angleTurns = angle / TAU;                                                 // 0-1
        float angleSnap = (Mathf.Round(angleTurns * angularDiv) / angularDiv);          // angle snapped
        float angleRadSnap = angleSnap * TAU;

        float x = distance * Mathf.Cos(angleRadSnap);
        float z = distance * Mathf.Sin(angleRadSnap);
        Vector3 snapedPosition = new Vector3(x, originalPos.y, z);
        return snapedPosition;
    }

    void CalculateSnap()                                                       // WORK FOR ONLY 1 BARREL
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Vector3 pos = go.transform.position;
            float x = Mathf.Round(pos.x - snapX);
            float y = Mathf.Round(pos.y - snapY);
            float z = Mathf.Round(pos.z - snapZ);

            scale = go.transform.localScale;

            snapPosition.x = x;
            snapPosition.y = y;
            snapPosition.z = z;
        }
    }
}
