using UnityEngine;

namespace Krearthur.GOP
{
    /// <summary>
    /// Base implementation of the IGridTransform, to use a custom offset instead of the objects collider or mesh bounds.
    /// </summary>
    public class GridTransform : MonoBehaviour, IGridTransform
    {
        public Vector3 offset = new Vector3(-0.5f, 0, -0.5f);
        [Range(0, 10)]
        public int sizeX = 1;
        [Range(0, 10)]
        public int sizeY = 1;
        [Range(0, 10)]
        public int sizeZ = 1;

        public Vector3 Position { get => transform.position + offset; set => transform.position = value - offset; }

        public Vector3 Offset { get => offset; set => offset = value; }

        public int SizeX { get => sizeX; set => sizeX = value; }
        public int SizeY { get => sizeY; set => sizeY = value; }
        public int SizeZ { get => sizeZ; set => sizeZ = value; }

    }

}