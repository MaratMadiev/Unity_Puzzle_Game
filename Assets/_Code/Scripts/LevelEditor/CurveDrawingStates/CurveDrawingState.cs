using static SnapPoints;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RoadSection;


// я искренне сочувствую тем, кто это будет читать
// TODO: переделать всё с паттерном State
public class CurveDrawingState : AbstractDrawingState
{
    int levelLayerMask;

    Vector3 firstPoint;
    SnapPoint firstSnapPoint = null;
    Vector3 anchorPoint;

    ProgressState state = ProgressState.Idle;

    bool firstSnaped = false;
    private Mesh mesh;
    private Mesh mesh1;
    private Mesh mesh2;

    public override bool IsCurrentlyDrawing => state != ProgressState.Idle;

    public CurveDrawingState(GameManager gm, LevelEditor editor) : base(gm, editor)
    {
        levelLayerMask = 1 << LayerMask.NameToLayer("levelCollider");
    }

    public override void Update()
    {
        Ray ray = editor.Cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int currentLevel = editor.CurrentLevel;
        int endLevel = currentLevel;
        if (editor.SlopeType == RoadType.Upward) ++endLevel;
        if (editor.SlopeType == RoadType.Downward) --endLevel;

        int snappingPointsTargetLevel = state == ProgressState.Idle ? currentLevel : endLevel;
        var snappingPoints = editor.SnapPoints.Dict.Values.Where(kvp => kvp.Key.level == snappingPointsTargetLevel);

        bool currentSnaped = false;
        SnapPoint currentSnapPoint = null;

        DrawSnapPointGuideMeshes(snappingPoints, snappingPointsTargetLevel);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, levelLayerMask))
        {
            Vector3 hitPoint = hit.point;

            CorrectHitPoint(snappingPoints, ref currentSnaped, ref currentSnapPoint, ref hitPoint);
            if (currentSnaped)
            {
                Graphics.DrawMesh(editor.SnappingPointIndicator,
                    currentSnapPoint.Key.xz.ToVector3XZ() + new Vector3(0, snappingPointsTargetLevel * GameRules.LevelHeight, 0),
                    Quaternion.identity, editor.IndicatorMaterial, 0);
            }

            bool isRoadValid = CheckCurve(hitPoint);

            RoadValidationResult validationResult = new();
            if (state == ProgressState.AnchorPicked)
            {
                validationResult = ValidateRoad(
                    new QuadBezier2(firstPoint.ToVector2FromXZ(),
                    anchorPoint.ToVector2FromXZ(),
                    hitPoint.ToVector2FromXZ()),
                    editor.SlopeType, currentLevel);

                isRoadValid &= validationResult.isValid;
            }

            DrawGuideMeshes(currentLevel, endLevel, hitPoint, isRoadValid);

            if (Input.GetMouseButtonDown(0) && isRoadValid)
            {
                ProcessLMB(currentSnaped, currentSnapPoint, hitPoint, validationResult);
            }
            if (Input.GetMouseButtonDown(1))
            {
                Cancel();
            }
        }
    }

    private bool CheckCurve(Vector3 hitPoint)
    {
        if (state == ProgressState.AnchorPicked)
        {
            var firstBeam = anchorPoint - firstPoint;
            var secondBeam = anchorPoint - hitPoint;

            if (editor.SlopeType == RoadType.Flat)
            {
                return firstBeam.magnitude + secondBeam.magnitude > GameRules.MinStraightLength
                    && Vector3.Angle(firstBeam, secondBeam) > GameRules.MinAngleFlat(firstBeam.magnitude * 2);
            }
            else
            {
                return firstBeam.magnitude + secondBeam.magnitude > GameRules.MinSlopeLength
                    && Vector3.Angle(firstBeam, secondBeam) > GameRules.MinAngleSlope(firstBeam.magnitude * 2);
            }
        }
        if (state == ProgressState.FirstPicked)
        {
            if (editor.SlopeType == RoadType.Flat)
            {
                return (hitPoint - firstPoint).magnitude > GameRules.MinStraightLength;
            }
            else
            {
                return (hitPoint - firstPoint).magnitude > GameRules.MinSlopeLength;
            }
        }
        return true;
    }

    private void DrawSnapPointGuideMeshes(IEnumerable<SnapPoint> snappingPoints, int snappingPointsTargetLevel)
    {

        foreach (var point in snappingPoints)
        {
            Graphics.DrawMesh(editor.SnappingPointIndicator,
                point.Key.xz.ToVector3XZ() + new Vector3(0, snappingPointsTargetLevel * GameRules.LevelHeight, 0),
                Quaternion.identity, editor.IndicatorMaterial, 0);
        }

    }

    private void Cancel()
    {
        state = ProgressState.Idle;
        firstSnaped = false;
        

    }

    private void ProcessLMB(bool currentSnaped, SnapPoint currentSnapPoint, Vector3 hitPoint, RoadValidationResult valRes)
    {

        if (state == ProgressState.Idle)
        {
            firstPoint = hitPoint;
            state = ProgressState.FirstPicked;
            if (currentSnaped)
            {
                firstSnapPoint = currentSnapPoint;
                firstSnaped = true;
            }
        }
        else if (state == ProgressState.FirstPicked)
        {
            anchorPoint = (firstPoint + hitPoint) / 2;
            state = ProgressState.AnchorPicked;
        }
        else
        {

            if (valRes.hasIntersection)
            {

                var nodeToRemove = gm.Nodes[valRes.intersectionId];
                var curveReplace = nodeToRemove.Road.Curve;
                var curvereplSplit = curveReplace.SplitAt(valRes.t);

                var roadToReplace1 = Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();
                var roadToReplace2 = Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();

                var graphNode1 = gm.Add(roadToReplace1);
                var graphNode2 = gm.Add(roadToReplace2);

                roadToReplace1.Initialize(curvereplSplit.left, editor.CurrentLevel, RoadType.Flat, graphNode1.Id);
                roadToReplace2.Initialize(curvereplSplit.right, editor.CurrentLevel, RoadType.Flat, graphNode2.Id);



                gm.AddConnection(graphNode1.Id, graphNode2.Id);

                var removedNodePrevs = nodeToRemove.PrevNodes;
                var removedNodeNexts = nodeToRemove.NextNodes;

                foreach (var pr in removedNodePrevs)
                {
                    gm.AddConnection(pr, graphNode1.Id);
                }

                foreach (var next in removedNodeNexts)
                {
                    gm.AddConnection(graphNode2.Id, next);
                }

                var curvesSplitted = new QuadBezier2(firstPoint.ToVector2FromXZ(), anchorPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ()).SplitAt(valRes.s);
                var roadThis1 = Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();
                var graphNodeThis1 = gm.Add(roadThis1);

                roadThis1.Initialize(curvesSplitted.left, editor.CurrentLevel, RoadType.Flat, graphNodeThis1.Id);

                gm.AddConnection(graphNodeThis1.Id, graphNode2.Id);
                gm.RemoveById(valRes.intersectionId);

                if (firstSnaped)
                {
                    foreach (var roadId in firstSnapPoint.IncomingRoads)
                    {
                        gm.AddConnection(roadId, graphNodeThis1.Id);
                    }
                }

                if (curvereplSplit.right.PointC == curvesSplitted.left.PointA)
                {
                    gm.AddConnection(graphNode2.Id, graphNodeThis1.Id);
                }



                if (curvesSplitted.right.Length > GameRules.MinStraightLength)
                {
                    var roadThis2 = Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();
                    var graphNodeThis2 = gm.Add(roadThis2);
                    roadThis2.Initialize(curvesSplitted.right, editor.CurrentLevel, RoadType.Flat, graphNodeThis2.Id);

                    gm.AddConnection(graphNodeThis1.Id, graphNodeThis2.Id);
                    gm.AddConnection(graphNode1.Id, graphNodeThis2.Id);

                    if (currentSnaped)
                    {
                        foreach (var roadId in currentSnapPoint.OutcomingRoads)
                        {
                            gm.AddConnection(graphNodeThis2.Id, roadId);
                        }
                    }

                    if (curvesSplitted.right.PointC == curvereplSplit.left.PointA)
                    {
                        gm.AddConnection(graphNodeThis2.Id, graphNode1.Id);
                    }

                }

            }
            else
            {
                var road = Object.Instantiate(editor.RoadPrefab);

                var roadSection = road.GetComponent<RoadSection>();
                var graphNode = gm.Add(roadSection);
                roadSection.Initialize(new(firstPoint.ToVector2FromXZ(), anchorPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ()), editor.CurrentLevel, editor.SlopeType, graphNode.Id);

                if (firstSnaped)
                {
                    foreach (var roadId in firstSnapPoint.IncomingRoads)
                    {
                        gm.AddConnection(roadId, graphNode.Id);
                    }
                }

                if (currentSnaped)
                {
                    foreach (var roadId in currentSnapPoint.OutcomingRoads)
                    {
                        gm.AddConnection(graphNode.Id, roadId);
                    }
                }
            }
            editor.OnAdd();
            state = ProgressState.Idle;
            firstSnaped = false;
        }

    }

    private void DrawGuideMeshes(int currentLevel, int endLevel, Vector3 hitPoint, bool isValid)
    {
        Material material = isValid ? editor.IndicatorMaterial : editor.IndicatorMaterialWrong;
        if (state == ProgressState.AnchorPicked)
        {
            QuadBezier2 curve = new QuadBezier2(firstPoint.ToVector2FromXZ(), anchorPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ());
            GenerateGuideMeshCurve(ref mesh, curve, editor.SlopeType, currentLevel, 1f);
            GenerateGuideMeshStraight(ref mesh1, firstPoint.ToVector2FromXZ(), anchorPoint.ToVector2FromXZ(), RoadType.Flat, currentLevel, 0.5f);
            GenerateGuideMeshStraight(ref mesh2, anchorPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ(), editor.SlopeType, currentLevel, 0.5f);
            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
            Graphics.DrawMesh(mesh2, Vector3.zero, Quaternion.identity, material, 0);
            Graphics.DrawMesh(mesh1, Vector3.zero, Quaternion.identity, material, 0);
        }
        else if (state == ProgressState.FirstPicked)
        {
            QuadBezier2 curve = new QuadBezier2(firstPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ());
            GenerateGuideMeshCurve(ref mesh, curve, editor.SlopeType, currentLevel, 1f);
            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
        }

    }

    private void CorrectHitPoint(IEnumerable<SnapPoint> snappingPoints, ref bool currentSnaped, ref SnapPoint currentSnapPoint, ref Vector3 hitPoint)
    {
        if (state == ProgressState.FirstPicked) return;
        if (state == ProgressState.AnchorPicked)
        {
            var len = (anchorPoint - firstPoint).magnitude;
            if (snappingPoints.Count() > 0)
            {
                var nearest = FindNearestSnapPoint(snappingPoints, hitPoint);
                var nearestDist = (nearest.Item1.Key.xz - anchorPoint.ToVector2FromXZ()).magnitude;
                float threshold = GameRules.SnappingVecThreshold;

                bool areSnapPointsTheSame = firstSnaped && nearest.Item1.Key.Equals(firstSnapPoint.Key);

                if (nearest.Item2 < GameRules.SnappingVecThreshold
                    && len - threshold <= nearestDist
                    && nearestDist <= len + threshold
                    && !areSnapPointsTheSame)
                {
                    hitPoint.x = nearest.Item1.Key.xz.x;
                    hitPoint.z = nearest.Item1.Key.xz.y;

                    currentSnapPoint = nearest.Item1;
                    currentSnaped = true;
                    return;
                }
            }
            var ancgToHit = hitPoint - anchorPoint;
            ancgToHit.y = 0;
            hitPoint = anchorPoint + ancgToHit.normalized * len;

            Debug.DrawLine(firstPoint, anchorPoint);
            Debug.DrawLine(hitPoint, anchorPoint);
        }
        else
        {
            if (snappingPoints.Count() > 0)
            {
                var nearest = FindNearestSnapPoint(snappingPoints, hitPoint);
                if (nearest.Item2 < GameRules.SnappingVecThreshold)
                {
                    hitPoint.x = nearest.Item1.Key.xz.x;
                    hitPoint.z = nearest.Item1.Key.xz.y;

                    currentSnapPoint = nearest.Item1;
                    currentSnaped = true;
                }
            }
        }
    }

    private static (SnapPoint, float) FindNearestSnapPoint(IEnumerable<SnapPoint> snappingPoints, Vector3 hitPoint)
    {
        var nearest = snappingPoints.First();
        float minDist = float.MaxValue;
        foreach (var point in snappingPoints)
        {
            if (Vector2.Distance(point.Key.xz, hitPoint.ToVector2FromXZ()) < minDist)
            {
                nearest = point;
                minDist = Vector2.Distance(point.Key.xz, hitPoint.ToVector2FromXZ());
            }
        }

        return (nearest, minDist);
    }

    public override void Enter()
    {
        mesh = new();
        mesh1 = new();
        mesh2 = new();

        mesh.MarkDynamic();
        mesh1.MarkDynamic();
        mesh2.MarkDynamic();
    }

    public override void Exit()
    {
        UnityEngine.Object.Destroy(mesh);
        UnityEngine.Object.Destroy(mesh1);
        UnityEngine.Object.Destroy(mesh2);
    }

    enum ProgressState
    {
        Idle,
        FirstPicked,
        AnchorPicked,
    }
}