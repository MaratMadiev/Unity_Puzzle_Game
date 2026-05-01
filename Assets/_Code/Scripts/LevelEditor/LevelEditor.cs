using System;
using System.Collections.Generic;
using UnityEngine;
using static RoadSection;

[RequireComponent(typeof(GameManager))]
public class LevelEditor : MonoBehaviour
{
    int currentLevel = 0;
    EditorMode type = EditorMode.Straight;
    RoadType slopeType = RoadType.Flat;

    GameManager gm;

    [SerializeField]
    Camera cam;
    [SerializeField]
    Collider levelCollider;

    [SerializeField]
    GameObject roadPrefab;
    [SerializeField]
    Mesh snappingPointIndicator;
    [SerializeField]
    Material indicatorMaterial;
    [SerializeField]
    Material indicatorMaterialWrong;
    [SerializeField]
    Material roadSnapMaterial;
    

    AbstractDrawingState drawingState;

    SnapPoints snapPoints;
    Mesh snapPointsMesh = null;

    public Camera Cam { get => cam; }
    public GameObject RoadPrefab { get => roadPrefab; }
    public SnapPoints SnapPoints { get => snapPoints; }
    public int CurrentLevel
    {
        get => currentLevel;
        set
        {
            if (drawingState != null && !drawingState.IsCurrentlyDrawing)
            {
                currentLevel = Math.Clamp(value, 0, GameRules.MaxLevel);
            }
        }
    }
    public Mesh SnappingPointIndicator { get => snappingPointIndicator; private set => snappingPointIndicator = value; }
    public Material IndicatorMaterial { get => indicatorMaterial; private set => indicatorMaterial = value; }
    public Material IndicatorMaterialWrong { get => indicatorMaterialWrong; private set => indicatorMaterialWrong = value; }
    public RoadType SlopeType
    {
        get => slopeType; private set
        {
            if (value == RoadType.Upward && currentLevel == GameRules.MaxLevel) return;
            if (value == RoadType.Downward && currentLevel == 0) return;
            slopeType = value;
        }
    }

    void Start()
    {
        snapPointsMesh = BuildSnapPointMesh();


        gm = GetComponent<GameManager>();
        snapPoints = new SnapPoints();
        if (gm.Nodes != null) snapPoints.RecalculateFully(gm);

        ChangeDrawingState(EditorMode.Curve);

    }
    public void OnAdd()
    {
        SnapPoints.RecalculateFully(gm);
        gm.UpdateGatewayRoads(SnapPoints);
        gm.OnChange();
    }

    public void OnDelete()
    {
        SnapPoints.RecalculateFully(gm);
        gm.UpdateGatewayRoads(SnapPoints);
        gm.OnChange();
    }

    private Mesh BuildSnapPointMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "SnapPointCircle";

        int segments = 24;
        float radius = RoadMeshSection.RoadWidth / 2;
        float offset = RoadMeshSection.RoadOffset;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        var ySlightly = 0.05f;
        var ySide = -0.7f;

        vertices.Add(new Vector3(0, ySlightly, 0));
        uv.Add(new Vector2(0.75f, 0.5f));

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;

            float x = Mathf.Cos(angle);
            float z = -Mathf.Sin(angle);

            vertices.Add(new Vector3(x * (radius), 0, z * (radius)));
            vertices.Add(new Vector3(x * (radius), 0, z * (radius)));
            vertices.Add(new Vector3(x * (radius + offset), -ySlightly, z * (radius + offset)));
            vertices.Add(new Vector3(x * (radius + offset), -ySlightly, z * (radius + offset)));
            vertices.Add(new Vector3(x * (radius + offset), ySide, z * (radius + offset)));


            uv.Add(new Vector2(x * 0.25f + 0.75f, z * 0.5f + 0.5f));
            uv.Add(new Vector2(0.0f, angle * (radius + offset)));
            uv.Add(new Vector2(0.15f, angle * (radius + offset)));
            uv.Add(new Vector2(0.15f, angle * (radius + offset)));
            uv.Add(new Vector2(0.3f, angle * (radius + offset)));
        }

        for (int i = 0; i < segments; i++)
        {
            int center = 0;
            int curr = i * 5 + 1;
            int next = (i + 1) % segments * 5 + 1;

            triangles.Add(center);
            triangles.Add(curr);
            triangles.Add(next);

            triangles.Add(curr + 1);
            triangles.Add(curr + 2);
            triangles.Add(next + 2);

            triangles.Add(next + 1);
            triangles.Add(curr + 1);
            triangles.Add(next + 2);

            triangles.Add(curr + 3);
            triangles.Add(curr + 4);
            triangles.Add(next + 4);

            triangles.Add(next + 3);
            triangles.Add(curr + 3);
            triangles.Add(next + 4);
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    void ChangeDrawingState(EditorMode newCurveType)
    {
        type = newCurveType;

        drawingState?.Exit();
        switch (newCurveType)
        {
            case EditorMode.Straight:
                drawingState = new StraightDrawingState(gm, this);
                break;
            case EditorMode.Curve:
                drawingState = new CurveDrawingState(gm, this);
                break;
            case EditorMode.Delete:
                drawingState = new DeleteDrawingState(gm, this);
                break;
            default:
                drawingState = new IdleDrawingState(gm, this);
                break;
        }
        drawingState.Enter();
    }

    [ContextMenu("snaps")]
    void LogSnaps()
    {
        Debug.Log("=== ОТЛАДКА SNAPPOINTS ===");
        foreach (var kvp in SnapPoints.Dict)
        {
            Debug.Log($"Key: ({kvp.Key.xz.x:F8}, {kvp.Key.xz.y:F8}) lvl={kvp.Key.level} hash={kvp.Key.GetHashCode()}");
            Debug.Log($"  Incoming: {string.Join(",", kvp.Value.IncomingRoads)}");
            Debug.Log($"  Outcoming: {string.Join(",", kvp.Value.OutcomingRoads)}");
        }
    }

    void Update()
    {
        levelCollider.transform.position = new Vector3(0, currentLevel * GameRules.LevelHeight, 0);
        drawingState.Update();

        DrawAllSnapPoints();

        TestChange();
    }

    private void TestChange()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ChangeDrawingState(EditorMode.None);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ChangeDrawingState(EditorMode.Delete);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeDrawingState(EditorMode.Straight);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeDrawingState(EditorMode.Curve);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            CurrentLevel = (currentLevel + 1) % (GameRules.MaxLevel + 1);
            Debug.Log("level changed: " + currentLevel);
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SlopeType = (RoadType)Math.Clamp((int)(slopeType - 1), 0, 2);
            Debug.Log("slopeType changed: " + slopeType);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SlopeType = (RoadType)Math.Clamp((int)(slopeType + 1), 0, 2);
            Debug.Log("slopeType changed: " + slopeType);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            GetComponent<SimulationCarManager>().StartSimulating();
        }

    }

    private void DrawAllSnapPoints()
    {
        foreach (var snapPoint in snapPoints.Dict.Keys)
        {
            Graphics.DrawMesh(snapPointsMesh,
                snapPoint.xz.ToVector3XZ() + new Vector3(0, snapPoint.level * GameRules.LevelHeight, 0),
                Quaternion.identity, roadSnapMaterial, 0);
        }
    }
}

public enum EditorMode
{
    None,
    Straight,
    Curve,
    Delete
}
