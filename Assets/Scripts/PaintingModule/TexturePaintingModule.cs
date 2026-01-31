using UnityEngine;

public class TexturePaintingModule : MonoBehaviour
{
    [Header("Brush Settings")]
    public Color brushColor = Color.red;
    [Range(1, 500)] public int brushSize = 50;
    [Range(0.01f, 1f)] public float brushFlow = 0.5f;
    [Range(0f, 1f)] public float brushHardness = 0.5f;

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Paint();
        }
    }

    private void Paint()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
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

            ApplyBrush(tex, pixelUV);
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
