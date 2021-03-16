#if UNITY_EDITOR
using UnityEngine;

namespace Krearthur.GOP
{
    /// <summary>
    /// Simple animation class that gives objects a bouncy animation
    /// </summary>
    [ExecuteInEditMode]
    public class Pop : MonoBehaviour
    {
        public float time = 0;
        public float freq = 3;
        public float decay = 5;
        public float dur = 0.3f;

        public Vector3 targetScale;
        void Start()
        {
            targetScale = transform.localScale;
            MeshFilter filter = GetComponentInChildren<MeshFilter>();
            float length = 1;
            if (filter != null)
            {
                length = filter.sharedMesh.bounds.extents.magnitude;
            }
            

            decay = 1 / length * 5;
            if (decay > 10) decay = 10;
            if (decay < 5) decay = 5;
        }

        // Update is called once per frame
        void Update()
        {
            time += Time.deltaTime;

            float t = time;
        
            Vector3 startVal = Vector3.one * 0.1f;
            Vector3 endVal = targetScale;

            Vector3 amp = (endVal - startVal) / dur;
            float w = freq * Mathf.PI * 2;
            Vector3 cur = endVal + amp * (Mathf.Sin((t - dur) * w) / Mathf.Exp(decay * (t - dur)) / w);

            transform.localScale = cur;

            if (time >= 1)
            {
                transform.localScale = targetScale;
                DestroyImmediate(this);
            }

        }
    }

}
#endif