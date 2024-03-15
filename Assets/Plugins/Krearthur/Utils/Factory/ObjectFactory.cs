#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Krearthur.Utils;

namespace Krearthur.GOP
{
    /// <summary>
    /// Base class that is able to manage creation and deletion of multiple objects. Think of it like a factory that can create a line of products.
    /// </summary>
    [ExecuteInEditMode]
    public class ObjectFactory : MonoBehaviour
    {
        [HideInInspector] public GameObject objectToCreate;
        [HideInInspector] public Transform group;
        protected MeshRenderer meshRenderer;
        protected MeshFilter meshFilter;

        [HideInInspector] public bool relativePos = true;
        [Tooltip("Append number of charge to instantiaed name: <ObjectName>_<charge> e.g. MyObject_1_3")]
        public bool appendChargeNumber = true;
        [Tooltip("Append number of charge + object index to instantiated name: <ObjectName>_<charge>_<index> e.g. MyObject_1_3")]
        public bool appendIndexNumber = true;
        [HideInInspector] public bool createUnderGroup = false;

        protected List<GameObject> products;
        protected int lastActiveIndex = 0;

        [HideInInspector] public Vector3 positionOffset = new Vector3(0, 0, 0);
        [HideInInspector] public Vector3 objectEuler = Vector3.zero;
        [HideInInspector] public Vector3 objectScale = Vector3.one;

        [Tooltip("Limit the maximum objects to prevent performance loss / crashes.")]
        public int objectLimit = 5000;

        [HideInInspector] public Vector3 upAxis = Vector3.up;
        protected GameObject temp;

        [Tooltip("Current product batch. Increments after each Rect, Circle or Line placements.")]
        public int charge;

        protected int origLayer;

        protected GOPainter gop;

        private void Start()
        {
            // does nothing
        }

        private void Awake()
        {
            gop = GetComponent<GOPainter>();
        }

        /// <summary>
        /// Called when the holded object is produced.
        /// GameObject: The object that is produced.
        /// first int: the index of the object in the products array
        /// second int: the length of the products array
        /// </summary>
        public event Action<GameObject, int, int> OnObjectProduced;
        public event Action<GameObject, int, int> OnObjectUpdated;
        public event Action<GameObject, int, int> OnObjectProducedLate;
        public event Action<GameObject, int, int> OnObjectUpdatedLate;

        public virtual GameObject Produce(Vector3 position)
        {
            if (products == null)
            {
                products = new List<GameObject>();
            }

            GameObject product = CreateFromTemplate(objectToCreate, position);

            products.Add(product);
            return product;
        }

        protected GameObject CreateFromTemplate(GameObject template, Vector3 position)
        {
            if (gop == null) gop = GetComponent<GOPainter>();

            GameObject product = null;
            if (template.IsPrefab() && !Application.isPlaying)
            {
                product = PrefabUtility.InstantiatePrefab(template.GetPrefabAsset()) as GameObject;
            }
            else
            {
                product = Instantiate(template, CalcPos(position), template.transform.rotation);
            }

            product.transform.position = CalcPos(position);
            product.transform.rotation = template.transform.rotation;
            origLayer = product.layer;
            product.layer = LayerMask.NameToLayer(gop.editLayer);

            if (objectEuler != Vector3.zero)
            {
                Transform center = product.transform.Find("Center");
                if (center != null)
                {
                    center.eulerAngles = objectEuler;
                }
                else
                {
                    product.transform.eulerAngles = objectEuler;
                }
            }
            product.transform.localScale = objectScale;

            // Fix naming
            if (product.name.StartsWith(gop.PickedPrefix))
            {
                product.name = product.name.Substring(gop.PickedPrefix.Length);
            }
            if (product.name.EndsWith("(Clone)"))
            {
                product.name = product.name.Substring(0, product.name.Length - "(Clone)".Length);
            }

            string appendix = "";
            if (appendChargeNumber) appendix += "_" + charge;
            if (appendIndexNumber) appendix += "_" + products.Count;

            product.name += appendix;

            if (createUnderGroup)
            {
                if (group == null) product.transform.SetParent(this.transform, true);
                else product.transform.SetParent(group, true);
            }

            return product;
        }

