using System;
using System.Collections.Generic;
using UnityEngine;
using static RoadSection;

public abstract class AbstractDrawingState
{
    protected bool shouldSnapTheAngles = false;
    protected GameManager gm;
    protected LevelEditor editor;
    public abstract bool IsCurrentlyDrawing { get; }

    public AbstractDrawingState(GameManager gm, LevelEditor editor)
    {
        this.gm = gm;
        this.editor = editor;
    }

    public abstract void Update();
    public abstract void Enter();
    public abstract void Exit();


    protected RoadValidationResult ValidateRoad(QuadBezier2 curve, RoadType roadType, int level)
    {
        RoadValidationResult res = new();
        res.isValid = true;

        int maxIntersections = 1; //пока обрабатываем только одно пересечение;
        int correctIntersections = 0;

        foreach (var graphNode in gm.Nodes)
        {
            var graphCurve = graphNode.Value.Road.Curve;
            var intersects = graphCurve.GetIntersectionPoints(curve);
            int levelDiff = level - graphNode.Value.Road.Level;

            if (Math.Abs(levelDiff) > 1) continue;

            if (intersects.Count > 1)
            {
                res.isValid = false;
                return res;
            }
            if (intersects.Count == 1)
            {
                bool allFlat = roadType == RoadType.Flat && graphNode.Value.Road.Type == RoadType.Flat;

                if (correctIntersections >= maxIntersections || (levelDiff == 0 && !allFlat))
                {
                    res.isValid = false;
                    return res;
                }

                if (levelDiff == 1 && (graphNode.Value.Road.Type == RoadType.Upward || roadType == RoadType.Downward)
                    || levelDiff == -1 && (graphNode.Value.Road.Type == RoadType.Downward || roadType == RoadType.Upward))
                {
                    res.isValid = false;
                    return res;
                }

                if (Math.Abs(levelDiff) == 1 && allFlat)
                {
                    continue;
                }

                var graphCurveSplit = graphNode.Value.Road.Curve.SplitAt(intersects[0].t);
                var curveSplit = curve.SplitAt(intersects[0].s);


                if (graphCurveSplit.left.Length < GameRules.MinStraightLength
                    || graphCurveSplit.right.Length < GameRules.MinStraightLength
                    || curveSplit.left.Length < GameRules.MinStraightLength)
                {
                    res.isValid = false;
                    return res;
                }

                res.isValid = true;
                res.hasIntersection = true;
                res.intersectionPoint = intersects[0].point;
                res.t = intersects[0].t;
                res.s = intersects[0].s;
                res.intersectionId = graphNode.Key;
                correctIntersections++;
            }
        }
        return res;
    }

    public static void GenerateGuideMeshStraight(ref Mesh mesh, Vector2 point1xz, Vector2 point2xz, RoadType roadType, int level, float width)
    {
        GenerateGuideMeshCurve(ref mesh, new(point1xz, point2xz), roadType, level, width);
    }

    public static void GenerateGuideMeshCurve(ref Mesh mesh, QuadBezier2 curve, RoadType roadType, int level, float width)
    {
        mesh.Clear();
        const float segmentLength = 1;
        mesh.name = "curve";


        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        List<Vector2> points2D = curve.GetLerpPointsLen(segmentLength);

        for (int i = 0; i < points2D.Count; i++)
        {
            var tangent = curve.GetTangent(i * 1f / (points2D.Count - 1)).ToVector3XZ();
            var toSide = new Vector3(-tangent.z, 0, tangent.x);

            Vector3 yOffset = new Vector3(0, level * GameRules.LevelHeight, 0);
            Vector3 centerYoffset = new Vector3(0, 0.5f, 0);

            if (roadType == RoadType.Upward)
            {
                yOffset += new Vector3(0, GameRules.LevelHeight * GameRules.UpFunction(i * 1f / (points2D.Count - 1)), 0);
            }
            else if (roadType == RoadType.Downward)
            {
                yOffset += new Vector3(0, -GameRules.LevelHeight * GameRules.UpFunction(i * 1f / (points2D.Count - 1)), 0);
            }


            vertices.Add(points2D[i].ToVector3XZ() + yOffset - toSide * width);
            vertices.Add(points2D[i].ToVector3XZ() + yOffset + centerYoffset);
            vertices.Add(points2D[i].ToVector3XZ() + yOffset + toSide * width);

            uv.Add(new(0, i));
            uv.Add(new(0.5f, i));
            uv.Add(new(1, i));
        }

        for (int i = 0; i < points2D.Count - 1; i++)
        {
            int ind = i * 3;
            int indNext = ind + 3;

            triangles.Add(ind);
            triangles.Add(ind + 1);
            triangles.Add(indNext);

            triangles.Add(ind + 1);
            triangles.Add(indNext + 1);
            triangles.Add(indNext);

            triangles.Add(ind + 1);
            triangles.Add(ind + 2);
            triangles.Add(indNext + 1);

            triangles.Add(ind + 2);
            triangles.Add(indNext + 2);
            triangles.Add(indNext + 1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

    }

    public struct RoadValidationResult
    {
        public bool isValid;
        public bool hasIntersection;
        public int intersectionId;
        public float t;
        public float s;
        public Vector2 intersectionPoint;

        public RoadValidationResult(bool isValid, bool hasIntersection, int intersectionId, float thisIntersect, float otherIntersect, Vector2 vector)
        {
            this.isValid = isValid;
            this.hasIntersection = hasIntersection;
            this.intersectionId = intersectionId;
            this.t = thisIntersect;
            this.s = otherIntersect;
            this.intersectionPoint = vector;
        }
    }
}
