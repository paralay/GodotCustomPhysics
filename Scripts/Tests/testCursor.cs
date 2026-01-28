using Godot;
using System;

public partial class testCursor : Sprite2D
{
    public override void _Process(double delta)
    {
        base._Process(delta);
        GlobalPosition = GetGlobalMousePosition();
    }
}
