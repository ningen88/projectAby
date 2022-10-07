using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class RandomGround : EditorWindow
{
    [MenuItem("Tool/RandomGround")]
    public static void RGround()
    {
        GetWindow<RandomGround>("RandomGround");
    }


    SerializedObject so;
    SerializedProperty terrainProp;

    public Terrain terrain;
    Texture2D[] diffuseTextures;
    Texture2D[] normalMaps;


    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
        so = new SerializedObject(this);
        terrainProp = so.FindProperty("terrain");

        // load diffuse textures
        string[] guidsDiff = AssetDatabase.FindAssets("t:texture2D", new[] {"Assets/UsedTextures/Albedo"});
        IEnumerable<string> pathsDiff = guidsDiff.Select(AssetDatabase.GUIDToAssetPath);
        diffuseTextures = pathsDiff.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();

        // load normal map
        string[] guidsNMap = AssetDatabase.FindAssets("t:texture2D", new[] {"Assets/UsedTextures/NormalMaps"});
        IEnumerable<string> pathsNMap = guidsNMap.Select(AssetDatabase.GUIDToAssetPath);
        normalMaps = pathsNMap.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void DuringSceneGUI(SceneView view)
    {
//        terrain.terrainData.terrainLayers[0].diffuseTexture
    }


    private TerrainLayer SetTextureLayer(Texture2D diffuse, Texture2D nMap, float tilingSize)
    {
        TerrainLayer layer = new TerrainLayer();
        layer.diffuseTexture = diffuse;
        layer.normalMapTexture = nMap;
        layer.tileOffset = Vector2.zero;
        layer.tileSize = Vector2.one * tilingSize;
        return layer;
    }

    private void SetRandomTexture()
    {
        if (terrain == null) return;

        int index = Random.Range(0, 4);
        TerrainLayer newLayer = SetTextureLayer(diffuseTextures[index], normalMaps[index], 5.0f);
        TerrainLayer[] layers = terrain.terrainData.terrainLayers;

        if (newLayer == null) return;                                     
        for(int i = 0; i < layers.Length; i++)                                  // if the newLayer is already in layers return
        {
            if (layers[i] == newLayer) return;
        }

        var newLayersArray = new TerrainLayer[layers.Length + 1];
        System.Array.Copy(layers, 0, newLayersArray, 0, layers.Length);
        newLayersArray[layers.Length] = newLayer;
        terrain.terrainData.terrainLayers = newLayersArray;
        AssetDatabase.AddObjectToAsset(terrain.terrainData.terrainLayers[0],"Assets");
        terrain.Flush();

        //       System.Array.Copy();
        //       terrainLayers[0].diffuseTexture = diffuseTextures[0];
        //       terrainLayers[0].normalMapTexture = normalMaps[0];
        //       terrain.terrainData.terrainLayers = terrainLayers;

        
//        terrain.terrainData.SetTerrainLayersRegisterUndo(terrainLayers, "layers");

//        terrain.terrainData.terrainLayers[1].diffuseTexture = diffuseTextures[0];
//        terrain.terrainData.terrainLayers[1].normalMapTexture = normalMaps[0];
    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.LabelField("Terrain");
        EditorGUILayout.PropertyField(terrainProp);
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Generate"))
        {
            SetRandomTexture();
        }

        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }

    }

}
