using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AI;


public class CombatMap : EditorWindow
{
    [MenuItem("Tool/CombatMap")]
    public static void CMap() 
    {
        GetWindow<CombatMap>("CombatMap");
    }


    SerializedObject so;
    SerializedProperty terrainProp;

    public GameObject terrain;

    private GameObject[] prefabs;
    private GridMap gridMap;
    private string savedMaskPath;

    private void OnEnable()
    {
        savedMaskPath = Application.dataPath + "/StreamingAssets/obstaclesPosition.json";
        so = new SerializedObject(this);
        terrainProp = so.FindProperty("terrain");
        

        // load assets
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] {"Assets/Prefabs/CombatMapAssets"});
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void OnValidate()
    {
        //get gridMap
        if (terrain != null)
        {
            gridMap = terrain.GetComponent<GridMap>();
        }
    }

    private void DuringSceneGUI(SceneView view) 
    {
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
        }

        bool holdingShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            // get world mouse position
            float x0 = hit.point.x;                                        
            float z0 = hit.point.z;
            float x = 0;
            float z = 0;

            // adjust position to fit into the grid
            if (x0 > 0 && z0 > 0)       
            {
                x = (int)x0 + 0.5f;
                z = (int)z0 + 0.5f;
            }
            if (x0 > 0 && z0 < 0)
            {
                x = (int)x0 + 0.5f;
                z = (int)z0 - 0.5f;
            }
            if (x0 < 0 && z0 < 0)
            {
                x = (int)x0 - 0.5f;
                z = (int)z0 - 0.5f;

            }
            if (x0 < 0 && z0 > 0)
            {
                x = (int)x0 - 0.5f;
                z = (int)z0 + 0.5f;
            }


            // spawn obj on click
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Debug.Log("click");
                /*                Obstacles obs = new Obstacles();
                                obs.xPos = x;
                                obs.zPos = z;
                                string jString = JsonUtility.ToJson(obs);
                                File.AppendAllText(savedMaskPath,jString);
                                File.AppendAllText(savedMaskPath, "\n");
                                int index = Random.Range(0, 2);
                                GameObject spawnObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);
                                GridMap.objectToRemove.Add(spawnObj);
                                spawnObj.transform.position = new Vector3(x, 0.0f, z);

                                NavMeshBuilder.BuildNavMesh();                                                                 // rebuild navmesh
                                Undo.RegisterCreatedObjectUndo(spawnObj, "created object");
                */
                Event.current.Use();
            }
        }           
    }
   

    private void GenerateBattlefield()
    {
        if (terrain == null) return;
        (float, float)[,] matrix = gridMap.GetGrid();                      // get all the possible position on the combat map
        int[,] mask = gridMap.getMask();                                   // get the mask that rapresent the obstacle positions

        int width = gridMap.getWidth();
        int height = gridMap.getHeight();

        // create physical obstacle on the map
        Obstacles obs = new Obstacles();                                  // create a new obstacle object
        Obstacles obs1 = new Obstacles();                                 // 2 more obstacles for double cell obstacle
        Obstacles obs2 = new Obstacles();

        for (int i = 0; i < height; i++)
        {
            for(int j = 0; j < width; j++)
            {
                if(mask[i,j] == 1)
                {
                    float x = matrix[i, j].Item1;
                    float y = matrix[i, j].Item2;
                    obs.xPos = x;                                                               
                    obs.zPos = y;
                    string jString = JsonUtility.ToJson(obs); 
                    File.AppendAllText(savedMaskPath, jString);
                    File.AppendAllText(savedMaskPath, "\n");
                    int index = Random.Range(0, 3);
                    GameObject spawnObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);
                    GridMap.objectToRemove.Add(spawnObj);
                    spawnObj.transform.position = new Vector3(x, 0.0f, y);
                    Undo.RegisterCreatedObjectUndo(spawnObj, "created object");
                }
                if (mask[i, j] == 2) 
                {
                    float x1 = matrix[i, j].Item1;
                    float y1 = matrix[i, j].Item2;
                    float x2 = x1;
                    float y2 = y1 - 1.0f;
                    obs1.xPos = x1;
                    obs1.zPos = y1;
                    obs2.xPos = x2;
                    obs2.zPos = y2;
                    string jString1 = JsonUtility.ToJson(obs1);
                    File.AppendAllText(savedMaskPath, jString1);
                    File.AppendAllText(savedMaskPath, "\n");
                    string jString2 = JsonUtility.ToJson(obs2);
                    File.AppendAllText(savedMaskPath, jString2);
                    File.AppendAllText(savedMaskPath, "\n");
                    int index = Random.Range(6,8);
                    GameObject spawnObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);
                    GridMap.objectToRemove.Add(spawnObj);
                    spawnObj.transform.position = new Vector3(x1, 0.0f, y1 - 0.5f);
                    Undo.RegisterCreatedObjectUndo(spawnObj, "created object");
                }
                else
                {
                    // create cosmetic (3%)
                    int isCosmetic = Random.Range(1, 101);
                    if (isCosmetic <= 3)
                    {
                        float x = Random.Range(-15.0f, 15.0f);
                        float y = Random.Range(-5.0f, 15.0f);
                        int index = Random.Range(3, 6);
                        GameObject spawnObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);
                        GridMap.objectToRemove.Add(spawnObj);
                        spawnObj.transform.position = new Vector3(x, 0.0f, y);
                        Undo.RegisterCreatedObjectUndo(spawnObj, "created object");
                    }
                }
            }
        }

        NavMeshBuilder.BuildNavMesh();                                                                       // rebuild navmesh 
    }

    void ResetSavedData(string path)
    {
        if (File.Exists(path))
        {
            // there are no object in the list (returning from start)
            if(GridMap.objectToRemove.Count == 0)
            {
                GameObject[] obsInScene = GameObject.FindGameObjectsWithTag("obstacle");
                GameObject[] cosInScene = GameObject.FindGameObjectsWithTag("cosmetic");

                foreach(GameObject obj in obsInScene)
                {
                    DestroyImmediate(obj);
                }
                foreach(GameObject obj in cosInScene)
                {
                    DestroyImmediate(obj);
                }
            }
            else
            {
                foreach (GameObject obj in GridMap.objectToRemove)
                {
                    DestroyImmediate(obj);
                }
                GridMap.objectToRemove.Clear();
            }
            
            File.WriteAllText(path, "");                                                                    // delete previous data from mask json file
            NavMeshBuilder.BuildNavMesh();                                                                  // rebuild navmesh
        }
    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(terrainProp);

        if (GUILayout.Button("Generate"))
        {
            GenerateBattlefield();
        }
        if (GUILayout.Button("ResetMask"))
        {
            ResetSavedData(savedMaskPath);
        }


        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
    }

}
