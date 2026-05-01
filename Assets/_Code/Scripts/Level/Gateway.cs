using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RoadMeshGateway))]
public class Gateway : MonoBehaviour
{
    public int Id { get; private set; }
    public GatewayType Type { get; private set; }
    public QuadBezier2 Curve { get; private set; }

    public int Intensity { get; private set; }

    public void Initialize(int id, GatewayType type, QuadBezier2 curve, int intensity)
    {
        this.Id = id;
        this.Type = type;
        this.Curve = curve;
        this.Intensity = intensity;

        var gatewayMesh = GetComponent<RoadMeshGateway>();
        gatewayMesh.GenerateRoadMesh();
    }
}

public enum GatewayType
{
    Start,
    Finish
}
