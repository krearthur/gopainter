using UnityEngine;
using UnityEditor;
using Krearthur.Utils;

namespace Krearthur.GOP
{
    /// <summary>
    /// Randomizes properties of painted objects.
    /// </summary>
    [ExecuteInEditMode]
    public class GORandomizer : MonoBehaviour, GOComponent
    {
#if UNITY_EDITOR
        protected GOPainter goPainter;
#endif
        public bool randomPosition = false;
        public bool individualAxisPos = true;

        public Vector3 minPositionDelta = new Vector3(-1, 0, -1);
        public Vector3 maxPositionDelta = new Vector3(1, 0, 1);

        public bool randomRotation = false;
        public bool individualAxisRot = true;

        [Tooltip("Minimum rotation change in local euler")]
        public Vector3 minEulerDelta = new Vector3(0, -180, 0);
        [Tooltip("Maximum rotation change in local euler")]
        public Vector3 maxEulerDelta = new Vector3(0, 180, 0);
        [Tooltip("Rotation degrees will be a multiple of this value. Set to 0 to disable. E.g. when you put in y=90 it means it will either be 90, 180, 270 or 360 degree. Only values between 0 - 360 are valid.")]
        public Vector3 fixRotationStep = Vector3.zero;
        public bool randomScale = false;
        public bool individualAxisScale = false;
        public Vector3 minScaleDelta = new Vector3(-0.5f, -0.5f, -0.5f);
        public Vector3 maxScaleDelta = new Vector3(0, 0, 0);

        public bool brightnessVariation = false;
        public bool dontChangeAlpha = true;
        public float darkestValue = 0.8f;
        public float brightestValue = 1.2f;

        public bool randomizeChildren = true;

        MaterialPropertyBlock propBlock;
        private void Start(){}

        private void Awake()
        {
#if UNITY_EDITOR
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }
#endif
            propBlock = new MaterialPropertyBlock();

            Marker[] markers = GameObject.FindObjectsOfType<Marker>();
            if (markers == null) return;
            foreach (Marker marker in markers)
            {
                DoColorVariateCheck(marker.gameObject);
            }
        }

        public void Register()
        {

#if UNITY_EDITOR
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            goPainter.OnObjectPainted += RandomizeTransform;
            goPainter.OnObjectMassPainted += RandomizeMass;
            SceneView.duringSceneGui += OnScene;
#endif

        }

        public void DeRegister()
        {

#if UNITY_EDITOR
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            goPainter.OnObjectPainted -= RandomizeTransform;
            goPainter.OnObjectMassPainted -= RandomizeMass;
            SceneView.duringSceneGui -= OnScene;
#endif
        }


        void RandomizeMass(GameObject[] gos, Vector3 axis, bool snapToGrid)
        {
            if (!enabled) return;
            foreach(GameObject go in gos)
            {
                RandomizeTransform(go, axis, snapToGrid);
            }
        }

