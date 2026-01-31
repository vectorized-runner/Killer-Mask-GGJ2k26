using UnityEngine;

namespace CarvingModule
{
    public class MaskCarvingModule : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GameObject maskObject;
        [SerializeField] private Collider targetCollider;

        [Header("Brush Settings")]
        [SerializeField] private float brushSize = 0.5f;
        [SerializeField] private float brushStrength = 0.01f;
        [SerializeField] private bool useSanding = false;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 200f;

        private Transform brushVisual;
        private Mesh mesh;
        private Vector3[] vertices;
        private MeshCollider meshCollider;

        private void Start()
        {
            CreateBrushVisual();

            if (maskObject == null)
                maskObject = gameObject;

            MeshFilter mf = maskObject.GetComponent<MeshFilter>();
            if (mf != null)
            {
                mesh = mf.mesh;

                if (!mesh.isReadable)
                {
                    Debug.LogError($"[MaskCarvingModule] '{maskObject.name}' nesnesindeki '{mesh.name}' mesh'i okunabilir değil! Lütfen modelin Import Settings ayarlarından 'Read/Write Enabled' seçeneğini işaretleyip Apply diyiniz.");
                    enabled = false;
                    return;
                }

                mesh.MarkDynamic();
                vertices = mesh.vertices;
            }

            if (targetCollider == null)
            {
                targetCollider = maskObject.GetComponent<Collider>();
            }

            // Ensure we have a physics way to raycast against the mesh
            if (targetCollider is MeshCollider mc)
            {
                meshCollider = mc;
            }
        }

        private void Update()
        {
            HandleRotation();
            HandleCarving();
        }

        private void HandleRotation()
        {
            // Rotate with Right Mouse Button
            if (Input.GetMouseButton(1))
            {
                float rotX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                float rotY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

                maskObject.transform.Rotate(Vector3.up, -rotX, Space.World);
                maskObject.transform.Rotate(Vector3.right, rotY, Space.World);
            }
        }

        private void HandleCarving()
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast against the mask
            if (targetCollider != null && targetCollider.Raycast(ray, out hit, 100f))
            {
                // Visual update
                if (brushVisual != null)
                {
                    brushVisual.gameObject.SetActive(true);
                    brushVisual.position = hit.point;
                    brushVisual.localScale = Vector3.one * brushSize;
                    brushVisual.up = hit.normal;
                }

                // Carving action
                if (Input.GetMouseButton(0))
                {
                    if (useSanding)
                    {
                        ApplySanding(hit.point);
                    }
                    else
                    {
                        ApplyCarving(hit.point, hit.normal);
                    }
                }
            }
            else
            {
                if (brushVisual != null) brushVisual.gameObject.SetActive(false);
            }
        }

        private void ApplyCarving(Vector3 hitPoint, Vector3 hitNormal)
        {
            Vector3 localHitPoint = maskObject.transform.InverseTransformPoint(hitPoint);
            Vector3 localNormal = maskObject.transform.InverseTransformDirection(hitNormal);

            bool modified = false;

            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector3.Distance(localHitPoint, vertices[i]);
                if (distance <= brushSize)
                {
                    // Calculate influence
                    float influence = 1f - (distance / brushSize);
                    
                    // Push vertex inward (opposite to normal)
                    vertices[i] -= localNormal * (brushStrength * influence);
                    modified = true;
                }
            }

            if (modified)
            {
                UpdateMeshRepresentation();
            }
        }

        private void ApplySanding(Vector3 hitPoint)
        {
            Vector3 localHitPoint = maskObject.transform.InverseTransformPoint(hitPoint);
            
            // Calculate average position in existing vertices within brush
            Vector3 avgPos = Vector3.zero;
            int count = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (Vector3.Distance(localHitPoint, vertices[i]) <= brushSize)
                {
                    avgPos += vertices[i];
                    count++;
                }
            }

            if (count > 0)
            {
                avgPos /= count;
                bool modified = false;

                for (int i = 0; i < vertices.Length; i++)
                {
                    float distance = Vector3.Distance(localHitPoint, vertices[i]);
                    if (distance <= brushSize)
                    {
                        float influence = 1f - (distance / brushSize);
                        // Move efficiently towards average
                        vertices[i] = Vector3.Lerp(vertices[i], avgPos, brushStrength * influence);
                        modified = true;
                    }
                }

                if (modified)
                {
                    UpdateMeshRepresentation();
                }
            }
        }

        private void UpdateMeshRepresentation()
        {
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Force update collider if it's a mesh collider so subsequent raycasts are accurate
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = mesh;
            }
        }

        // Public API for UI
        public void SetSanding(bool active) => useSanding = active;
        public void SetBrushSize(float size) => brushSize = size;
        public void SetBrushStrength(float strength) => brushStrength = strength;

        private void CreateBrushVisual()
        {
            if (brushVisual == null)
            {
                // Create a primitive sphere
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "BrushVisual";
                
                // Remove collider so it doesn't interfere with raycasts
                if(sphere.TryGetComponent<Collider>(out var collider))
                    Destroy(collider);
                
                // Optional: Set transparent material
                var renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = new Color(1f, 0f, 0f, 0.5f);
                }

                brushVisual = sphere.transform;
                brushVisual.SetParent(transform);
                brushVisual.gameObject.SetActive(false);
            }
        }
    }
}
