#if UNITY_EDITOR
using UnityEngine;
using Krearthur.Utils;

namespace Krearthur.GOP
{
    /// <summary>
    /// Class that creates objects and positions them in a circular manner.
    /// </summary>
    [ExecuteInEditMode]
    public class CircleFactory : MonoBehaviour
    {
        [HideInInspector] public ObjectFactory segmentFactory;
        [Range(3, 50)]
        [HideInInspector] public int steps = 8;
        [HideInInspector] public int Steps { get { return steps; } set { steps = value; if (steps < 3) steps = 3; if (steps > 50) steps = 50; } }
        [Range(0f, 360f)]
        [HideInInspector] public float degrees = 360;
        [Range(0f, 360f)]
        [HideInInspector] public float degreesOffset = 0;
        [HideInInspector] public Transform start;
        [HideInInspector] public Transform target;
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 endPos;
        [Tooltip("If true only perfect circles are created")]
        [HideInInspector] public bool forceEven = false;
        [HideInInspector] public bool fill = false;
        [HideInInspector] public bool produceOnUpdate = false;
        [HideInInspector] public bool useAbsolutePositions = true;
        [HideInInspector] public bool snapToGrid = false;
        [HideInInspector] public Vector3 gridSize = Vector3.one;
        [HideInInspector] public bool alignWithNormal = false;
        [HideInInspector] public Vector3 startOffset;
        [HideInInspector] public Vector3 targetOffset;
        [HideInInspector] public GOPainter.CanvasAxis axis;

        [Range(0f, 2f)]
        [HideInInspector] public float correctionRatio = 1;

        [HideInInspector] public bool calculateNumberByPaddingAndDistance = true;
        [HideInInspector] [Range(1f, 10f)] public float padding = 2.5f;

        public bool drawEllipses = false;
        public bool drawNormals = false;
        public bool drawCenter = false;

        protected int polyLineApproximation = 300;
        protected Vector3[] ellipsesPoints = new Vector3[300];
        protected Vector3[] ellipsesNormals = new Vector3[300];
        protected float[] ellipsesAngles = new float[300];
        protected float[] polyLineLengths = new float[300];

        protected Vector3[] positions;
        protected Vector3[] normals;

        public void Hide()
        {
            segmentFactory.DeactivateProducts();
            enabled = false;
        }

        public GameObject Produce()
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

            Vector3 diagonal = endPosWithOffset - startWithOffset;

            if (snapToGrid)
            {
                startWithOffset = startWithOffset.GridPos(gridSize);
                endPosWithOffset = endPosWithOffset.GridPos(gridSize);
                diagonal = endPosWithOffset - startWithOffset;
            }

            // Settings for axis = Y -> XZ-Plane
            float signRadiusA = (endPos.x - startPos.x) < 0 ? -1 : 1;
            float signRadiusB = (endPos.z - startPos.z) < 0 ? -1 : 1;
            float radiusA = signRadiusA * (endPos.x - startPos.x);
            float radiusB = signRadiusB * (endPos.z - startPos.z);

            if (axis == GOPainter.CanvasAxis.X) // YZ-Plane
            {
                signRadiusA = (endPos.z - startPos.z) < 0 ? -1 : 1;
                signRadiusB = (endPos.y - startPos.y) < 0 ? -1 : 1;
                radiusA = signRadiusA * (endPos.z - startPos.z);
                radiusB = signRadiusB * (endPos.y - startPos.y);
            }
            else if (axis == GOPainter.CanvasAxis.Z) // XY-Plane
            {
                signRadiusA = (endPos.x - startPos.x) < 0 ? -1 : 1;
                signRadiusB = (endPos.y - startPos.y) < 0 ? -1 : 1;
                radiusA = signRadiusA * (endPos.x - startPos.x);
                radiusB = signRadiusB * (endPos.y - startPos.y);
            }
            radiusA *= 0.5f;
            radiusB *= 0.5f;
            if (forceEven)
            {
                if (Mathf.Abs(radiusA) > Mathf.Abs(radiusB)) radiusB = Mathf.Abs(radiusA) * signRadiusB;
                if (Mathf.Abs(radiusB) > Mathf.Abs(radiusA)) radiusA = Mathf.Abs(radiusB) * signRadiusA;
            }
            if (Mathf.Abs(radiusA) < 0.05f || Mathf.Abs(radiusB) < 0.05f) return null;

            Vector3 center = startWithOffset + diagonal * 0.5f;
 
            float totalPolyLineLength = CalculateEllipsesAndCircumference(center, radiusA, radiusB);

            // -- Calculate steps based on total arc length divided by object dimension
            steps = Mathf.CeilToInt((totalPolyLineLength + 0.0001f) / (padding * segmentFactory.GetAvgDimension()));
            // ---- Then place the objects at equal distance on polyline points
            float arcLength = totalPolyLineLength / (steps-1);
            if (degrees == 360) arcLength = totalPolyLineLength / steps;
            positions = new Vector3[steps];
            normals = new Vector3[steps];