        protected virtual Vector3 CalcPos(Vector3 position)
        {
            Vector3 pos = position + positionOffset;
            if (relativePos)
            {
                pos += transform.position;
            }
            return pos;
        }

        public GameObject ProduceOrUpdate(Vector3 position)
        {
            GameObject product = GetAt(0);
            if (product == null)
            {
                product = Produce(position);
            }
            else
            {
                product.transform.position = CalcPos(position);
                product.SetActive(true);
            }

            return product;
        }

        public void MassProduceOrUpdate(Vector3[] positions) { MassProduceOrUpdate(positions, null, Vector3.zero); }
        public void MassProduceOrUpdate(Vector3[] positions, Vector3 direction) { MassProduceOrUpdate(positions, null, direction); }
        public void MassProduceOrUpdate(Vector3[] positions, Vector3[] normals) { MassProduceOrUpdate(positions, normals, Vector3.zero); }
        /// <summary>
        /// Updates the products positions. If there are not enough products yet, produce the missing ones.
        /// If there are too many, deactivate the overlapping.
        /// </summary>
        /// <param name="positions">positions of objects to create or update</param>
        /// <param name="normals">normal directions where objects should look at. Overrides lookDir param. Can be null</param>
        /// <param name="lookDir">direction where objects should look at. Set to Vector3.zero to ignore</param>
        public void MassProduceOrUpdate(Vector3[] positions, Vector3[] normals, Vector3 lookDir)
        {
            if (positions.Length > objectLimit)
            {
                Array.Resize<Vector3>(ref positions, objectLimit);
            }
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject product = GetAt(i);
                bool newlyProduced = false;

                if (product == null)
                { // Produce new product 
                    product = Produce(positions[i]);
                    newlyProduced = true;
                }
                else
                { // Update product
                    product.transform.position = CalcPos(positions[i]);
                    product.SetActive(true);
                    lastActiveIndex = i;
                }

                if (normals != null)
                {
                    lookDir = normals[i];
                }
                if (lookDir != Vector3.zero)
                {
                    product.transform.rotation = Quaternion.LookRotation(lookDir, upAxis);
                    if (objectEuler != Vector3.zero)
                    {
                        product.transform.eulerAngles += objectEuler;
                    }
                    else
                    {
                        product.transform.eulerAngles += objectToCreate.transform.eulerAngles;
                    }
                }

                if (newlyProduced)
                {
                    OnObjectProduced?.Invoke(product, i, positions.Length);
                    product = products[i];
                    OnObjectProducedLate?.Invoke(product, i, positions.Length);
                } else
                {
                    OnObjectUpdated?.Invoke(product, i, positions.Length);
                    product = products[i];
                    OnObjectUpdatedLate?.Invoke(product, i, positions.Length);
                }
                
            }

            // Deactivate overlapping products
            if (products.Count > positions.Length)
            {
                for (int i = positions.Length; i < products.Count; i++)
                {
                    products[i].SetActive(false);
                    lastActiveIndex = i - 1;
                }
            }
        }

