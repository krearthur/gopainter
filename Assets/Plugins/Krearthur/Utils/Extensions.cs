#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Krearthur.Utils
{
    public static class Extensions
    {

        public static bool IsPrefab(this GameObject go)
        {
            if (go.scene.rootCount == 0) return true;
            PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(go);

            return status == PrefabInstanceStatus.Connected
                || status == PrefabInstanceStatus.Disconnected;
        }

        /// <summary>
        /// Gets the prefab this game object is instanced from, or just the game object if not found.
        /// </summary>
        public static GameObject GetPrefabInstance(this GameObject go)
        {
            if (go.IsPrefab()) return go;

            GameObject prefab = PrefabUtility.GetPrefabInstanceHandle(go) as GameObject;
            if (prefab == null)
            {
                prefab = go;
            }
            return prefab;
        } 

        public static GameObject GetPrefabAsset(this GameObject go)
        {
            if (go.IsPrefab() == false) return null;

            string pathToPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            return PrefabUtility.GetCorrespondingObjectFromSourceAtPath(go, pathToPrefab);
        }

        public static Vector3 GridPos(this Vector3 pos, Vector3 gridSize)
        {
            return GridPos(pos, gridSize.x, gridSize.y, gridSize.z);
        }

        public static Vector3 GridPos(this Vector3 pos, float gridSizeX = 1, float gridSizeY = 1, float gridSizeZ = 1)
        {
            Vector3 gridPos = new Vector3(
                (Mathf.RoundToInt(pos.x / gridSizeX)) * gridSizeX,
                (Mathf.RoundToInt(pos.y / gridSizeY)) * gridSizeY,
                (Mathf.RoundToInt(pos.z / gridSizeZ)) * gridSizeZ);

            return gridPos;
        }

        public static float GridPos(this float scalar, float gridSize)
        {
            return (int)(scalar / gridSize) * gridSize;
        }

        public static Vector3 IntPos(this Vector3 pos)
        {
            return new Vector3((int)(pos.x), (int)(pos.y), (int)(pos.z));
        }
    }
}
#endif