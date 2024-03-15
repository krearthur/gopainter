#if UNITY_EDITOR
using UnityEngine;

namespace Krearthur.GOP
{
    /// <summary>
    /// An extra component for GOPainter that adds a little bouncy popping animation to newly painted objects.
    /// </summary>
    [ExecuteInEditMode]
    public class ObjectPaintAnimation : MonoBehaviour, GOComponent
    {
        protected GOPainter goPainter;

        private void Start(){}

        public void Register()
        {
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            goPainter.OnObjectPainted += PopObject;
            goPainter.OnObjectMassPainted += PopObjects;
        }

        public void DeRegister()
        {
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            goPainter.OnObjectPainted -= PopObject;
            goPainter.OnObjectMassPainted -= PopObjects;
        }

        void PopObjects(GameObject[] gos, Vector3 axis, bool snapToGrid)
        {
            if (!enabled) return;
            foreach (GameObject go in gos)
            {
                PopObject(go, axis, snapToGrid);
            }
        }

        void PopObject(GameObject go, Vector3 axis, bool snapToGrid)
        {
            if (!enabled) return;
            go.AddComponent<Pop>();
        }
    }

}
#endif