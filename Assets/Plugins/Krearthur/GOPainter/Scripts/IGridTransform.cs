using UnityEngine;

namespace Krearthur.GOP
{
    /// <summary>
    /// This interface serves as another way to determine the offset of objects.
    /// With this interface you can define a custom offset for an object.
    /// When doing so the objects collider or mesh bounds are ignored.
    /// </summary>
    public interface IGridTransform
    {
        Vector3 Position { get; set; }
        Vector3 Offset { get; set; }

        int SizeX { get; set; }
        int SizeY { get; set; }
        int SizeZ { get; set; }

    }

}