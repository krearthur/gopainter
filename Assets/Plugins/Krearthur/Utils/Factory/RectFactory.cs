#if UNITY_EDITOR
using UnityEngine;
using Krearthur.Utils;

namespace Krearthur.GOP
{
    /// <summary>
    /// Class that creates objects and positions them in a rectangular manner.
    /// </summary>
    [ExecuteInEditMode]
    public class RectFactory : MonoBehaviour
    {
        [HideInInspector] public ObjectFactory segmentFactory;

        [HideInInspector] [Range(3, 50)] public int rows = 3;
        [HideInInspector] [Range(3, 50)] public int columns = 3;
        [HideInInspector] public Transform start;
        [HideInInspector] public Transform target;
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 endPos;
        [HideInInspector] public bool produceOnUpdate = false;
        [HideInInspector] public bool useAbsolutePositions = true;
        [HideInInspector] public bool snapToGrid = false;
        [HideInInspector] public bool fill = true;
        [Tooltip("Align objects along columns and rows. Only works when fill is set to false")]
        [HideInInspector] public bool alignObjects = false;

        [HideInInspector] public Vector3 gridSize = Vector3.one;
        [HideInInspector] public Vector3 startOffset;
        [HideInInspector] public Vector3 targetOffset;
        [HideInInspector] public GOPainter.CanvasAxis axis;

        [HideInInspector] public bool calculateNumberByPaddingAndDistance = true;
        [HideInInspector] [Range(1f, 10f)] public float padding = 2.5f;

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

            Vector3 diagonal = endPosWithOffset - startWithOffset;

            if (snapToGrid)
            {
                startWithOffset = startWithOffset.GridPos(gridSize);
                endPosWithOffset = endPosWithOffset.GridPos(gridSize);
                diagonal = endPosWithOffset - startWithOffset;
            }

            // Settings for axis = Y -> XZ-Plane
            float signWidth = (endPos.x - startPos.x) < 0 ? -1 : 1;
            float signHeight = (endPos.z - startPos.z) < 0 ? -1 : 1;
            float width = signWidth * (endPos.x - startPos.x);
            float height = signHeight * (endPos.z - startPos.z);
            Vector3 rowDirection = new Vector3(endPos.x, startPos.y, startPos.z) - startPos;
            Vector3 columnDirection = new Vector3(startPos.x, startPos.y, endPos.z) - startPos;

            if (axis == GOPainter.CanvasAxis.X) // YZ-Plane
            {
                signWidth = (endPos.z - startPos.z) < 0 ? -1 : 1;
                signHeight = (endPos.y - startPos.y) < 0 ? -1 : 1;
                width = signWidth * (endPos.z - startPos.z);
                height = signHeight * (endPos.y - startPos.y);
                rowDirection = new Vector3(startPos.x, startPos.y, endPos.z) - startPos;
                columnDirection = new Vector3(startPos.x, endPos.y, startPos.z) - startPos;
            }
            else if (axis == GOPainter.CanvasAxis.Z) // XY-Plane
            {
                signWidth = (endPos.x - startPos.x) < 0 ? -1 : 1;
                signHeight = (endPos.y - startPos.y) < 0 ? -1 : 1;
                width = signWidth * (endPos.x - startPos.x);
                height = signHeight * (endPos.y - startPos.y);
                rowDirection = new Vector3(endPos.x, startPos.y, startPos.z) - startPos;
                columnDirection = new Vector3(startPos.x, endPos.y, startPos.z) - startPos;
            }

            rowDirection = rowDirection.normalized;
            columnDirection = columnDirection.normalized;

            if (calculateNumberByPaddingAndDistance)
            {
                columns = Mathf.CeilToInt((width) / (padding * segmentFactory.GetAvgDimension()));
                rows = Mathf.CeilToInt((height) / (padding * segmentFactory.GetAvgDimension()));

                if (rows == 0 || columns == 0) return null;
            }

            Vector3[] positions = new Vector3[rows * columns];
            Vector3[] directions = new Vector3[1];
            if (!fill)
            {
                int size = 0;
                if (rows <= 2 || columns <= 2)
                {
                    size = rows * columns;
                } else
                {
                    size = rows * 2 + (columns - 2) * 2;
                }
                positions = new Vector3[size];
                directions = new Vector3[size];
            }
            int processed = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (!fill)
                    {
                        if (row > 0 && row < rows-1) // rows inside
                        {
                            if (col > 0 && col < columns-1) // columns inside
                            {
                                continue;
                            }
                        }
                    }
                    Vector3 pos = startWithOffset;
                    float dividerWidth = (columns == 1 ? 2 : columns - 1);
                    float dividerHeight = (rows == 1 ? 2 : rows - 1);

                    if (Mathf.Approximately(1, padding))
                    {
                        dividerWidth = columns;
                        dividerHeight = rows;
                    }

                    float deltaWidth = ((signWidth * (width)) / dividerWidth) * col;
                    float deltaHeight = ((signHeight * (height)) / dividerHeight) * row;
                    if (snapToGrid)
                    {
                        deltaWidth = Mathf.RoundToInt((signWidth * (width)) / dividerWidth) * col;
                        deltaHeight = Mathf.RoundToInt((signHeight * (height)) / dividerHeight) * row;
                    }
                    
                    if (axis == GOPainter.CanvasAxis.Y)
                    {
                        pos.x += deltaWidth;
                        pos.z += deltaHeight;
                        pos.y = startWithOffset.y + (endPosWithOffset - startWithOffset).y * col / dividerWidth;
                    }
                    else if (axis == GOPainter.CanvasAxis.X)
                    {
                        pos.z += deltaWidth;
                        pos.y += deltaHeight;
                        pos.x = startWithOffset.x + (endPosWithOffset - startWithOffset).x * col / dividerWidth;
                    }
                    else if (axis == GOPainter.CanvasAxis.Z)
                    {
                        pos.x += deltaWidth;
                        pos.y += deltaHeight;
                        pos.z = startWithOffset.z + (endPosWithOffset - startWithOffset).z * col / dividerWidth;
                    }

                    if (!fill)
                    {
                        if (alignObjects && (row == 0 || row == rows-1))
                        {
                            // align with row
                            directions[processed] = (row == 0)? rowDirection : -rowDirection;

                            //if (col == 0 && row == 0)
                            //{
                            //    directions[processed] = (rowDirection - columnDirection) / 2;
                            //}
                            
                        }else if (alignObjects)
                        {
                            // align with column
                            directions[processed] = (col == 0)? -columnDirection : columnDirection;
                        }
                        positions[processed] = pos;
                    } else
                    {
                        positions[(row * columns) + col] = pos;
                    }
                    processed++;
                }
            }

            if (!fill)
            {
                if (alignObjects)
                {
                    segmentFactory.MassProduceOrUpdate(positions, directions);
                } else
                {
                    segmentFactory.MassProduceOrUpdate(positions);
                }
                
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