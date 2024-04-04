#if UNITY_EDITOR
using UnityEngine;
using Krearthur.Utils;

namespace Krearthur.GOP
{
    /// <summary>
    /// Class that creates objects and arranges them in a line. 
    /// </summary>
    [ExecuteInEditMode]
    public class LineFactory : MonoBehaviour
    {
        [HideInInspector] public ObjectFactory segmentFactory;

        [HideInInspector] [Range(3, 50)] public int numberOfSegments = 3;
        [HideInInspector] public Transform start;
        [HideInInspector] public Transform target;
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 endPos;
        [HideInInspector] public bool produceOnUpdate = false;
        [HideInInspector] public bool useAbsolutePositions = true;
        [HideInInspector] public bool snapToGrid = false;
        [HideInInspector] public Vector3 gridSize = Vector3.one;
        [HideInInspector] public bool alignObjects = false;
        [HideInInspector] public Vector3 startOffset;
        [HideInInspector] public Vector3 targetOffset;
        [HideInInspector] [Range(0, 0.5f)] public float trimStart = 0.1f;
        [HideInInspector] [Range(0.5f, 1f)] public float trimEnd = 0.9f;

        [HideInInspector] public bool calculateNumberByPaddingAndDistance = true;
        [HideInInspector] [Range(1f, 10f)] public float padding = 2.5f;

        [HideInInspector] public bool animate = false;
        [HideInInspector] [SerializeField] private float animationTimer;

        [HideInInspector] [Range(0.5f, 10f)] public float animationSpeed = 2;

        public bool debugDraw = false;

        public void Hide()
        {
            segmentFactory.DeactivateProducts();
            enabled = false;
        }

        public GameObject Produce(Vector3 position)
        {
            segmentFactory.ActivateProducts();
            if (segmentFactory == null) return null;

            if (start == null)
            {
                start = transform;
            }

            Vector3 startWithOffset = start.position + startOffset;
            if (useAbsolutePositions)
            {
                startWithOffset = startPos;
            }

            if (target == null && !useAbsolutePositions)
            {
                return segmentFactory.ProduceOrUpdate(startWithOffset);
            }

            Vector3 endPosWithOffset = endPos;
            if (!useAbsolutePositions)
            {
                endPosWithOffset = target.position + targetOffset;
            }

            Vector3 trail = endPosWithOffset - startWithOffset;
            Vector3 originalTrail = trail;
            Vector3 originalStart = startWithOffset;
            Vector3 originalEnd = endPosWithOffset;

            if (trimStart > 0.0001f)
            {
                startWithOffset = originalStart + originalTrail * trimStart;
                trail = endPosWithOffset - startWithOffset;
            }

            if (trimEnd < 0.9999f)
            {
                endPosWithOffset = originalStart + originalTrail * trimEnd;
                trail = endPosWithOffset - startWithOffset;
            }

            if (snapToGrid)
            {
                startWithOffset = startWithOffset.GridPos(gridSize);
                endPosWithOffset = endPosWithOffset.GridPos(gridSize);
                trail = endPosWithOffset - startWithOffset;
            }

            if (calculateNumberByPaddingAndDistance)
            {
                float objectLength = segmentFactory.GetAvgDimension();
                numberOfSegments = Mathf.CeilToInt((trail.magnitude + 0.0001f) / (padding * objectLength));

                if (numberOfSegments < 0) numberOfSegments = 1;
                if (numberOfSegments == 0) return null;
            }
            Vector3[] positions = new Vector3[animate ? numberOfSegments - 1 : numberOfSegments];

            // grows from 0 to 1 and then jumps back to 0 periodically
            animationTimer = !animate ? 0 : (animationTimer + Time.deltaTime * animationSpeed) % 1;

            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = startWithOffset + trail * (float)(i + animationTimer) / (numberOfSegments == 1 ? 2 : numberOfSegments - 1);
                if (snapToGrid)
                {
                    positions[i] = positions[i].GridPos(gridSize);
                }
            }

            if (alignObjects)
            {
                Vector3 direction = (endPosWithOffset - startWithOffset).normalized;
                segmentFactory.MassProduceOrUpdate(positions, direction);
            } else
            {
                segmentFactory.MassProduceOrUpdate(positions);
            }

            return segmentFactory.GetAt(0);
        }

        public void Update()
        {
            if (produceOnUpdate)
            {
                Produce(startPos);
            }
        }

        private void OnDrawGizmos()
        {
            if (debugDraw)
            {
                Gizmos.DrawSphere(startPos, 0.5f);
                Gizmos.DrawSphere(endPos, 0.5f);
            }
        }
    }
}
#endif