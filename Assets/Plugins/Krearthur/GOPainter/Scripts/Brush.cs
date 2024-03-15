using UnityEngine;

namespace Krearthur.GOP
{
    /// <summary>
    /// A component just to mark the paint brush game object
    /// </summary>
    public class Brush : MonoBehaviour
    {
        void Awake()
        {
            if (Application.isPlaying)
            {
                // normaly GO Painter should have deleted the brush object, but make sure it will be deleted when game starts
                Destroy(this.gameObject);
            }
        }
    }

}