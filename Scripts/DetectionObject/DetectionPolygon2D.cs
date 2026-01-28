using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

// Author : Raphaël Guibé

namespace Com.IsartDigital.Physics
{
    [GlobalClass]
	public partial class DetectionPolygon2D: Node2D
	{
		[Export] public Polygon2D Shape { get; private set; } = default;
        public List<Plane2D> Planes { get; private set; } = new List<Plane2D>();

        private Vector2 adjacentVector = Vector2.Zero;
        private Vector2 previousNormal;

        private bool flipCheck = true; //If this is true, will Check if the normal is in the right side, then will set itself to false
        private bool planesReversed = false;

        protected bool hasMoved = false; //If this bool is true The hitBox & PointsPos will updates in the process
        protected bool shapeUpdated = false;
        public Vector2 pointsPos; //Position To convert Shape Points Local Position into This Object Position Using UpdatePointsPos()

        public override void _Ready()
        {
            UpdatePointsPos();
            UpdateHitBox(true);
            QueueRedraw();
        }

        //Draw for debug
        public override void _Draw()
        {
            base._Draw();
            foreach(Plane2D lPlane in Planes)
            {
                // the line starting and ending points
                Vector2 startingPoint = lPlane.debugStartPoint;
                Vector2 endingPoint = lPlane.debugStartPoint + lPlane.normal * 100;
                DrawLine(startingPoint, endingPoint, Colors.Red, 4);

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

        //if needed can change to _Process
        public override void _PhysicsProcess(double pDelta)
        {
            if (hasMoved)
            {
                UpdatePointsPos();
                UpdateHitBox(shapeUpdated);
            }
        }

        public void UpdatePointsPos() { pointsPos = Position + Shape.Position; }

        /// <summary>
        /// If <paramref name="pUpdatePolygon"/> This function will recreate every planes from zero
        /// <para> Else it will juste change the distance of the plane</para>
        /// </summary>
        /// <param name="pUpdatePolygon"></param>
        public void UpdateHitBox(bool pUpdatePolygon = false)
        {
            int lCount = Shape.Polygon.Length;
            Plane2D lPlane;
            
            if (pUpdatePolygon)
            {
                Planes.Clear();
                flipCheck = pUpdatePolygon;

                for (int i = 0; i < lCount; i++)
                {
                    lPlane = CreatePlane(Shape.Polygon[i], Shape.Polygon[(i + 1) % lCount]);
                    Planes.Add(lPlane);
                    if (flipCheck) adjacentVector = lPlane.dvec;
                }
                if (planesReversed) foreach (Plane2D lPlaneToReverse in Planes) lPlaneToReverse.ReversePlane();
            }
            else
            {
                for (int i = 0; i < lCount; i++)
                {
                    lPlane = Planes[i];
                    lPlane.distance = (Shape.Polygon[i] + pointsPos).Dot(lPlane.normal);
                }
            }
        }

        /// <summary>
        /// Create a Place between 2 points and Adjusts the normal automaticly
        /// </summary>
        /// <param name="pPointA"></param>
        /// <param name="pPointB"></param>
        /// <returns></returns>
        protected Plane2D CreatePlane(Vector2 pPointA, Vector2 pPointB)
        {
            Vector2 lDVec = (pPointA - pPointB).Normalized();
            Plane2D lPlane = new Plane2D(
                pPointA + pointsPos,
                lDVec
            );

            lPlane.debugStartPoint = (pPointB + pPointA) * 0.5f + Shape.Position;

            previousNormal = lPlane.normal;

            if(flipCheck && adjacentVector != Vector2.Zero)
            {
                flipCheck = false;
                float lDot = previousNormal.Dot(adjacentVector);
                if (lDot > 0)
                {
                    planesReversed = true;
                }
                else if (lDot == 0) flipCheck = true;
            }
            return lPlane;
        }

        #region Point Detection

        /// <summary>
        /// Checks if a Point is in the Polygon and the Distance is used for spheres
        /// </summary>
        /// <param name="pPoint"></param>
        /// <param name="pDistance"></param>
        /// <returns></returns>
        public Plane2D PointInArea(Vector2 pPoint, float pDistance = 0f)
        {
            float lDistance;
            float lMinDistance = pDistance;
            Plane2D lClosestPlane = null;
            foreach (Plane2D lPlane in Planes)
            {
                lDistance = lPlane.DistanceToPlane(pPoint);
                if (lDistance > pDistance) return null;
                lDistance = Math.Abs(lDistance - pDistance);
                if (lMinDistance > lDistance)
                {
                    lMinDistance = lDistance;
                    lClosestPlane = lPlane;
                }
            }
            return lClosestPlane;
        }

        /// <summary>
        /// Finds the closest Points to <paramref name="pPoint"/> in the <see cref="Shape"/>
        /// </summary>
        /// <param name="pPoint"></param>
        /// <returns></returns>
        public (Vector2, float) FindClosestPointTo(Vector2 pPoint)
        {
            Vector2 lMinPoint = (Shape.Polygon[0] + pointsPos);
            float lMinDistance = lMinPoint.DistanceSquaredTo(pPoint); //Distance Squared is easier to compute

            Vector2 lPoint;
            float lDistance;

            for (int i = 1; i < Shape.Polygon.Length; i++)
            {
                lPoint = (Shape.Polygon[i] + pointsPos);
                lDistance = lPoint.DistanceSquaredTo(pPoint);
                if (lDistance < lMinDistance)
                {
                    lMinDistance = lDistance;
                    lMinPoint = lPoint;
                }
            }
            return (lMinPoint,Mathf.Sqrt(lMinDistance));
        }

        #endregion

        #region Shapes Detection

        /// <summary>
        /// Returns true if a Separating Plane was found
        /// </summary>
        /// <param name="pPoints"></param>
        /// <param name="pPlanes"></param>
        /// <param name="pObjectPosition"></param>
        /// <returns></returns>
        public bool FindSeparatingPlane(Vector2[] pPoints, List<Plane2D> pPlanes, Vector2 pObjectPosition = default)
        {
            bool lIsOut;
            foreach (Plane2D lPlane in pPlanes)
            {
                lIsOut = true;
                foreach (Vector2 pPoint in pPoints)
                {
                    if (lPlane.DistanceToPlane(pPoint + pObjectPosition) < 0)
                    {
                        lIsOut = false;
                        break;
                    }
                }
                if (lIsOut) return false;//separating Plane found
            }
            return true;
        }

        /// <summary>
        /// Returns true if This polygon is colliding with another DetectionPolygon2D using Seperating Plan Theorem
        /// <para> Only works with Convex Shapes</para>
        /// </summary>
        /// <param name="pPoints"></param>
        /// <param name="pPlanes"></param>
        /// <param name="pObjectPosition"></param>
        /// <returns></returns>
        public bool ShapeInArea(Vector2[] pPoints, List<Plane2D> pPlanes, Vector2 pObjectPosition = default)
        {
            if (FindSeparatingPlane(Shape.Polygon, pPlanes, pointsPos)) 
                return FindSeparatingPlane(pPoints, Planes, pObjectPosition); //Finds if a separating plane exists in any of the planes of both objects
            return false;
        }

        /// <summary>
        /// Returns true if This polygon is colliding with another DetectionPolygon2D using Seperating Plan Theorem
        /// <para> Only works with Convex Shapes</para>
        /// </summary>
        /// <param name="pPoints"></param>
        /// <param name="pPlanes"></param>
        /// <param name="pObjectPosition"></param>
        /// <returns></returns>
        public bool ShapeInArea(DetectionPolygon2D pObject) => ShapeInArea(pObject.Shape.Polygon, pObject.Planes, pObject.pointsPos);

        #endregion
    }
}
