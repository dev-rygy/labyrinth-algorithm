using UnityEngine;

namespace RyansLibrary
{
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
    }
}
