using System;
using UnityEngine;

public enum CarvingMode
{
    Disabled,
    Carve, 
    Raise, 
    Drag
}

    public class MaskCarvingModule : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GameObject maskObject;
        [SerializeField] private Collider targetCollider;

        [Header("Brush Settings")]
        [SerializeField] private float brushSize = 0.5f;
        [SerializeField] private float brushStrength = 0.1f;
        [SerializeField] private bool useSanding = false;

        [Header("Mode Settings")]
        [SerializeField] private CarvingMode currentMode = CarvingMode.Carve;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 200f;

        private Transform brushVisual;
        private Mesh mesh;
        private Vector3[] vertices;
        private float[] perStrokeDeformation;
        private Vector3? lastDragWorldPos;

        private MeshCollider meshCollider;

        private void Awake()
        {
            Debug.LogError("MaskCarvingMode awake");
        }

        public void SetCarvingMode(CarvingMode mode)
        {
            currentMode = mode;
        }
        
        public void SetMask(GameObject mask)
        {
            maskObject = mask;
            
            CreateBrushVisual();

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
                perStrokeDeformation = new float[vertices.Length];
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
            if(currentMode == CarvingMode.Disabled)
            {
                return;
            }
            
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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = default;
            int maskLayer = LayerMask.GetMask("Mask");
            bool hitTarget = targetCollider != null && targetCollider.Raycast(ray, out hit, 100f) && ((1 << targetCollider.gameObject.layer) & maskLayer) != 0;

            if (hitTarget)
            {
                // Visual update
                if (brushVisual != null)
                {
                    brushVisual.gameObject.SetActive(true);
                    brushVisual.position = hit.point;
                    brushVisual.localScale = Vector3.one * brushSize;
                    brushVisual.up = hit.normal;
                }
            }
            else
            {
                if (brushVisual != null) brushVisual.gameObject.SetActive(false);
            }

            // Reset stroke data on mouse down
            if (Input.GetMouseButtonDown(0))
            {
                System.Array.Clear(perStrokeDeformation, 0, perStrokeDeformation.Length);
                lastDragWorldPos = hitTarget ? hit.point : (Vector3?)null;
            }

            // Continuous action
            if (Input.GetMouseButton(0) && hitTarget)
            {
                if (useSanding)
                {
                    ApplySanding(hit.point);
                }
                else
                {
                    switch (currentMode)
                    {
                        case CarvingMode.Carve:
                            ApplyDeformation(hit.point, -hit.normal); // Push inward
                            break;
                        case CarvingMode.Raise:
                            ApplyDeformation(hit.point, hit.normal);  // Pull outward
                            break;
                        case CarvingMode.Drag:
                            HandleDrag(hit.point, hit.normal);
                            break;
                    }
                }
            }
            else
            {
                lastDragWorldPos = null;
            }
        }

        private void ApplyDeformation(Vector3 hitPoint, Vector3 direction)
        {
            Vector3 localHitPoint = maskObject.transform.InverseTransformPoint(hitPoint);
            Vector3 localDir = maskObject.transform.InverseTransformDirection(direction.normalized);

            bool modified = false;

            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector3.Distance(localHitPoint, vertices[i]);
                if (distance <= brushSize)
                {
                    // Calculate influence
                    float influence = 1f - (distance / brushSize);
                    float proposedAmount = brushStrength * influence; // Default simple rate
                    
                    // We want to limit total deformation per stroke to 'brushStrength'.
                    // However, if we just use 'brushStrength' as the rate per frame, it's too fast.
                    // Let's assume brushStrength is the MAX depth per stroke, and we apply a fraction of it per frame?
                    // Or user wants "brushStrength" to be the limit.
                    // "strength kadar deforme edip bırakmalı"
                    
                    // Let's define a speed factor. For now, let's say we move 10% of strength per frame until we hit limit?
                    float moveSpeed = brushStrength * 5f * Time.deltaTime; 

                    // Check how much we already deformed this vertex this stroke
                    float currentDeformation = perStrokeDeformation[i];
                    
                    if (currentDeformation < brushStrength)
                    {
                        // Calculate allowed move
                        float remaining = brushStrength - currentDeformation;
                        float move = Mathf.Min(moveSpeed, remaining);
                        
                        // Apply move
                        vertices[i] += localDir * move;
                        perStrokeDeformation[i] += move;
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                UpdateMeshRepresentation();
            }
        }

        private void HandleDrag(Vector3 hitPoint, Vector3 hitNormal)
        {
            // Drag logic: Move vertices based on mouse movement relative to camera/surface
            // We need a stable plane to project mouse movement
            if (lastDragWorldPos == null)
            {
                lastDragWorldPos = hitPoint;
                return;
            }

            // Current world pos is hit.point, but if we deform, hit.point changes. 
            // Better to project mouse ray onto a plane defined by initial hit normal.
            Plane dragPlane = new Plane(Camera.main.transform.forward, hitPoint);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            float enter;
            if (dragPlane.Raycast(ray, out enter))
            {
                Vector3 currentWorldPos = ray.GetPoint(enter);
                Vector3 worldDelta = currentWorldPos - lastDragWorldPos.Value;
                
                // If delta is too small, ignore
                if (worldDelta.sqrMagnitude < 0.00001f) return;

                Vector3 localDelta = maskObject.transform.InverseTransformDirection(worldDelta);
                Vector3 localHitPoint = maskObject.transform.InverseTransformPoint(lastDragWorldPos.Value); // Use invalid/old hit point or current? 
                // Using current hit point for falloff feels more natural for a brush that follows surface
                // But dragging implies "grabbing". 
                
                // Let's just apply delta to vertices in range of 'hitPoint'
                localHitPoint = maskObject.transform.InverseTransformPoint(hitPoint);

                bool modified = false;
                for (int i = 0; i < vertices.Length; i++)
                {
                    float dist = Vector3.Distance(localHitPoint, vertices[i]);
                    if (dist <= brushSize)
                    {
                         // Influence
                        float influence = 1f - (dist / brushSize);
                        
                        // Apply drag
                        // Limit dragging too? User said "limit" for deformation generally. 
                        // But drag is usually direct control. I'll limit the RATE if needed, but grab usually 1:1.
                        // However, we should dampen it by strength maybe?
                        
                        vertices[i] += localDelta * influence * 0.5f; // Dampen slightly to avoid exploding mesh
                        modified = true;
                    }
                }

                if (modified)
                {
                    UpdateMeshRepresentation();
                    lastDragWorldPos = currentWorldPos;
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
    }
