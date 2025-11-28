using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Fog of war volume, surrounds level/map as a cube like shape.
/// Will store 2D "history" texture in XZ (top-down) and remove
/// cylinders of visibility from floor to ceilling around the player.
/// Once area/pixel is revealed, it will stay clear (no re-fogging).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class FogOfWar : MonoBehaviour
{
    [Header("Debug (auto-filled)")]
    [SerializeField] private Vector2 worldBottomLeft; //X, Z minimum of the fog volume
    [SerializeField] private Vector2 worldSize;       //width/height of the fog volume in X,Z

    [Header("Fog volume settings")]
    [Tooltip("Resolution of the fog texture (width = height). Higher is sharper, but more computation")]
    public int textureSize = 512;

    [Tooltip("Reveal radius in world units (XZ plane)")]
    public float revealRadius = 5f;

    [Header("Player")]
    [Tooltip("Transform to reveal fog around player (usually root)")]
    public Transform player;

    private Texture2D fogTexture;
    private Color32[] pixels;
    private Renderer rend;
    private bool pixelsChanged = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    void Start()
    {
        //Get world-space bounds of fog cube.
        //We only get X and Z, Y is not needed
        var bounds = rend.bounds;
        worldBottomLeft = new Vector2(bounds.min.x, bounds.min.z);
        worldSize = new Vector2(bounds.size.x, bounds.size.z);

        //Create alpha-only texture to store fog mask
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.Alpha8, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        pixels = new Color32[textureSize * textureSize];

        //Start fully fogged, alpha = 255
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 255);
        }

        fogTexture.SetPixels32(pixels);
        fogTexture.Apply();

        //Give the material texture and world bounds
        //Shader will use these to map world XZ to UV to alpha
        var mat = rend.material;
        mat.mainTexture = fogTexture;

        mat.SetVector("_WorldBottomLeft", new Vector4(worldBottomLeft.x, worldBottomLeft.y, 0f, 0f));
        mat.SetVector("_WorldSize", new Vector4(worldSize.x, worldSize.y, 0f, 0f));

    }

    void Update()
    {
        if (player == null)
            return;

        //Reveal fog  in the radius around the player's current position
        RevealAtPosition(player.position);

        // If true, upload pixels to GPU if something changed
        if (pixelsChanged)
        {
            fogTexture.SetPixels32(pixels);
            fogTexture.Apply();
            pixelsChanged = false;
        }
    }

    /// <summary>
    /// Clears/Reveals fog in circular area around the set world position (player).
    /// Creates a vertical cylinder of visibilty from the floor to ceilling of the fog cube at that position
    /// 
    /// Once the are is revealed, it stays revealed, which leads to a revealed path.
    /// </summary>
    private void RevealAtPosition(Vector3 worldPos)
    {
        //Convert the world XZ positions into UV coordinates
        //UV is a normalized 0 to 1 range across the fog texture
        //(0,0) is bottom left of the fog area, and (1,1) is the top right
        float u = (worldPos.x - worldBottomLeft.x) / worldSize.x;
        float v = (worldPos.z - worldBottomLeft.y) / worldSize.y;

        //If the position is outside the world area covered by the fog, do nothing
        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return;
        
        //Convert the UV coordinates into pixel coordinates (0 through textureSize-1) on the texture
        int px = Mathf.RoundToInt(u * (textureSize - 1));
        int py = Mathf.RoundToInt(v * (textureSize - 1));

        //Convert reveal radius (in world units) to radius in texture pixels
        float TPixPerWorldX  = (float)textureSize / worldSize.x;
        float TPixPerWorldY  = (float)textureSize / worldSize.y;
        int radiusX = Mathf.CeilToInt(revealRadius * TPixPerWorldX);
        int radiusY = Mathf.CeilToInt(revealRadius * TPixPerWorldY);

        if (radiusX <= 0 || radiusY <= 0)
            return;

        //Reveal circle is centered at px and py, with radiusX and radiusY in pixels.
        //In worse case, any pixel that could be inside that cicle must be between:
        // X: [px - radiusX, px + radiusX]
        // Y: [py - radiusY, py + radiusY]
        //Compute the range, but also clamp so we never go outside
        //the correct texture indices [0, textureSize - 1].
        int startX = Mathf.Max(px - radiusX, 0);
        int endX = Mathf.Min(px + radiusX, textureSize - 1);
        int startY = Mathf.Max(py - radiusY, 0);
        int endY = Mathf.Min(py + radiusY, textureSize - 1);

        //Loop those pixels and reveal the ones inside the circular area.
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                //offset (in pixels) from the center of the reveal circle (px, py)
                //example, if x is at px, then ox = 0
                float offX = x - px;
                float offY = y - py;

                //Check if pixel is inside the circle defined by radius X/Y
                //If the value is over 1, it's outside, so we skip
                if ((offX * offX) / (radiusX * radiusX) + (offY * offY) / (radiusY * radiusY) > 1f)
                    continue;

                //Convert 2D (x, y) coordinates to a 1D index in pixels[] array
                int idx = y * textureSize + x;

                //Reveal only if this pixel is still fogged (alpha > 0)
                if (pixels[idx].a != 0)
                {
                    pixels[idx].a = 0;
                    pixelsChanged = true;
                }
            }
        }
    }
}
