using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class IdleDrawingState : AbstractDrawingState
{
    public IdleDrawingState(GameManager gm, LevelEditor editor) : base(gm, editor)
    {
    }

    public override bool IsCurrentlyDrawing => false;

    public override void Enter()
    {
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
    }
}

