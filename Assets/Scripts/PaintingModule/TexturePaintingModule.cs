using UnityEngine;

public class TexturePaintingModule : MonoBehaviour
{
    [Header("Brush Settings")]
    public Color brushColor = Color.red;
    [Range(1, 500)] public int brushSize = 50;
    [Range(0.01f, 1f)] public float brushFlow = 0.5f;
    [Range(0f, 1f)] public float brushHardness = 0.5f;

    [Header("Preview Settings")]
    public bool showPreview = true;
    public float previewSize = 0.5f;

    [Header("Sticker Settings")]
    public bool useSticker = false;
    public Texture2D stickerTexture;
    [Range(0f, 360f)] public float stickerAngle = 0f;

    private Camera _cam;
    private GameObject _previewCursor;
    private Material _previewMaterial;
    private Texture2D _defaultBrushTexture;
    
    public bool IsEnabled { get; set; }
    
    private void Start()
    {
        _cam = Camera.main;
        CreatePreviewCursor();
        CreateDefaultBrushTexture();
    }

    private void Update()
    {
        if (!IsEnabled)
        {
            return;
        }
        
        HandleInput();
        UpdatePreview();

        if (Input.GetMouseButton(0))
        {
            Paint();
        }
    }

    private void HandleInput()
    {
        if (useSticker)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                stickerAngle += scroll * 10f; // 10 degrees per scroll tick
                stickerAngle = Mathf.Repeat(stickerAngle, 360f);
            }
        }
    }

    private void CreatePreviewCursor()
    {
        _previewCursor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(_previewCursor.GetComponent<Collider>());
        _previewCursor.name = "BrushPreviewCursor";
        
        _previewMaterial = new Material(Shader.Find("Sprites/Default"));
        _previewCursor.GetComponent<Renderer>().material = _previewMaterial;
        _previewCursor.SetActive(false);
    }

    private void CreateDefaultBrushTexture()
    {
        int size = 128;
        _defaultBrushTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = Mathf.Pow(alpha, 0.5f);
                colors[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        _defaultBrushTexture.SetPixels(colors);
        _defaultBrushTexture.Apply();
    }

    private void UpdatePreview()
    {
        if (!showPreview || _cam == null)
        {
            if (_previewCursor != null) _previewCursor.SetActive(false);
            return;
        }

        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        int maskLayer = LayerMask.GetMask("Mask");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, maskLayer))
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null && (rend.material.mainTexture != null || (rend.material.shader.name.Contains("Standard") || rend.material.shader.name.Contains("Lit"))))
            {
                _previewCursor.SetActive(true);
                // Offset slightly to avoid z-fighting
                _previewCursor.transform.position = hit.point + hit.normal * 0.01f;
                _previewCursor.transform.rotation = Quaternion.LookRotation(-hit.normal);
                
                if (useSticker)
                {
                    _previewCursor.transform.Rotate(Vector3.forward, stickerAngle);
                }
                
                // Calculate correct size
                int texWidth = 1024;
                if (rend.material.mainTexture is Texture2D t2d) texWidth = t2d.width;
                
                float worldSize = CalculateBrushWorldSize(hit, texWidth);
                _previewCursor.transform.localScale = Vector3.one * worldSize;

                if (useSticker && stickerTexture != null)
                {
                    _previewMaterial.mainTexture = stickerTexture;
                    _previewMaterial.color = Color.white;
                }
                else
                {
                    _previewMaterial.mainTexture = _defaultBrushTexture;
                    _previewMaterial.color = brushColor;
                }
                return;
            }
        }

        _previewCursor.SetActive(false);
    }

    private float CalculateBrushWorldSize(RaycastHit hit, int texWidth)
    {
        // Fallback if we can't calculate: use previewSize but scaled by brushSize relative to a baseline (50)
        float fallback = previewSize * (brushSize / 50f); 

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null) return fallback;

        Mesh mesh = meshCollider.sharedMesh;
        if (!mesh.isReadable) return fallback;

        try 
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            
            int triIndex = hit.triangleIndex;
            if (triIndex < 0 || (triIndex * 3 + 2) >= triangles.Length) return fallback;

            int i0 = triangles[triIndex * 3 + 0];
            int i1 = triangles[triIndex * 3 + 1];
            int i2 = triangles[triIndex * 3 + 2];
            
            // Get World Positions
            Transform tr = hit.collider.transform;
            Vector3 p0 = tr.TransformPoint(vertices[i0]);
            Vector3 p1 = tr.TransformPoint(vertices[i1]);
            Vector3 p2 = tr.TransformPoint(vertices[i2]);
            
            // Get UV Positions
            Vector2 uv0 = uvs[i0];
            Vector2 uv1 = uvs[i1];
            Vector2 uv2 = uvs[i2];
            
            // Calculate Areas
            float worldArea = Vector3.Cross(p1 - p0, p2 - p0).magnitude * 0.5f;
            float uvArea = Mathf.Abs((uv1.x - uv0.x) * (uv2.y - uv0.y) - (uv2.x - uv0.x) * (uv1.y - uv0.y)) * 0.5f;
            
            if (uvArea < 1e-6f) return fallback;
            
            // Ratio of World Size to UV Size (roughly linear scale)
            float textureWorldScale = Mathf.Sqrt(worldArea / uvArea);
            
            // Brush size is in pixels.
            // UV size = brushSize / texWidth.
            float brushUVSize = brushSize / (float)texWidth;
            
            return brushUVSize * textureWorldScale;
        }
        catch
        {
            return fallback;
        }
    }

    private void Paint()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        int maskLayer = LayerMask.GetMask("Mask");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, maskLayer))
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            // Texture coordinates are only available on MeshCollider
            if (rend == null || hit.collider as MeshCollider == null)
                return;

            Texture2D tex = GetTexture(rend);
            if (tex == null) return;

            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x *= tex.width;
            pixelUV.y *= tex.height;

            if (useSticker && stickerTexture != null)
            {
                ApplySticker(tex, pixelUV);
            }
            else
            {
                ApplyBrush(tex, pixelUV);
            }
        }
    }

    private Texture2D GetTexture(Renderer rend)
    {
        Material mat = rend.material;
        Texture mainTex = mat.mainTexture;

        if (mainTex == null)
        {
            return CreateBaseTexture(rend);
        }

        if (mainTex is Texture2D t2d)
        {
            // If the texture name is "RuntimePainted", we assume it's already our runtime instance.
            if (t2d.name == "RuntimePainted")
                return t2d;

            // Otherwise, we need to clone it to avoid modifying the original asset 
            // and ensure it's read/write enabled
            return CreateTextureInstance(rend, t2d);
        }
        
        return null;
    }

    private Texture2D CreateBaseTexture(Renderer rend)
    {
        int width = 1024;
        int height = 1024;
        Texture2D newTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        
        newTex.SetPixels(colors);
        newTex.name = "RuntimePainted";
        newTex.Apply();
        
        rend.material.mainTexture = newTex;
        return newTex;
    }

    private Texture2D CreateTextureInstance(Renderer rend, Texture2D source)
    {
        Texture2D newTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        
        try
        {
            newTex.SetPixels(source.GetPixels());
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Texture '{source.name}' on '{rend.name}' is not readable. Creating a blank white texture for painting.");
            Color[] colors = new Color[source.width * source.height];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            newTex.SetPixels(colors);
        }
        
        newTex.name = "RuntimePainted";
        newTex.Apply();
        
        rend.material.mainTexture = newTex;
        return newTex;
    }

    private void ApplySticker(Texture2D tex, Vector2 centerPixel)
    {
        if (stickerTexture == null) return;

        int r = brushSize / 2;
        int cx = (int)centerPixel.x;
        int cy = (int)centerPixel.y;
        
        int startX = Mathf.Clamp(cx - r, 0, tex.width);
        int startY = Mathf.Clamp(cy - r, 0, tex.height);
        int endX = Mathf.Clamp(cx + r, 0, tex.width);
        int endY = Mathf.Clamp(cy + r, 0, tex.height);
        
        int width = endX - startX;
        int height = endY - startY;
        
        if (width <= 0 || height <= 0) return;

        Color[] pixels = tex.GetPixels(startX, startY, width, height);
        bool modified = false;

        float angleRad = stickerAngle * Mathf.Deg2Rad;
        float cosA = Mathf.Cos(angleRad);
        float sinA = Mathf.Sin(angleRad);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Current pixel position relative to center
                float relX = (startX + x) - cx;
                float relY = (startY + y) - cy;

                // Rotate 
                float rotX = relX * cosA - relY * sinA;
                float rotY = relX * sinA + relY * cosA;

                // Normalize to 0-1 range for sticker UV
                float u = (rotX / brushSize) + 0.5f;
                float v = (rotY / brushSize) + 0.5f;

                if (u >= 0 && u <= 1 && v >= 0 && v <= 1)
                {
                    Color stickerColor = stickerTexture.GetPixelBilinear(u, v);
                    float alpha = stickerColor.a * brushFlow;
                    
                    if (alpha > 0)
                    {
                        int idx = y * width + x;
                        pixels[idx] = Color.Lerp(pixels[idx], stickerColor, alpha);
                        modified = true;
                    }
                }
            }
        }
        
        if (modified)
        {
            tex.SetPixels(startX, startY, width, height, pixels);
            tex.Apply();
        }
    }

    private void ApplyBrush(Texture2D tex, Vector2 centerPixel)
    {
        int r = brushSize / 2;
        int cx = (int)centerPixel.x;
        int cy = (int)centerPixel.y;
        
        int startX = Mathf.Clamp(cx - r, 0, tex.width);
        int startY = Mathf.Clamp(cy - r, 0, tex.height);
        int endX = Mathf.Clamp(cx + r, 0, tex.width);
        int endY = Mathf.Clamp(cy + r, 0, tex.height);
        
        int width = endX - startX;
        int height = endY - startY;
        
        if (width <= 0 || height <= 0) return;

        Color[] pixels = tex.GetPixels(startX, startY, width, height);
        
        if (ProcessPixels(pixels, width, height, startX, startY, centerPixel, r))
        {
            tex.SetPixels(startX, startY, width, height, pixels);
            tex.Apply();
        }
    }

    private bool ProcessPixels(Color[] pixels, int width, int height, int startX, int startY, Vector2 centerPixel, int radius)
    {
        bool modified = false;
        
        // Pre-calculate hardness values
        float hardRadius = radius * brushHardness;
        float fadeRange = radius - hardRadius;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int px = startX + x;
                int py = startY + y;
                
                float distance = Vector2.Distance(new Vector2(px, py), centerPixel);
                
                if (distance <= radius)
                {
                    float alpha = CalculateAlpha(distance, hardRadius, fadeRange);
                    
                    if (alpha > 0)
                    {
                        int idx = y * width + x;
                        pixels[idx] = Color.Lerp(pixels[idx], brushColor, alpha);
                        modified = true;
                    }
                }
            }
        }
        return modified;
    }

    private float CalculateAlpha(float distance, float hardRadius, float fadeRange)
    {
        float alpha = 1f;
        if (distance > hardRadius)
        {
            // If fadeRange is basically zero (hardness 1), we shouldn't really be here due to distance checks
            // unless strict inequality floating point issues, but just safe guard.
            if (fadeRange < 0.001f) return 0f;
            
            float distInFade = distance - hardRadius;
            alpha = 1f - (distInFade / fadeRange);
        }
        
        alpha *= brushFlow;
        return Mathf.Clamp01(alpha);
    }
}
