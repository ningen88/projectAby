using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public enum DrawType
{
    RANDOM,
    CIRCULAR,
    EQUIDISTANT,
    NATURAL
}

public class PlantTrees : EditorWindow
{

    [MenuItem("Tool/Plant")]
    public static void Plant()
    {
        GetWindow<PlantTrees>("Plant");                                         // setup window
    }

    // variable
    public float radius = 10.0f;
    public int objectCount;
    public GameObject obj;
    public DrawType drawType = DrawType.RANDOM;

    SerializedObject so;                              
    SerializedProperty radiusProp;
    SerializedProperty objectCountProp;
    SerializedProperty drawTypeProp;
    SerializedProperty objProp;

    RndPoint[] rndPoints;
    GameObject[] prefabs;
    public List<GameObject> selectedObjs = new List<GameObject>();                              // list of selectedObj
    [SerializeField] bool[] selectedObjState;                                                   // array that hold the state of obj (selected/not selected)



    public struct RndPoint 
    {
        public Vector2 rndPosition;
        public float rndAngle;
        public GameObject rndObj;
        public void RandomizeValue(List<GameObject> selectedPrefabs, float angleIncrement, DrawType drawType, float r)
        {
            if (drawType == DrawType.RANDOM)
            {
                rndPosition = Random.insideUnitCircle;
            }
            else if(drawType == DrawType.CIRCULAR)
            {
                rndPosition.x = Mathf.Cos(angleIncrement);
                rndPosition.y = Mathf.Sin(angleIncrement);
            }
            else if (drawType == DrawType.EQUIDISTANT)
            {
                rndPosition.x = r * Mathf.Cos(angleIncrement);
                rndPosition.y = r * Mathf.Sin(angleIncrement);
            }
            else if(drawType == DrawType.NATURAL)
            {
                float posX = r * Mathf.Cos(angleIncrement);
                float posY = r * Mathf.Sin(angleIncrement);
                float randomX = Random.Range(0.1f, 0);
                float randomY = Random.Range(0.1f, 0);

                if (posX > 0) rndPosition.x = posX - randomX;
                else rndPosition.x = posX + randomX;

                if (posY > 0) rndPosition.y = posY - randomY;
                else rndPosition.y = posY + randomY;
            }
            
            rndAngle = Random.Range(0, 361);
            if(selectedPrefabs.Count == 0)
            {
                rndObj = null;
            }
            else
            {
                int index = Random.Range(0, selectedPrefabs.Count);
                rndObj = selectedPrefabs[index];
            }
        }
    }

    public class SpawnPoint
    {
        public RndPoint spawnData;
        public Vector3 position;
        public Quaternion rotation;
        public bool isValid = false;

        public SpawnPoint(Vector3 pos, Quaternion rot, RndPoint sd)
        {
            this.position = pos;
            this.rotation = rot;
            this.spawnData = sd;

            if(spawnData.rndObj != null)
            {
                ObjectProperties objectProperties = spawnData.rndObj.GetComponent<ObjectProperties>();
                if (objectProperties == null)
                {
                    isValid = true;
                }
                else
                {
                    float h = objectProperties.height;
                    Vector3 up = rotation * Vector3.up;
                    Ray ray = new Ray(position, up);
                    if (Physics.Raycast(ray, h))
                    {
                        isValid = false;
                    }
                    else isValid = true;
                }
            } 
        }
    }



    private float CalculateRadius(int k, int n, int b)                              // k -> iteration, n -> number of points, b-> number of bondary points
    {
        if (k > n - b)
        {
            return 1.0f;                                                            // points on boundary
        }
        else
        {
            float num = Mathf.Sqrt(k - 0.5f);
            float den = Mathf.Sqrt(n-(b+1)/2);
            return num/ den;
        }
    }


    private void GenerateRandomPoints()                                            // generate an array of vector2 points
    {
        rndPoints = new RndPoint[objectCount];
        float angleIncrement = 0;        
        float r = 1;
        int b = (int)Mathf.Round(2*Mathf.Sqrt(objectCount));                                        // number of bondary points
        float phi = (Mathf.Sqrt(5)+1) / 2;                                                          // golden ratio

        for(int i = 0; i < objectCount; i++)
        {
            if(i == 0)
            {
                r = 0;
            }
            else
            {
                r = CalculateRadius(i, objectCount, b);
            }
            
            angleIncrement = (2 * Mathf.PI) * i / (phi * phi);
            rndPoints[i].RandomizeValue(selectedObjs, angleIncrement, drawType, r);
        }
    }