        /// <summary>
        /// Creates or updates n objects at the provided positions. n is the size of the array.
        /// </summary>
        /// <param name="positions"></param>
        public void MassProduce(Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                Produce(positions[i]);
            }
        }

        public void UpdateAllProductsRotation(Vector3 eulerAngles)
        {
            if (eulerAngles != Vector3.zero)
            {
                if (products == null) return;
                foreach (GameObject product in products)
                {
                    if (product == null) continue;
                    Transform center = product.transform.Find("Center");
                    if (center != null)
                    {
                        center.eulerAngles = eulerAngles;
                    }
                    else
                    {
                        product.transform.eulerAngles = eulerAngles;
                    }
                }
                objectEuler = eulerAngles;
            }
        }

        /// <summary>
        ///  Takes product at index, deletes it, and creates a new product via objectReplacement and puts it in the products array.
        /// </summary>
        public void SwapProductObject(int index, GameObject objectTemplate)
        {
            if (products == null || products.Count == 0 || index < 0 || index >= products.Count)
            {
                Debug.LogWarning("index " + index + " out of bounds or products is empty");
            }
            GameObject currentProduct = products[index];
            Vector3 position = currentProduct.transform.position;
            DestroyImmediate(currentProduct);
            products[index] = CreateFromTemplate(objectTemplate, position);

        }

        public GameObject GetAt(int index)
        {
            if (products == null || products.Count == 0) return null;
            if (index >= products.Count) return null;

            return products[index];
        }

        public void DeactivateProducts()
        {
            if (products == null) return;

            foreach (GameObject p in products)
            {
                p.SetActive(false);
            }
            lastActiveIndex = 0;
        }

        public void DestroyAll()
        {
            if (products == null) return;

            if (Application.isPlaying)
            {
                foreach (GameObject p in products)
                {
                    Destroy(p);
                }
            }
            else
            {
                foreach (GameObject p in products)
                {
                    DestroyImmediate(p);
                }
            }

            products.Clear();
        }

        public void ActivateProducts()
        {
            if (products == null) return;

            foreach (GameObject p in products)
            {
                if (p != null)
                    p.SetActive(true);
            }
        }

        public List<GameObject> GetAllProducts()
        {
            return products;
        }

        public List<GameObject> GetActiveProducts()
        {
            return products.FindAll(gameObject => gameObject != null && gameObject.activeSelf);
        }

        public GameObject RemoveFirst()
        {
            if (products == null || products.Count == 0) return null;

            GameObject destroyed = products[0];
            if (Application.isPlaying)
            {
                Destroy(destroyed);
            }
            else
            {
                DestroyImmediate(destroyed);
            }
            products.RemoveAt(0);

            return destroyed;
        }

        public GameObject RemoveLast()
        {
            if (products == null || products.Count == 0) return null;

            GameObject destroyed = products[products.Count - 1];
            if (Application.isPlaying)
            {
                Destroy(destroyed);
            }
            else
            {
                DestroyImmediate(destroyed);
            }
            products.RemoveAt(products.Count - 1);

            return destroyed;
        }

        public virtual float GetAvgDimension()
        {
            if (objectToCreate.TryGetComponent(out IGridTransform gTransform))
            {
                return gTransform.SizeX;
            }
            else
            {
                float size = 1;
                Collider col = objectToCreate.GetComponentInChildren<Collider>();
                if (col != null)
                {
    
                    size = GetOrCreateTemp().GetComponentInChildren<Collider>().bounds.size.x;
                }
                else
                {
                    MeshFilter mf = objectToCreate.GetComponentInChildren<MeshFilter>();
                    if (mf != null)
                    {
                        size = mf.sharedMesh.bounds.size.x;
                    }
                    
                }
                if (size <= 0) size = 1;
                return size;
            }
        }

        protected GameObject GetOrCreateTemp()
        {
            if (temp == null || temp.GetPrefabAsset() != null && objectToCreate != temp.GetPrefabAsset())
            {
                if (objectToCreate.IsPrefab())
                {
                    temp = PrefabUtility.InstantiatePrefab(objectToCreate) as GameObject;
                    temp.transform.position = new Vector3(1000, 1000, 1000);
                }
                else
                {
                    temp = Instantiate(objectToCreate, new Vector3(1000, 1000, 1000), Quaternion.identity);
                }
                temp.name = "[temp]";
                temp.AddComponent<Marker>().typeCode = MarkerCode.MarkForDestruction;
            }
            temp.SetActive(true);
            return temp;
        }

        public int Count()
        {
            return products == null ? 0 : products.Count;
        }

        /// <summary>
        /// Releases all products, meaning the factory cuts the connection to them and can start a new line of products
        /// </summary>
        public void Release()
        {
            if (products != null)
            {
                foreach (GameObject go in products)
                {
                    if (go != null)
                    {
                        go.layer = origLayer;
                    }
                }
            }
            products = null;
            Marker[] temps = FindObjectsOfType<Marker>();
            foreach(Marker temp in temps)
            {
                if (temp.typeCode == MarkerCode.MarkForDestruction) DestroyImmediate(temp.gameObject);
            }
            charge++;
        }

        public GameObject GetLatest()
        {
            if (products != null && products.Count > 0)
            {
                for (int i = products.Count - 1; i >= 0; i--)
                {
                    if (products[i].activeSelf) return products[i];
                }
            }
            return null;
        }

        public bool IsEmpty()
        {
            return products == null || products.Count == 0;
        }
    }
}
#endif