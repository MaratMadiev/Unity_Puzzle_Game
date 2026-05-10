using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RoadMeshSection))]
[RequireComponent(typeof(MeshFilter))]
public class RoadSection : MonoBehaviour
{
    QuadBezier2 curve = null;
    int level = 0;
    int id;
    RoadType type = RoadType.Flat;

    public QuadBezier2 Curve
    {
        get { return curve; }
    }

    public void Initialize(QuadBezier2 curve, int level, RoadType type, int id)
    {
        this.curve = curve;
        this.level = level;
        this.type = type;
        this.id = id;
        var roadMesh = GetComponent<RoadMeshSection>();
        roadMesh.GenerateRoadMesh();
        gameObject.layer = LayerMask.NameToLayer("roadDeletable");
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
    }


    public int Level
    {
        get { return level; }
    }
    public RoadType Type
    {
        get { return type; }
    }

    public int Id { get => id; }

    void Start()
    {
        GraphNode sa = new(this, 0); 
    }

    void Update()
    {

    }

    public enum RoadType
    {
        Flat, Upward, Downward
    }
}
