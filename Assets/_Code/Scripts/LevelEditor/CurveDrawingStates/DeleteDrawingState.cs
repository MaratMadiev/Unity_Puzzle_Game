using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

public class DeleteDrawingState : AbstractDrawingState
{
    int levelLayerMask;
    private Mesh mesh;
    public DeleteDrawingState(GameManager gm, LevelEditor editor) : base(gm, editor)
    {
        levelLayerMask = 1 << LayerMask.NameToLayer("roadDeletable");
    }

    public override bool IsCurrentlyDrawing => false;

    public override void Enter()
    {
        mesh = new();
        mesh.MarkDynamic();
    }

    public override void Exit()
    {
        UnityEngine.Object.Destroy(mesh);
    }

    public override void Update()
    {
        var ray = editor.Cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, levelLayerMask))
        {
            var go = hit.transform.gameObject;
            var roadSection = go.GetComponent<RoadSection>();

            GenerateGuideMeshCurve(ref mesh, roadSection.Curve, roadSection.Type, roadSection.Level, 1f);
            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, editor.IndicatorMaterial, 0);
            if (Input.GetMouseButtonDown(0))
            {
                gm.RemoveById(roadSection.Id);
                editor.OnDelete();
            } 
        }

    }
}