    private void OnEnable()
    {
        so = new SerializedObject(this);
        radiusProp = so.FindProperty("radius");
        objectCountProp = so.FindProperty("objectCount");
        objProp = so.FindProperty("obj");
        drawTypeProp = so.FindProperty("drawType");

        // load prefabs
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();                                   // carefull when loading too many prefabs
        if(selectedObjState == null || selectedObjState.Length != prefabs.Length )
        {
            selectedObjState = new bool[prefabs.Length];                                                              // setup the length of selectedObjState array
        }

        GenerateRandomPoints();
        Repaint();
        SceneView.duringSceneGui += DuringSceneGUI;
        
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void InstantiateObj(List<SpawnPoint> points)
    {
        if (points.Count == 0) return;

        foreach (SpawnPoint point in points)
        {
            if (point.isValid == false) 
                continue;

            GameObject spawnObj = (GameObject)PrefabUtility.InstantiatePrefab(point.spawnData.rndObj);
            spawnObj.transform.position = point.position;
            spawnObj.transform.rotation = point.rotation;
            Undo.RegisterCreatedObjectUndo(spawnObj, "spawn object");
        }
        GenerateRandomPoints();                                                                                                      // update points
    }

    void DrawGiz(RaycastHit hitInfo, Vector3 normal, Vector3 tangent, Vector3 bitangent)
    {
        // draw normal, tangent and bitangent.
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(4, hitInfo.point, hitInfo.point + normal);

        Handles.color = Color.red;
        Handles.DrawAAPolyLine(4, hitInfo.point, hitInfo.point + bitangent);

        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(4, hitInfo.point, hitInfo.point + tangent);
        Handles.color = Color.white;

        // Draw a disc that adapt with the surface
        const int circleDetail = 64;
        Vector3[] circlePoints = new Vector3[circleDetail];
        const float TAU = 6.28318530718f;


        for (int i = 0; i < circleDetail; i++)
        {
            float t = i / ((float)circleDetail - 1);
            float angRad = t * TAU;
            Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
            Vector3 pos = hitInfo.point + (bitangent * dir.x + tangent * dir.y) * radius;
            pos += hitInfo.normal * 10;
            Ray circleRay = new Ray(pos, -normal);

            if (Physics.Raycast(circleRay, out RaycastHit hitC))
            {
                circlePoints[i] = hitC.point + hitC.normal * 0.02f;
            }
            else
            {
                circlePoints[i] = circleRay.origin;
            }
        }

        Handles.DrawAAPolyLine(circlePoints);
    }

    void DrawPreview(SpawnPoint point, RaycastHit hitPt, Camera cam)
    {
        if (point.spawnData.rndObj != null && point.isValid)
        {
            MeshFilter[] filters = point.spawnData.rndObj.GetComponentsInChildren<MeshFilter>();
            Matrix4x4 poseToWorld = Matrix4x4.TRS(point.position, point.rotation, Vector3.one);

            foreach (MeshFilter filter in filters)
            {
                Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
                Matrix4x4 childToWorld = poseToWorld * childToPose;
                Mesh mesh = filter.sharedMesh;                                                                         // get mesh from the object
                Material material = filter.GetComponent<MeshRenderer>().sharedMaterial;                                // get the material from the object
                Graphics.DrawMesh(mesh, childToWorld, material, 0, cam);
                //                                material.SetPass(0);                                                 // set the material as the actual material
                //                                Graphics.DrawMeshNow(mesh, childToWorld);                            // use an immediate drawing function
            }

        }
        // Draw point and normal when there are no objects selected
        else
        {
            Handles.SphereHandleCap(-1, hitPt.point, Quaternion.identity, 0.1f, EventType.Repaint);                   // and draw a point in that position
            Handles.DrawAAPolyLine(2, hitPt.point, hitPt.point + hitPt.normal);                                       // and the normal
        }

    }

    void DisplayOnScreenGUI()
    {
        Rect recT = new Rect(8, 8, 50, 50);

        for (int i = 0; i < prefabs.Length; i++)
        {
            Texture icon = AssetPreview.GetAssetPreview(prefabs[i]);
            EditorGUI.BeginChangeCheck();
            selectedObjState[i] = GUI.Toggle(recT, selectedObjState[i], new GUIContent(icon));                       // assign the state to each toggle
            if (EditorGUI.EndChangeCheck())                                                                          // execute this code only when there are changes in GUI.Toggle
            {
                selectedObjs.Clear();                                                                                 // brute force clear of selectedObjs list (alternative use Remove in some way)
                for (int j = 0; j < prefabs.Length; j++)                                                              // loop through every prefabs
                {
                    if (selectedObjState[j])                                                                         // if one is selected, add that one to the selectedObjs list
                    {
                        selectedObjs.Add(prefabs[j]);
                    }
                }
                GenerateRandomPoints();
            }
            recT.y += recT.height + 2;
        }
    }



    void DuringSceneGUI(SceneView view)
    {
        Handles.zTest = CompareFunction.LessEqual;
        Camera cam = view.camera;

        if (Event.current.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();                                                               // we repaint when we move the mouse for a better smooth feeling
        }

        bool holdingCTRL = (Event.current.modifiers & EventModifiers.Control) != 0;
        bool holdingShift = (Event.current.modifiers & EventModifiers.Shift) != 0;

        if (Event.current.type == EventType.ScrollWheel && holdingCTRL)                          // only change radius with mouse wheel when holding ctrl
        {
            float scrollDir = Mathf.Sign(Event.current.delta.y);                                // delta of the scrollwheel (how much you are scrolling)
            so.Update();
            radiusProp.floatValue *= 1f + scrollDir * 0.05f;
            so.ApplyModifiedProperties();
            Repaint();                                                                          // update editor window
            Event.current.Use();
        }

        // Camera ray
        Transform camTf = cam.transform;

        // Mouse cursor ray
        Ray r = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        List<SpawnPoint> objInfo = new List<SpawnPoint>();                                                                    // is a list of pose (position, rotation)
        
        if (Physics.Raycast(r, out RaycastHit hitInfo))                                                           
        {
            Vector3 normal = hitInfo.normal;                                                                      // this is the normal in the point of contact
            Vector3 bitangent = Vector3.Cross(normal, camTf.up).normalized;                                       
            Vector3 tangent = Vector3.Cross(normal, bitangent);                                                   // calculate the tanget with the cross prodoct with normal and bitangent
                                                                                                                   

            // LOCAL FUNCTION
            Ray GetTangentRay(Vector2 tangentSpecePos)
            {
                Vector3 pos = hitInfo.point + (bitangent * tangentSpecePos.x + tangent * tangentSpecePos.y) * radius;
                pos += hitInfo.normal * 10;                                                                                  // offset margin (hard coded!)
                return new Ray(pos, -normal);
            }
            // END OF LOCAL FUNCTION
            
            foreach (RndPoint p in rndPoints)                                                                      // calculate the position of the random points from tangent space to world space
            {
                
                Ray ptRay = GetTangentRay(p.rndPosition);

                if(Physics.Raycast(ptRay, out RaycastHit hitPt))
                {                   
                    Quaternion randQ = Quaternion.Euler(0f,0f, p.rndAngle);
                    Quaternion rot =  Quaternion.LookRotation(hitPt.normal) * randQ * Quaternion.Euler(90f, 0f, 0f);
                    SpawnPoint point = new SpawnPoint(hitPt.point, rot, p);
                    objInfo.Add(point);

                    // call a function that handle meshes preview
                    DrawPreview(point, hitPt, cam);
                }
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && holdingShift)
            {
                InstantiateObj(objInfo);
            }

            // Draw normal, tangent, bitangent and the disc
            DrawGiz(hitInfo, normal, tangent, bitangent);

        }

        // On screen GUI
        Handles.BeginGUI();
        DisplayOnScreenGUI();
        Handles.EndGUI();

    }

    private void OnGUI()
    {
        so.Update();

        GUILayout.Label("Hold ctrl + mouse wheel to control radius");
        GUILayout.Label("Hold shift + left mouse click to spawn objects");
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(radiusProp);        
        radiusProp.floatValue = Mathf.Max(10.0f, radiusProp.floatValue);

        EditorGUILayout.Space(5);
//        EditorGUILayout.PropertyField(objProp);
        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(objectCountProp);
        objectCountProp.intValue = Mathf.Max(1, objectCountProp.intValue);

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(drawTypeProp);

        
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }

        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);                                                               // this deselect property field when you click with left mouse button
            Repaint();
        }
    }
}
