using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DrawFunctions : MonoBehaviour
{
    [SerializeField] Material lineMaterial;
    [SerializeField] Material circleMaterial;
    [SerializeField] CombatMenuManager combatMenuManager;
    [SerializeField] TMP_Text endGameText;

    private void Start()
    {
        DrawBorder();
    }

    public void DrawLine(Vector3 startPos, Vector3 endPos, Material material, Color startColor, Color endColor, float startWidth, float endWidth, string tag)
    {
        GameObject line = new GameObject();
        line.tag = tag;
        line.transform.position = startPos;
        line.AddComponent<LineRenderer>();
        LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
        lineRenderer.material = material;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    private void DrawBorder()
    {
        //lineUp
        Vector3 startUp = new Vector3(-10.0f, 0.01f, 5.0f);
        Vector3 endUp = new Vector3(10.0f, 0.01f, 5.0f);
        DrawLine(startUp, endUp, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "borderLine");

        //lineDown
        Vector3 startDown = new Vector3(-10.0f, 0.01f, -5.0f);
        Vector3 endDown = new Vector3(10.0f, 0.01f, -5.0f);
        DrawLine(startDown, endDown, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "borderLine");

        //lineLeft
        Vector3 startLeft = new Vector3(-10.0f, 0.01f, 5.0f);
        Vector3 endLeft = new Vector3(-10.0f, 0.01f, -5.0f);
        DrawLine(startLeft, endLeft, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "borderLine");

        //lineRight
        Vector3 startRight = new Vector3(10.0f, 0.01f, 5.0f);
        Vector3 endRight = new Vector3(10.0f, 0.01f, -5.0f);
        DrawLine(startRight, endRight, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "borderLine");
    }

    public void DrawCell(Vector3 origin)
    {
        float margin = 0.03f;                                                                                  // a little margin between cells

        //lineUP
        Vector3 startUp = new Vector3(origin.x - 0.5f + margin, 0.01f, origin.z + 0.5f - margin);
        Vector3 endUp = new Vector3(origin.x + 0.5f - margin, 0.01f, origin.z + 0.5f - margin);
        DrawLine(startUp, endUp, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "line");

        //LineDown
        Vector3 startDown = new Vector3(origin.x - 0.5f + margin, 0.01f, origin.z - 0.5f + margin);
        Vector3 endDown = new Vector3(origin.x + 0.5f - margin, 0.01f, origin.z - 0.5f + margin);
        DrawLine(startDown, endDown, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "line");

        //LineRight
        Vector3 startRight = new Vector3(origin.x + 0.5f - margin, 0.01f, origin.z + 0.5f - margin);
        Vector3 endRight = new Vector3(origin.x + 0.5f - margin, 0.01f, origin.z - 0.5f + margin);
        DrawLine(startRight, endRight, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "line");

        //LineLeft
        Vector3 startLeft = new Vector3(origin.x - 0.5f + margin, 0.01f, origin.z + 0.5f - margin);
        Vector3 endLeft = new Vector3(origin.x - 0.5f + margin, 0.01f, origin.z - 0.5f + margin);
        DrawLine(startLeft, endLeft, lineMaterial, Color.cyan, Color.cyan, 0.03f, 0.03f, "line");
    }

    public void DrawCircle(Vector3 origin, float radius)
    {
        int steps = 64;
        Vector3[] points = new Vector3[steps + 1];
        GameObject line = new GameObject();
        line.tag = "line";
        line.AddComponent<LineRenderer>();
        LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
        lineRenderer.positionCount = steps;
        lineRenderer.material = circleMaterial;
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.startWidth = 0.03f;
        lineRenderer.endWidth = 0.03f;

        for (int i = 0; i < steps; i++)
        {
            float circPercentage = i / ((float)steps - 1);
            float radians = circPercentage * Mathf.PI * 2;
            float x = origin.x + Mathf.Cos(radians) * radius;
            float z = origin.z + Mathf.Sin(radians) * radius;
            points[i] = new Vector3(x, 0.01f, z);
        }

        lineRenderer.SetPositions(points);
    }

    public void ShowWinMessage()
    {
        combatMenuManager.ActivateEndGameMsg();
        endGameText.SetText("You win");
    }

    public void ShowLooseMessage()
    {
        combatMenuManager.ActivateEndGameMsg();
        endGameText.SetText("You Loose");
    }

}
