using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RoadSection))]
public class RoadMeshSection : RoadMeshBase
{

    RoadSection node = null;

    private RoadSection Node
    {
        get
        {
            if (node == null) node = GetComponent<RoadSection>();
            return node;
        }
        set => node = value;
    }



    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        node = GetComponent<RoadSection>();

        GenerateRoadMesh();
    }

    public void GenerateRoadMesh()
    {

        if (Node == null || Node.Curve == null) return;

        UpdateMesh(Node.Curve, Node.Level, Node.Type);

    }
}
