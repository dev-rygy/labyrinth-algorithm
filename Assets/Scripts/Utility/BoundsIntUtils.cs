using UnityEngine;

namespace RyansLibrary
{
    /// <summary>
    /// Basic set-style operations on BoundsInt volumes, backing BoundsIntersectOp/BoundsUnionOp and zone-fitting
    /// checks (e.g. MapGeneratorController.SpawnZones uses CanContainBounds to make sure a connection zone's
    /// spawn area is big enough before picking a random position inside it).
    /// </summary>
    public class BoundsIntUtils
    {
        public static BoundsInt IntersectBounds(BoundsInt boundsA, BoundsInt boundsB)
        {
            // Create shared bounds between two zones
            BoundsInt intersectedBounds = new BoundsInt();

            // Find what bounds has the absolute minimum and maximum values on each axis and use those to create the intersecting bounds
            Vector3Int min = new Vector3Int(Mathf.Max(boundsA.xMin, boundsB.xMin), Mathf.Max(boundsA.yMin, boundsB.yMin), Mathf.Max(boundsA.zMin, boundsB.zMin));
            Vector3Int max = new Vector3Int(Mathf.Min(boundsA.xMax, boundsB.xMax), Mathf.Min(boundsA.yMax, boundsB.yMax), Mathf.Min(boundsA.zMax, boundsB.zMax));

            intersectedBounds = new BoundsInt(min, max - min);

            return intersectedBounds;
        }

        public static BoundsInt CombineBounds(BoundsInt boundsA, BoundsInt boundsB)
        {
            // Create shared bounds between two zones
            BoundsInt combinedBounds = new BoundsInt();
            Vector3Int position = new Vector3Int(
                                (int)(boundsA.position.x + boundsB.position.x) / 2,
                                (int)(boundsA.position.y + boundsB.position.y) / 2,
                                (int)(boundsA.position.z + boundsB.position.z) / 2);
            Vector3Int size = boundsA.size + boundsB.size;
            combinedBounds.position = position;
            combinedBounds.size = size;

            return combinedBounds;
        }

        public static bool CanContainBounds(BoundsInt containedBounds, BoundsInt containerBounds)
        {
            // Check if the container bounds can contain the contained bounds
            float xDifference = containerBounds.size.x - containedBounds.size.x;
            float yDifference = containerBounds.size.y - containedBounds.size.y;
            float zDifference = containerBounds.size.z - containedBounds.size.z;

            // If any of the differences are negative, then the container bounds cannot contain the contained bounds
            if (xDifference < 0 || yDifference < 0 || zDifference < 0)
                return false;
            else
                return true;
        }
    }
}
