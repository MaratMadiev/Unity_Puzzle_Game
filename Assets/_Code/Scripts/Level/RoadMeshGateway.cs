using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Gateway))]
public class RoadMeshGateway : RoadMeshBase
{

    Gateway node = null;

    private Gateway Node
    {
        get
        {
            if (node == null) node = GetComponent<Gateway>();
            return node;
        }
        set => node = value;
    }



    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        node = GetComponent<Gateway>();

    }

    public void GenerateRoadMesh()
    {

        if (Node == null || Node.Curve == null) return;
        UpdateMesh(Node.Curve, 0, RoadSection.RoadType.Flat);
    }
}
