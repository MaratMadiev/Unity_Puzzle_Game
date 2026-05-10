using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static RoadSection;
using static SnapPoints;

public class StraightDrawingState : AbstractDrawingState
{
    int levelLayerMask;

    Vector3 firstPoint;
    SnapPoint firstSnapPoint = null;

    bool firstPickedUp = false; // SSoT
    bool firstSnaped = false; // SSoT
    private Mesh mesh;

    public override bool IsCurrentlyDrawing => firstPickedUp;

    public StraightDrawingState(GameManager gm, LevelEditor editor) : base(gm, editor)
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

        int snappingPointsTargetLevel = firstPickedUp ? endLevel : currentLevel;
        var snappingPoints = editor.SnapPoints.Dict.Values.Where(kvp => kvp.Key.level == snappingPointsTargetLevel);

        bool currentSnaped = false;
        SnapPoint currentSnapPoint = null;

        DrawSnappingPointGuideMeshes(snappingPointsTargetLevel, snappingPoints);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, levelLayerMask))
        {
            Vector3 hitPoint = hit.point;

            CorrectHitPoint(snappingPoints, ref currentSnaped, ref currentSnapPoint, ref hitPoint);

            bool isRoadValid = CheckCurve(hitPoint);
            RoadValidationResult validationResult = new();
            if (firstPickedUp)
            {
                validationResult = ValidateRoad(
                    new QuadBezier2(firstPoint.ToVector2FromXZ(),
                    hitPoint.ToVector2FromXZ()),
                    editor.SlopeType, currentLevel);

                isRoadValid &= validationResult.isValid;
            }

            DrawGuideMeshes(currentLevel, hitPoint, isRoadValid);

            if (currentSnaped)
            {
                Graphics.DrawMesh(editor.SnappingPointIndicator,
                    currentSnapPoint.Key.xz.ToVector3XZ() + new Vector3(0, snappingPointsTargetLevel * GameRules.LevelHeight, 0),
                    Quaternion.identity, editor.IndicatorMaterial, 0);
            }

            if (isRoadValid && Input.GetMouseButtonDown(0))
            {
                if (!EventSystem.current.IsPointerOverGameObject()) ProcessLMB(currentSnaped, currentSnapPoint, hitPoint, validationResult);
            }
            if (Input.GetMouseButtonDown(1))
            {
                CancelRoad();
            }
        }

    }

    private bool CheckCurve(Vector3 hitPoint)
    {
        if (!firstPickedUp) return true;

        if (editor.SlopeType == RoadType.Flat)
        {
            return (hitPoint - firstPoint).magnitude > GameRules.MinStraightLength;
        }
        else
        {
            return (hitPoint - firstPoint).magnitude > GameRules.MinSlopeLength;
        }
    }

    private void DrawSnappingPointGuideMeshes(int snappingPointsTargetLevel, IEnumerable<SnapPoint> snappingPoints)
    {
        foreach (var point in snappingPoints)
        {
            Graphics.DrawMesh(editor.SnappingPointIndicator,
                point.Key.xz.ToVector3XZ() + new Vector3(0, snappingPointsTargetLevel * GameRules.LevelHeight, 0),
                Quaternion.identity, editor.IndicatorMaterial, 0);
        }
    }

    private void CancelRoad()
    {
        firstPickedUp = false;
        firstSnaped = false;
    }

    private void ProcessLMB(bool currentSnaped, SnapPoint currentSnapPoint, Vector3 hitPoint, RoadValidationResult valRes)
    {
        if (!firstPickedUp)
        {
            firstPoint = hitPoint;
            firstPickedUp = true;
            if (currentSnaped)
            {
                firstSnapPoint = currentSnapPoint;
                firstSnaped = true;
            }
        }
        else
        {
            if (valRes.hasIntersection)
            {
                var nodeToRemove = gm.Nodes[valRes.intersectionId];
                var curveReplace = nodeToRemove.Road.Curve;
                var curvereplSplit = curveReplace.SplitAt(valRes.t);

                var roadToReplace1 = UnityEngine.Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();
                var roadToReplace2 = UnityEngine.Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();

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

                var curvesSplitted = new QuadBezier2(firstPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ()).SplitAt(valRes.s);
                var roadThis1 = UnityEngine.Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();

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

                if (curvesSplitted.right.Length > GameRules.MinStraightLength)
                {
                    var roadThis2 = UnityEngine.Object.Instantiate(editor.RoadPrefab).GetComponent<RoadSection>();
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

                }
            }
            else
            {
                var road = UnityEngine.Object.Instantiate(editor.RoadPrefab);
                var roadSection = road.GetComponent<RoadSection>();
                var graphNode = gm.Add(roadSection);
                roadSection.Initialize(new(firstPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ()), editor.CurrentLevel, editor.SlopeType, graphNode.Id);

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

            firstPickedUp = false;
            firstSnaped = false;
        }

    }

    private void DrawGuideMeshes(int currentLevel, Vector3 hitPoint, bool isValid)
    {
        if (firstPickedUp)
        {
            Material material = isValid ? editor.IndicatorMaterial : editor.IndicatorMaterialWrong;
            QuadBezier2 curve = new QuadBezier2(firstPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ());
            GenerateGuideMeshStraight(ref mesh, firstPoint.ToVector2FromXZ(), hitPoint.ToVector2FromXZ(), editor.SlopeType, currentLevel, 1f);
            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
        }
    }

    private void CorrectHitPoint(IEnumerable<SnapPoint> snappingPoints, ref bool currentSnaped, ref SnapPoint currentSnapPoint, ref Vector3 hitPoint)
    {
        if (snappingPoints.Count() > 0)
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
            bool areSnapPointsTheSame = firstSnaped && nearest.Key.Equals(firstSnapPoint.Key);

            if (minDist < GameRules.SnappingVecThreshold && !areSnapPointsTheSame)
            {
                hitPoint.x = nearest.Key.xz.x;
                hitPoint.z = nearest.Key.xz.y;

                currentSnapPoint = nearest;
                currentSnaped = true;
            }
        }
    }

    public override void Enter()
    {
        mesh = new();
        mesh.MarkDynamic();
    }

    public override void Exit()
    {
        UnityEngine.Object.Destroy(mesh);
    }
}

