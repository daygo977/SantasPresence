using UnityEngine;

public class Fog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("World bounds (XZ)")]
    //(x min, z min)
    [SerializeField] private Vector2 worldMin = new Vector2(-50f, -50f);
    //(x max, z max)
    [SerializeField]private Vector2 worldMax = new Vector2(50f, 50f);

    [Header("Fog Settings")]
    //Resolution of fog texture
    [SerializeField] private int textureSize = 512;
    //Reveal readius in world units
    [SerializeField] private float revealRadius = 5f;

    private Texture2D fogTexture;
    private Color[] pixels;

    void Start()
    {
        //Create texture
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.R8, false, true);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;

        //Fill with black, unvisited
        pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0f, 0f, 0f, 1f);
        }
        fogTexture.SetPixels(pixels);
        fogTexture.Apply();

        //Send to shaders as globals
        Shader.SetGlobalTexture("_FogTexture", fogTexture);
        Shader.SetGlobalVector("_Fog_WorldMin", new Vector4(worldMin.x, worldMin.y, 0f, 0f));
        Shader.SetGlobalVector("_Fog_WorldMax", new Vector4(worldMax.x, worldMax.y, 0f, 0f));
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 pos = player.position;

        //Clamp inside bounds just in case
        float clampedX = Mathf.Clamp(pos.x, worldMin.x, worldMax.x);
        float clampedY = Mathf.Clamp(pos.y, worldMin.y, worldMax.y);

        //World XZ to 0...1 UV
        float u = Mathf.InverseLerp(worldMin.x, worldMax.x, clampedX);
        float v = Mathf.InverseLerp(worldMin.y, worldMax.y, clampedY);

        //UV to texture coordinates
        int centerX = Mathf.RoundToInt(u * (textureSize-1));
        int centerY = Mathf.RoundToInt(v * (textureSize-1));

        //Convert world radius to texture radius
        float worldWidth = worldMax.x - worldMin.x;
        float texPixelsPerUnit = textureSize / Mathf.Max(worldWidth, 0.0001f);
        int radius = Mathf.Max(1, Mathf.RoundToInt(revealRadius * texPixelsPerUnit));
        int sqrRadius = radius * radius;

        //Paint white circle, visited
        for (int )
    }
}
