#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Krearthur.GOP
{
    /// <summary>
    /// This class casts a ray down for each painted object, until something was hit, and places the object there.
    /// It is designed to be used with the rect, line and circle tools to work with uneven terrain.
    /// </summary>
    [ExecuteInEditMode]
    public class GOSurfaceSnapper : MonoBehaviour, GOComponent
    {
        protected GOPainter goPainter;
        protected ObjectFactory factory;

        [Tooltip("Goes x meters above the painted object, then casts down")]
        public float maxHeightToCastDownFrom = 30;

        private void Start(){}

        public void Register()
        {
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }
            
            goPainter.OnObjectMassPaintedLate += CastObjectsDown;
            goPainter.OnObjectPainted += CastObjectDownGOP;
            

            if (factory == null)
            {
                factory = GetComponent<ObjectFactory>();
            }

            factory.OnObjectProducedLate += CastObjectDown;
            factory.OnObjectUpdatedLate += CastObjectDown;
            SceneView.duringSceneGui += OnScene;
        }

        public void DeRegister()
        {
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }
            
            goPainter.OnObjectMassPaintedLate -= CastObjectsDown;
            goPainter.OnObjectPainted -= CastObjectDownGOP;
            
            if (factory == null)
            {
                factory = GetComponent<ObjectFactory>();
            }

            factory.OnObjectProducedLate -= CastObjectDown;
            factory.OnObjectUpdatedLate -= CastObjectDown;

            SceneView.duringSceneGui -= OnScene;
        }

        void CastObjectDownGOP(GameObject go, Vector3 axis, bool snapToGrid)
        {
            if (!enabled) return;
            CastObjectDownInternal(go);
        }

        void CastObjectDownInternal(GameObject go)
        {
            if (!enabled) return;
            if (this != null && goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            if (this != null && !enabled || goPainter.snapToGrid)
            {
                return;
            }
            //print("yo, snap down!");

            int layerMask = (1 << LayerMask.NameToLayer(goPainter.canvasLayer));
            if (goPainter.paintOnlyOnGround == false)
            {
                layerMask = ~(1 << LayerMask.NameToLayer(goPainter.editLayer));
            }

            RaycastHit[] hits = null;
            Vector3 direction = goPainter.GetCanvasAxis();

            if (goPainter.GetSceneCamAxisPolarity() > 0) direction *= -1;
            if (goPainter.GetCanvasAxis() == Vector3.forward) direction *= -1;

            hits = Physics.RaycastAll(go.transform.position + direction * maxHeightToCastDownFrom, -direction, maxHeightToCastDownFrom * 10, layerMask);
            RaycastHit nearestHit = new RaycastHit();
            float nearestDistance = 99999;

            foreach (RaycastHit hit in hits)
            {
                if (hit.distance < nearestDistance && hit.collider.gameObject != go
                    && !(goPainter.hideCanvas && hit.collider.gameObject == goPainter.paintCanvas))
                {
                    nearestDistance = hit.distance;
                    nearestHit = hit;
                }
            }

            if (nearestHit.collider != null && nearestHit.collider.gameObject != go)
            {
                goPainter.SetObjectPosition(go, nearestHit.point, nearestHit.normal, true);
            }
        }

        void CastObjectDown(GameObject go, int index, int length)
        {
            if (!enabled) return;
            CastObjectDownInternal(go);
        }

        void CastObjectsDown(GameObject[] gos, Vector3 axis, bool snapToGrid)
        {
            if (!enabled) return;
            if (this != null && goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }
            if (this != null && !enabled || snapToGrid)
            {
                return;
            }
            int layerMask = ~(1 << LayerMask.NameToLayer(goPainter.editLayer));

            foreach (GameObject go in gos)
            {
                RaycastHit[] hits = null;
                Vector3 direction = axis;

                if (goPainter.GetSceneCamAxisPolarity() > 0) direction *= -1;
                if (axis == Vector3.forward) direction *= -1;

                hits = Physics.RaycastAll(go.transform.position + direction * maxHeightToCastDownFrom, -direction, maxHeightToCastDownFrom * 10, layerMask);
                RaycastHit nearestHit = new RaycastHit();
                float nearestDistance = 99999;

                foreach (RaycastHit hit in hits)
                {
                    if (hit.distance < nearestDistance && hit.collider.gameObject != go
                        && !(goPainter.hideCanvas && hit.collider.gameObject == goPainter.paintCanvas))
                    {
                        nearestDistance = hit.distance;
                        nearestHit = hit;
                    }
                }

                if (nearestHit.collider != null && nearestHit.collider.gameObject != go)
                {
                    goPainter.SetObjectPosition(go, nearestHit.point, nearestHit.normal, true);
                }

            }
        }

        private void OnScene(SceneView scene)
        {
            if (!enabled) return;
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }
            if (goPainter.active == false || !goPainter.showInfoBar) return;

            float offset = 0;
            if (TryGetComponent(out GORandomizer rand) && rand.enabled)
            {
                offset = 100;
            }
            if (!goPainter.alignWithStroke && !goPainter.alignWithSurface)
            {
                offset -= 140;
            }
            Handles.BeginGUI();

            GUI.Box(new Rect(offset + 140, scene.position.height - 77, 100, 30), "");
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUI.Label(new Rect(offset + 143, scene.position.height - 70, 100, 30), "Snap2Ground");

            Handles.EndGUI();
        }
    }

}
#endif