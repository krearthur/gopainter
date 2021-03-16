using UnityEngine;

namespace Krearthur.Utils
{
    /// <summary>
    /// The purpose of this class is to just mark objects so they can be found more easily
    /// </summary>
    public class Marker : MonoBehaviour
    {
        public MarkerCode typeCode;
    }

    public enum MarkerCode
    {
        RandomColor,
        MarkForDestruction
    }
}