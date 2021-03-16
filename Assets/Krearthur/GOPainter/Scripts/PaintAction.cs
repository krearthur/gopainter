using System.Collections.Generic;
using UnityEngine;

namespace Krearthur.GOP
{
    /// <summary>
    /// Represents a paint action that is used for history management.
    /// </summary>
    public class PaintAction
    {
        public enum ActionType
        {
            Added,
            Removed
        }

        public ActionType type;
        public List<GameObject> paintObjects;
        protected List<Vector3> positions;
        protected List<Vector3> rotations;

        public PaintAction(GameObject paintSubject, ActionType type)
        {
            paintObjects = new List<GameObject>();
            positions = new List<Vector3>();
            rotations = new List<Vector3>();

            paintObjects.Add(paintSubject);
            positions.Add(paintSubject.transform.position);
            rotations.Add(paintSubject.transform.eulerAngles);

            this.type = type;
        }

        public PaintAction(ICollection<GameObject> paintSubjects, ActionType type)
        {
            paintObjects = new List<GameObject>();
            positions = new List<Vector3>();
            rotations = new List<Vector3>();

            foreach (GameObject go in paintSubjects)
            {
                if (go != null)
                {
                    paintObjects.Add(go);
                    positions.Add(go.transform.position);
                    rotations.Add(go.transform.eulerAngles);
                }

            }

            this.type = type;
        }

        public void Undo(bool ignoreGlueObjects = false)
        {
            if (type == ActionType.Added)
            {
                foreach (GameObject go in paintObjects)
                {
                    if (go == null) continue;

                    //go.SetActive(false);
                    GameObject.DestroyImmediate(go); 
                }
            }
            else if (type == ActionType.Removed)
            {
                for (int i = 0; i < paintObjects.Count; i++)
                {
                    if (paintObjects[i] == null) continue;
                    GameObject go = paintObjects[i];
                    go.SetActive(true);
                    go.transform.position = positions[i];
                    go.transform.eulerAngles = rotations[i];

                }
            }
        }

    }
}