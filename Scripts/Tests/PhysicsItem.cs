using Godot;
using System;

// Author : Raphaël Guibé

namespace Com.IsartDigital.Physics
{
	public partial class PhysicsItem: DetectionPolygon2D
	{
        [Export] DetectionPolygon2D PhysicsObject;
        Vector2 previousPos = default;

        public override void _Ready()
        {
            base._Ready();
            previousPos = Position;
        }

        public override void _PhysicsProcess(double delta)
        {
            Position = GetGlobalMousePosition();
            hasMoved = previousPos != Position;
            previousPos = Position;
            QueueRedraw();
            if (ShapeInArea(PhysicsObject)) Shape.Modulate = Colors.Blue;
            else Shape.Modulate = Colors.White;
        }
    }
}
