using Godot;
using System;
using static Godot.Image;

// Author : Raphaël Guibé

namespace Com.IsartDigital.Physics
{
	public partial class Ball2D : Node2D
	{
        private float radius = 100f;
		[Export] public float Radius { 
            get => radius;
            set { radius = Mathf.Abs(value); }
        }

		[Export] private DetectionPolygon2D[] colliders;
        private bool collided = false;

        private float closestDistance;
        private Vector2 closestPoint;

        private Plane2D plane = null;

        Plane2D debugPlane = null;
        Color color = Colors.White;

        private Vector2 velocity = Vector2.Zero;
        private Vector2 reflection = Vector2.Zero;
        private float damping = 0.7f;

        [Export] private float gravity = 300f;
        private float currentGravity = 0f;
        private readonly Vector2 gravityVector = Vector2.Down;

        [Export(PropertyHint.Range, "0,1")]
        public float Damping
        {
            get => damping;
            set { damping = Mathf.Min(MathF.Abs(value), 1f); }
        }

        private const float EXPLOSION_FORCE = 700f;
        private const float SPEED_MIN_THRESHOLD = 2f;
        private const float SPEED_MAX_TRESHOLD = 2000f;

        public override void _Draw()
        {
            base._Draw();
            DrawCircle(Vector2.Zero, radius, color);
            DrawLine(Vector2.Zero, velocity.Normalized() * Radius * damping, Colors.Black, 4);

            if (debugPlane !=null) DrawLine(debugPlane.debugStartPoint - Position, Vector2.Zero, Colors.Blue, 5f);

            if (plane != null)
            {
                // the line starting and ending points
                Vector2 startingPoint = Vector2.Zero;
                Vector2 endingPoint = plane.normal * radius;
                DrawLine(startingPoint,endingPoint , Colors.Red, 5f);

                // the arrow size and flatness
                float arrowSize = 20f;
                float flatness = 0.5f;

                // calculate the direction vector
                Vector2 direction = (endingPoint - startingPoint).Normalized();

                // calculate the side vectors
                Vector2 side1 = new Vector2(-direction.Y, direction.X);
                Vector2 side2 = new Vector2(direction.Y, -direction.X);

                // calculate the T-junction points
                Vector2 e1 = endingPoint + side1 * arrowSize * flatness;
                Vector2 e2 = endingPoint + side2 * arrowSize * flatness;

                // calculate the arrow edges
                Vector2 p1 = e1 - direction * arrowSize;
                Vector2 p2 = e2 - direction * arrowSize;

                // draw the arrow sides as a polygon
                DrawPolygon(new Vector2[] { endingPoint, p1, p2 }, new[] { Colors.Red });

                // alternatively, draw the arrow as two lines
                DrawLine(endingPoint, p1, Colors.Red, 2);
                DrawLine(endingPoint, p2, Colors.Red, 2);
            }
           
        }

        public override void _PhysicsProcess(double pDelta)
        {
            float lDelta = (float)pDelta;

            #region Collision Detection

            collided = false;

            Vector2 lProjectedPoint;
            float lDistance;
            Plane2D lPlane;

            foreach (DetectionPolygon2D lCollider in colliders)
            {
                lPlane = BallInArea(lCollider);
                if (lPlane != null)
                {
                    collided = true;

                    Vector2 lNormal = lPlane.normal;

                    lProjectedPoint = lPlane.PointProjection(Position - radius * (-lPlane.normal));
                    

                    if (closestDistance < Radius)
                    {
                        lNormal = -plane.normal;
                        lProjectedPoint = closestPoint;

                        GD.Print("Corner");
                    }
                    lDistance = lProjectedPoint.DistanceTo(Position);

                    Position += (lProjectedPoint + lNormal * (Radius - lDistance)) - lProjectedPoint;

                    reflection = velocity - 2 *(velocity.Dot(lNormal)) * lNormal;
                    GD.Print(reflection.Length());
                    if (reflection.Length() > SPEED_MIN_THRESHOLD)
                        velocity = velocity.Lerp(reflection, damping);
                }
            }

            //if (collided) color = Colors.Blue;
            //else color = Colors.White;
            velocity += gravity * gravityVector * lDelta;

            Position += velocity * lDelta;

            #endregion

            QueueRedraw();
        }

        public Plane2D BallInArea(DetectionPolygon2D pObject)
        {
            Plane2D lPlane = pObject.PointInArea(Position, radius);
            if (lPlane!=null)
            {
                (Vector2, float) lClosestPoint = pObject.FindClosestPointTo(Position);
                Vector2 lNormal = (lClosestPoint.Item1 - Position).Normalized();
                plane = new Plane2D(Position.Dot(lNormal), lNormal);

                if (!IsSeparatingPlane(pObject.Shape.Polygon, pObject.pointsPos))
                {
                    debugPlane = lPlane;
                    plane.SetPlaneOnPoint(lClosestPoint.Item1);
                    closestPoint = lClosestPoint.Item1;
                    closestDistance = lClosestPoint.Item2;
                    return lPlane;
                }
            }
            return null;
        }

        public bool IsSeparatingPlane(Vector2[] pPoints, Vector2 pObjectPosition)
        {
            foreach (Vector2 pPoint in pPoints)
            {
                if (plane.DistanceToPlane(pPoint + pObjectPosition) <= radius)
                {
                    return false;
                }
            }
            return true;
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (Input.IsActionJustPressed("Explosion"))
            {
                GD.Print(EXPLOSION_FORCE);
                Vector2 lDirection = (GlobalPosition - GetGlobalMousePosition());
                float lDistance = lDirection.Length();
                lDirection = lDirection.Normalized();

                float lForce = Mathf.Max(EXPLOSION_FORCE - lDistance, 0);
                velocity += lDirection * lForce;
            }
        }
    }
}