            // ---- Place objects on ellipses
            int lastPolyIndex = 0;
            for (int step = 0; step < steps; step++)
            {
                float searchLength = step * arcLength;
                Vector3 point = Vector3.zero;

                // walk the polyLine and find point that matches the length
                for (int i = lastPolyIndex; i < ellipsesPoints.Length; i++)
                {
                    if (step == 0)
                    {
                        point = ellipsesPoints[i];
                        normals[step] = ellipsesNormals[i];
                        break;
                    }
                    float currentDelta = Mathf.Abs(searchLength - polyLineLengths[i]);
                    float prevDelta = 999;
                    if (i > 0)
                    {
                        prevDelta = Mathf.Abs(searchLength - polyLineLengths[i - 1]);
                    }
                    float nextDelta = 999;
                    if (i+1 < ellipsesPoints.Length)
                    {
                        nextDelta = Mathf.Abs(searchLength - polyLineLengths[i + 1]);
                    }
                    if (currentDelta < prevDelta && currentDelta < nextDelta)
                    {
                        // found nearest point
                        point = ellipsesPoints[i];
                        // get normal
                        normals[step] = ellipsesNormals[i];
                        break;
                    }
                }

                if (snapToGrid) point = point.GridPos(gridSize);
                positions[step] = point;
            }

            if (alignWithNormal)
            {
                segmentFactory.MassProduceOrUpdate(positions, normals);
            } else
            {
                segmentFactory.MassProduceOrUpdate(positions);
            }
            
            return segmentFactory.GetAt(0);
        }

        protected float CalculateEllipsesAndCircumference(Vector3 center, float radiusA, float radiusB)
        {
            // ---- Create a poly line approximating the ellipses
            // source: https://math.stackexchange.com/questions/701523/regular-division-of-the-perimeter-of-an-ellipse
            float polyAngleStep = degrees / polyLineApproximation;
            float totalPolyLineLength = 0;

            for (int i = 0; i < polyLineApproximation; i++)
            {
                float angle = i * polyAngleStep;
                Vector3 point = CalcPointOnEllipses(center, radiusA, radiusB, angle);
                ellipsesPoints[i] = point;
                if (i > 0)
                {
                    // calculate normal
                    Vector3 line = ellipsesPoints[i] - ellipsesPoints[i - 1];
                    if (axis == GOPainter.CanvasAxis.X) ellipsesNormals[i] = -Vector3.Cross(line, Vector3.right).normalized;
                    if (axis == GOPainter.CanvasAxis.Y) ellipsesNormals[i] = -Vector3.Cross(line, Vector3.up).normalized;
                    if (axis == GOPainter.CanvasAxis.Z) ellipsesNormals[i] = Vector3.Cross(line, Vector3.forward).normalized;

                    totalPolyLineLength += line.magnitude;
                    // store the arc length from the beginning to current point for each point
                    // so we can find the corresponding point faster later
                    polyLineLengths[i] = totalPolyLineLength;
                    ellipsesAngles[i] = angle;
                }
                else
                {
                    if (axis == GOPainter.CanvasAxis.X) ellipsesNormals[i] = Vector3.forward;
                    if (axis == GOPainter.CanvasAxis.Y) ellipsesNormals[i] = Vector3.right;
                    if (axis == GOPainter.CanvasAxis.Z) ellipsesNormals[i] = Vector3.right;
                    polyLineLengths[i] = 0;
                    ellipsesAngles[i] = 0;
                }
            }

            return totalPolyLineLength;
        }

        // source: https://math.stackexchange.com/questions/22064/calculating-a-point-that-lies-on-an-ellipse-given-an-angle
        public Vector3 CalcPointOnEllipses(Vector3 center, float radiusA, float radiusB, float angle)
        {
            if (angle == 90) angle = 89.8f;
            if (angle == 180) angle = 180.2f;
            float rad = Mathf.Deg2Rad * angle;
            // Calculate the two coordinates of the point on ellipses (located at origin)
            float numerator1 = radiusA * radiusB;
            float numerator2 = radiusA * radiusB * Mathf.Tan(rad);
            float denominator = Mathf.Sqrt(radiusB * radiusB + radiusA * radiusA * (Mathf.Tan(rad) * Mathf.Tan(rad)));
            float coord1 = numerator1 / denominator;
            float coord2 = numerator2 / denominator;

            if (angle > 90 && angle < 270)
            {
                coord1 = -coord1;
                coord2 = -coord2;
            }

            if (axis == GOPainter.CanvasAxis.X)
            {
                return center + new Vector3(0, coord2, coord1);
            }
            else if(axis == GOPainter.CanvasAxis.Y)
            {
                return center + new Vector3(coord1, 0, coord2);
            }else 
            {
                return center + new Vector3(coord1, coord2, 0);
            }
        }

        public void Update()
        {
            if (produceOnUpdate)
            {
                Produce();
            }
        }

        private void OnDrawGizmos()
        {
            if (drawEllipses)
            {
                Vector3 center = startPos + (endPos- startPos) * 0.5f;

                if (drawCenter)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawSphere(center, 0.3f);
                }

                // draw first normal
                if (drawNormals)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(ellipsesPoints[0], ellipsesPoints[0] + ellipsesNormals[0]);
                }

                Gizmos.color = Color.cyan;
                // Draw PolyLine
                for (int i = 1; i < polyLineApproximation; i++)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(ellipsesPoints[i - 1], ellipsesPoints[i]);

                    // Draw normal
                    if (drawNormals)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(ellipsesPoints[i], ellipsesPoints[i] + ellipsesNormals[i]);
                    }
                    
                }
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(ellipsesPoints[polyLineApproximation - 1], ellipsesPoints[0]);

            }
        }

    }
}
#endif