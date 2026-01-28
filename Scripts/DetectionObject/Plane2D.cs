using Godot;
using System;

// Author : Raphaël Guibé

namespace Com.IsartDigital.Physics
{
	public class Plane2D
	{
        /// <summary>
        /// Create a plane <paramref name="pPoint"/> a point on the plane <paramref name="pDvec"/> the directory vector of the plane
        /// </summary>
        /// <param name="pPoint"></param>
        /// <param name="pDvec"></param>
        public Plane2D(Vector2 pPoint, Vector2 pDvec) 
        {
            dvec = pDvec;
            normal = new Vector2(-pDvec.Y, pDvec.X);
            distance = normal.Dot(pPoint);
        }
        public Plane2D(float pDistance, Vector2 pNormal)
        {
            dvec = new Vector2(-pNormal.Y, pNormal.X);
            normal = pNormal;
            distance = pDistance;
        }

        public Vector2 normal;
        public Vector2 dvec;
        public float distance; // from origin

        public Vector2 debugStartPoint = Vector2.One;

        public float DistanceToPlane(Vector2 pPoint)
        {
            return normal.Dot(pPoint) - distance;
        }

        public Vector2 PointProjection(Vector2 pPoint)
        {
            float lDist = DistanceToPlane(pPoint);
            return pPoint - lDist * normal;
        }

        public void ReversePlane()
        {
            normal = -normal;
            distance = -distance;
        }

        public void SetPlaneOnPoint(Vector2 pPoint)
        {
            distance = normal.Dot(pPoint);
        }
    }
}
