using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class Car : MonoBehaviour
{
    List<CarPathData> path;
    int currentCurve = 0;
    float totalDist = 0;
    float currentCurveDist = 0;
    float currentSpeed = 0;
    List<(float, float)> speedControlPoints;
    public UnityEvent onPathEndCallback = new();

    public Car BlockedBy { get; private set; }

    void Start()
    {

    }

    public void Initialize(List<CarPathData> path, GameObject carModelprefab)
    {
        this.path = path;
        totalDist = 0;
        currentCurveDist = 0;
        currentSpeed = 5;

        currentCurve = 0;


        var modelObj = Instantiate(carModelprefab);
        modelObj.transform.SetParent(transform);
        modelObj.transform.position += transform.position;

        CalculateSpeedPoints();
        gameObject.layer = LayerMask.NameToLayer("car");
        transform.position = path[0].curve.PointA.ToVector3XZ(0);
        Physics.SyncTransforms();
    }

    private void CalculateSpeedPoints()
    {
        speedControlPoints = new();
        float totalLength = 0;
        foreach (var path in path)
        {
            if (path.curve.Length < 5f)
            {
                speedControlPoints.Add((totalLength, GameRules.CurvatureToMaxSpeed(path.curve.GetCurvature(0.5f))));
            }
            else
            {
                speedControlPoints.Add((totalLength, GameRules.CurvatureToMaxSpeed(path.curve.GetCurvature(0))));
                const float cpLength = 7f;
                float localLength = cpLength;
                while (true)
                {
                    if (localLength + cpLength > path.curve.Length) break;
                    float accumulatedLength = totalLength + localLength;
                    speedControlPoints.Add((accumulatedLength, GameRules.CurvatureToMaxSpeed(path.curve.GetCurvature(localLength / path.curve.Length))));
                    localLength += cpLength;
                }
            }
            totalLength += path.curve.Length;
        }
    }

    public void Emulate(float timeDelta)
    {
        if (gameObject == null) return;

        float rayCastDist = CalculateMinDistanceToOtherCars();

        (float, float) cp = FindNextControlPoint();
        float targetSpeed = cp.Item2;
        targetSpeed = UpdateTargetSpeed(targetSpeed, rayCastDist);

        if (currentSpeed > targetSpeed) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, GameRules.CarDeccelerate * timeDelta);
        else currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, GameRules.CarAccelerate * timeDelta);

        if (rayCastDist < 6f)
        {
            bool isDeadlock = BlockedBy != null && BlockedBy.BlockedBy == this;
            bool hasPriority = isDeadlock &&
                Vector3.Angle(transform.forward, BlockedBy.transform.forward) < 120f &&
                GetInstanceID() < BlockedBy.GetInstanceID();

            if (!hasPriority) currentSpeed = 0;
        }
        ;

        totalDist += currentSpeed * timeDelta;
        currentCurveDist += currentSpeed * timeDelta;

        if (currentCurveDist > path[currentCurve].curve.Length)
        {
            if (currentCurve == path.Count - 1)
            {
                onPathEndCallback.Invoke();
                Destroy(gameObject);
                return;
            }
            currentCurveDist -= path[currentCurve].curve.Length;
            ++currentCurve;
        }

        float coef = currentCurveDist / path[currentCurve].curve.Length;
        coef = Mathf.Clamp01(coef);
        var currentPoint = path[currentCurve].curve.GetPoint(coef).ToVector3XZ();
        var yOffset = new Vector3(0, path[currentCurve].level * GameRules.LevelHeight, 0);
        float yDeriv = 0;

        if (path[currentCurve].type == RoadSection.RoadType.Upward)
        {
            yOffset += new Vector3(0, GameRules.UpFunction(coef) * GameRules.LevelHeight, 0);
            yDeriv = GameRules.UpFunctionDerivateive(coef) * GameRules.LevelHeight / path[currentCurve].curve.Length;
        }
        if (path[currentCurve].type == RoadSection.RoadType.Downward)
        {
            yOffset += new Vector3(0, -GameRules.UpFunction(coef) * GameRules.LevelHeight, 0);
            yDeriv = -GameRules.UpFunctionDerivateive(coef) * GameRules.LevelHeight / path[currentCurve].curve.Length;
        }
        transform.position = currentPoint + yOffset;

        var curveTangent = path[currentCurve].curve.GetTangent(coef);

        var yEuler = Vector2.SignedAngle(Vector2.up, curveTangent);
        var xEuler = Vector2.SignedAngle(new(1, 0), new(1, yDeriv));

        transform.eulerAngles = new Vector3(-xEuler, -yEuler, 0);
    }

    private static float UpdateTargetSpeed(float current, float rayCastDist)
    {
        if (rayCastDist < 10) return current * 0.3f;
        if (rayCastDist < 15) return current * 0.6f;
        if (rayCastDist < 20) return current * 0.8f;
        return current;
    }

    private float CalculateMinDistanceToOtherCars()
    {
        float res = 50f;
        int mask = 1 << LayerMask.NameToLayer("car");

        Vector3 hit1Pos = new Vector3(-0.9f, 1, 0);
        Vector3 hit2Pos = new Vector3(0, 1, 0);
        Vector3 hit3Pos = new Vector3(0.9f, 1, 0);

        Vector3 worldVector1 = transform.TransformPoint(hit1Pos);
        Vector3 worldVector2 = transform.TransformPoint(hit2Pos);
        Vector3 worldVector3 = transform.TransformPoint(hit3Pos);

        Ray ray1 = new Ray(worldVector1, transform.forward);
        Ray ray2 = new Ray(worldVector2, transform.forward);
        Ray ray3 = new Ray(worldVector3, transform.forward);

        Debug.DrawRay(ray1.GetPoint(0), ray1.direction);
        Debug.DrawRay(ray2.GetPoint(0), ray2.direction);
        Debug.DrawRay(ray3.GetPoint(0), ray3.direction);

        if (Physics.Raycast(ray1, out RaycastHit hit1, res, mask) && hit1.distance < res)
        {
            res = hit1.distance;
            BlockedBy = hit1.collider.GetComponent<Car>();
        }
        if (Physics.Raycast(ray2, out RaycastHit hit2, res, mask) && hit2.distance < res)
        {
            res = hit2.distance;
            BlockedBy = hit2.collider.GetComponent<Car>();
        }
        if (Physics.Raycast(ray3, out RaycastHit hit3, res, mask) && hit3.distance < res)
        {
            res = hit3.distance;
            BlockedBy = hit3.collider.GetComponent<Car>();
        }

        return res;
    }

    private (float, float) FindNextControlPoint()
    {
        if (speedControlPoints.Count == 0) return (-1f, 5f);
        int left = 0;
        int right = speedControlPoints.Count - 1;

        int result = -1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (speedControlPoints[mid].Item1 > totalDist)
            {
                result = mid;
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        if (result != -1) return speedControlPoints[result];
        return (-1, 5);
    }
}
