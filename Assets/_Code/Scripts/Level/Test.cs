using UnityEngine;


public class Test : MonoBehaviour
{
    QuadBezier2 curve = new(new(0, 0), new(10, 0), new(10, 10));


    [SerializeField]
    Vector2 p1;
    [SerializeField]
    Vector2 p2;
    [SerializeField]
    Vector2 p3;

    [SerializeField]
    Vector2 q1;
    [SerializeField]
    Vector2 q2;
    [SerializeField]


    void Start()
    {
        
    }
    [ContextMenu("curvature")]
    void OnDrawGizmos()
    {
        curve = new QuadBezier2(p1, p2, p3);

        var quad = new Rect(q1, q2 - q1);

        bool s = curve.IntersectsRect(quad);

        Gizmos.color = s ? Color.red : Color.green;
        Gizmos.DrawCube(quad.center.ToVector3XZ(), quad.size.ToVector3XZ(0.2f)); 
        drawCurve(curve, Color.red);
    }

    public static void drawCurve(QuadBezier2 curve, Color color)
    {
        if (curve == null) return;
        var points = curve.GetLerpPoints(5);


        Debug.DrawLine(curve.PointA.ToVector3XZ(), curve.PointB.ToVector3XZ());
        Debug.DrawLine(curve.PointB.ToVector3XZ(), curve.PointC.ToVector3XZ());

        // Рисуем саму кривую
        if (points != null && points.Count > 1)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Debug.DrawLine(points[i].ToVector3XZ(), points[i + 1].ToVector3XZ(), color);
            }
        }

    }
}
