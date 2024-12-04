using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sloyd.WebAPI
{
    public static class AutoPlacementUtility
    {
        public static Vector3 FindNearestFreeSpace(GameObject newObject, float searchRadius = 50f, float gridStep = 1f)
        {
            var existingObjects = Object.FindObjectsOfType<SloydSceneObject>();
            if (existingObjects == null || existingObjects.Length == 0)
            {
                return Vector3.zero;
            }

            Bounds newObjectBounds = CalculateBounds(newObject);

            List<(Vector3 position, float distance)> potentialPositions = new List<(Vector3, float)>();

            for (float x = -searchRadius; x <= searchRadius; x += gridStep)
            {
                for (float z = -searchRadius; z <= searchRadius; z += gridStep)
                {
                    Vector3 candidatePosition = new Vector3(x, 0, z);
                    newObjectBounds.center = candidatePosition;
                    
                    if (!IsColliding(newObjectBounds, existingObjects))
                    {
                        float distance = candidatePosition.magnitude;
                        potentialPositions.Add((candidatePosition, distance));
                    }
                }
            }

            if (potentialPositions.Count > 0)
            {
                return potentialPositions.OrderBy(p => p.distance).First().position;
            }
            else
            {
                return FindNearestFreeSpace(newObject, searchRadius * 2f, gridStep);
            }
        }

        private static Bounds CalculateBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(gameObject.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static bool IsColliding(Bounds bounds, SloydSceneObject[] existingObjects)
        {
            foreach (var obj in existingObjects)
            {
                Bounds existingBounds = CalculateBounds(obj.gameObject);
                if (bounds.Intersects(existingBounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}