        void RandomizeTransform(GameObject go, Vector3 axis, bool snapToGrid)
        {
            if (!enabled) return;
            
            if (randomPosition)
            {
                Vector3 randomPos = new Vector3(
                    Mathf.Lerp(minPositionDelta.x, maxPositionDelta.x, Random.value),
                    Mathf.Lerp(minPositionDelta.y, maxPositionDelta.y, Random.value),
                    Mathf.Lerp(minPositionDelta.z, maxPositionDelta.z, Random.value));
                
                if (!individualAxisPos)
                {
                    randomPos = Vector3.Lerp(minPositionDelta, maxPositionDelta, Random.value);
                }

                go.transform.position += randomPos;
            }

            if (randomRotation)
            {
                Vector3 randomEuler = new Vector3(
                    Mathf.Lerp(minEulerDelta.x, maxEulerDelta.x, Random.value),
                    Mathf.Lerp(minEulerDelta.y, maxEulerDelta.y, Random.value),
                    Mathf.Lerp(minEulerDelta.z, maxEulerDelta.z, Random.value));

                if (!individualAxisRot)
                {
                    randomEuler = Vector3.Lerp(minEulerDelta, maxEulerDelta, Random.value);
                }

                if (fixRotationStep != Vector3.zero)
                {
                    fixRotationStep.x = Mathf.Clamp(fixRotationStep.x, 0, 360);
                    fixRotationStep.y = Mathf.Clamp(fixRotationStep.y, 0, 360);
                    fixRotationStep.z = Mathf.Clamp(fixRotationStep.z, 0, 360);

                    int xRolls = (int)(360 / fixRotationStep.x);
                    int yRolls = (int)(360 / fixRotationStep.y);
                    int zRolls = (int)(360 / fixRotationStep.z);

                    randomEuler = new Vector3(
                        fixRotationStep.x * (int)(xRolls * Random.value),
                        fixRotationStep.y * (int)(yRolls * Random.value),
                        fixRotationStep.z * (int)(zRolls * Random.value));
                }

                //go.transform.localEulerAngles += randomEuler;
                go.transform.Rotate(randomEuler, Space.Self);
            }

            if (randomScale)
            {
                Vector3 randomScale = new Vector3(
                    Mathf.Lerp(minScaleDelta.x, maxScaleDelta.x, Random.value),
                    Mathf.Lerp(minScaleDelta.y, maxScaleDelta.y, Random.value),
                    Mathf.Lerp(minScaleDelta.z, maxScaleDelta.z, Random.value));

                if (!individualAxisScale)
                {
                    randomScale = Vector3.Lerp(minScaleDelta, maxScaleDelta, Random.value);
                }

                Mesh m = go.GetComponentInChildren<MeshFilter>().sharedMesh;

                Vector3 before = m.bounds.extents;
                before.x *= go.transform.localScale.x;
                before.y *= go.transform.localScale.y;
                before.z *= go.transform.localScale.z;

                Vector3 centerToPivotBefore = m.bounds.center;

                Vector3 s = go.transform.localScale;
                go.transform.localScale = new Vector3(s.x * (1 + randomScale.x), s.y * (1 + randomScale.y), s.z * (1 + randomScale.z));
                m.RecalculateBounds();
                Physics.SyncTransforms();

                Vector3 centerToPivotAfter = m.bounds.center;

                Vector3 after = m.bounds.extents;
                after.x *= go.transform.localScale.x;
                after.y *= go.transform.localScale.y;
                after.z *= go.transform.localScale.z;
                float nonCenterFactor = 1;
                if (m.bounds.center != Vector3.zero)
                {
                    nonCenterFactor = 1 - (m.bounds.center.y / m.bounds.extents.y);
                }

                if (axis == Vector3.up)
                {
                    // move it yDelta/2 down/up
                    Vector3 delta = (before - after);
                    delta.x = delta.z = 0;
                    delta.y *= nonCenterFactor;
                    go.transform.position -= delta;
                }
            }
            
            if (brightnessVariation)
            {
                DoRandomBrightness(go);
            }

        }

        void DoColorVariateCheck(GameObject go)
        {
            if (!enabled) return;
            if (go.TryGetComponent(out Renderer renderer) && go.TryGetComponent(out Marker marker) && marker.typeCode == MarkerCode.RandomColor)
            {
                RandomColor(renderer);
            }
            if (go.transform.childCount > 0)
            {
                foreach (Transform child in go.transform)
                {
                    DoColorVariateCheck(child.gameObject);
                }
            }
        }

        void DoRandomBrightness(GameObject go)
        {
            if (!enabled) return;
            if (go.TryGetComponent(out Renderer renderer))
            {
                RandomColor(renderer);
            }
            if (randomizeChildren && go.transform.childCount > 0)
            {
                foreach (Transform child in go.transform)
                {
                    DoRandomBrightness(child.gameObject);
                }
            }
        }

        void RandomColor(Renderer renderer)
        {
            if (!enabled) return;
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(propBlock);
            float alphaBefore = renderer.sharedMaterial.color.a;
            Color color = renderer.sharedMaterial.color * Mathf.Lerp(darkestValue, brightestValue, Random.value);

            if (renderer.gameObject.TryGetComponent(out Marker marker) == false || marker.typeCode != MarkerCode.RandomColor)
            {
                renderer.gameObject.AddComponent<Marker>().typeCode = MarkerCode.RandomColor;
            }
            
            if (dontChangeAlpha)
            {
                color.a = alphaBefore;
            }
            propBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propBlock);
        }

#if UNITY_EDITOR
        private void OnScene(SceneView scene)
        {
            if (!enabled) return;
            if (goPainter == null)
            {
                goPainter = GetComponent<GOPainter>();
            }

            if (goPainter.active == false || !goPainter.showInfoBar) return;

            Handles.BeginGUI();
            float offset = 0;
            if (!goPainter.alignWithStroke && !goPainter.alignWithSurface)
            {
                offset -= 140;
            }

            GUI.Box(new Rect(offset + 140, scene.position.height - 77, 100, 30), "");
            GUI.skin.label.fontSize = 13;
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUI.Label(new Rect(offset + 155, scene.position.height - 70, 100, 30), "Randomize");

            Handles.EndGUI();
        }
#endif
    }
}