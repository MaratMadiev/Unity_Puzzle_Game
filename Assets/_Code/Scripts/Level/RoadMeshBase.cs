
using System.Collections.Generic;
using UnityEngine;
using static RoadSection;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoadMeshBase : MonoBehaviour
{
    public static readonly float SegmentLength = 0.5f;
    public static readonly float RoadWidth = 3.5f;
    public static readonly float RoadOffset = 0.5f;

    protected MeshFilter mf = null;
    protected MeshRenderer mr = null;
    protected Mesh mesh = null;


    [SerializeField]
    protected Material roadMaterial = null;
    public Material Material
    {
        get { return roadMaterial; }
        set { roadMaterial = value; }
    }

    protected MeshFilter Mf
    {
        get
        {
            if (mf == null) mf = GetComponent<MeshFilter>();
            return mf;
        }
        set => mf = value;
    }
    protected MeshRenderer Mr
    {
        get
        {
            if (mr == null) mr = GetComponent<MeshRenderer>();
            return mr;
        }
        set => mr = value;
    }


    // Start is called before the first frame update
    protected virtual void Start()
    {
        Mf = GetComponent<MeshFilter>();
        Mr = GetComponent<MeshRenderer>();
    }

    public void UpdateMesh(QuadBezier2 curve, int level, RoadType type)
    {

        mesh = new Mesh();
        mesh.name = "RoadMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        List<Vector2> points2D = curve.GetLerpPointsLen(SegmentLength);

        for (int i = 0; i < points2D.Count; i++)
        {
            var tangent = curve.GetTangent(i * 1f / (points2D.Count - 1)).ToVector3XZ();
            var toSide = new Vector3(-tangent.z, 0, tangent.x);

            var yOffset = new Vector3(0, level * GameRules.LevelHeight, 0);
            var ySlightly = new Vector3(0, 0.05f, 0);
            var ySide = new Vector3(0, -0.7f, 0);

            if (type == RoadType.Upward)
            {
                yOffset += new Vector3(0, GameRules.LevelHeight * GameRules.UpFunction(i * 1f / (points2D.Count - 1)), 0);
            }
            else if (type == RoadType.Downward)
            {
                yOffset += new Vector3(0, -GameRules.LevelHeight * GameRules.UpFunction(i * 1f / (points2D.Count - 1)), 0);
            }

            var point = points2D[i].ToVector3XZ() + yOffset; //todo

            vertices.Add(point - toSide * RoadWidth / 2);
            vertices.Add(point + ySlightly);
            vertices.Add(point + toSide * RoadWidth / 2);

            vertices.Add(point + toSide * RoadWidth / 2);
            vertices.Add(point + toSide * (RoadWidth / 2 + RoadOffset) - ySlightly);
            vertices.Add(point + toSide * (RoadWidth / 2 + RoadOffset) - ySlightly);
            vertices.Add(point + toSide * (RoadWidth / 2 + RoadOffset) + ySide);

            vertices.Add(point - toSide * RoadWidth / 2);
            vertices.Add(point - toSide * (RoadWidth / 2 + RoadOffset) - ySlightly);
            vertices.Add(point - toSide * (RoadWidth / 2 + RoadOffset) - ySlightly);
            vertices.Add(point - toSide * (RoadWidth / 2 + RoadOffset) + ySide);

            uv.Add(new Vector2(1, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.75f, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.5f, i * SegmentLength * 0.5f));

            uv.Add(new Vector2(0, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.15f, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.15f, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.3f, i * SegmentLength * 0.5f));

            uv.Add(new Vector2(0, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.15f, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.15f, i * SegmentLength * 0.5f));
            uv.Add(new Vector2(0.3f, i * SegmentLength * 0.5f));
        }

        for (int i = 0; i < points2D.Count - 1; i++)
        {
            int ind = i * 11;
            int indNext = ind + 11;
            //road
            triangles.Add(ind);
            triangles.Add(ind + 1);
            triangles.Add(indNext);

            triangles.Add(indNext);
            triangles.Add(ind + 1);
            triangles.Add(indNext + 1);

            triangles.Add(ind + 1);
            triangles.Add(ind + 2);
            triangles.Add(indNext + 1);

            triangles.Add(indNext + 1);
            triangles.Add(ind + 2);
            triangles.Add(indNext + 2);
            // left concrete part
            triangles.Add(ind + 3);
            triangles.Add(ind + 4);
            triangles.Add(indNext + 3);

            triangles.Add(indNext + 4);
            triangles.Add(indNext + 3);
            triangles.Add(ind + 4);

            triangles.Add(ind + 5);
            triangles.Add(ind + 6);
            triangles.Add(indNext + 5);

            triangles.Add(indNext + 6);
            triangles.Add(indNext + 5);
            triangles.Add(ind + 6);

            //right concrete
            triangles.Add(ind + 7);
            triangles.Add(indNext + 7);
            triangles.Add(ind + 8);

            triangles.Add(indNext + 8);
            triangles.Add(ind + 8);
            triangles.Add(indNext + 7);

            triangles.Add(ind + 9);
            triangles.Add(indNext + 9);
            triangles.Add(ind + 10);

            triangles.Add(indNext + 10);
            triangles.Add(ind + 10);
            triangles.Add(indNext + 9);



        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Mr.material = roadMaterial;

        Mf.sharedMesh = mesh;

    }
}
