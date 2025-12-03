using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FogOfWar : MonoBehaviour
{
    [Header("World bounds covered by fog")]
    public Vector2 worldBottomLeft = new Vector2(-50f, -50f);   //X and Z
    public Vector2 worldSize = new Vector2(100f, 100f);         //width (x), height (z)

    [Header("Texture settings")]
    public int textureSize = 256;
    public float revealRadius = 5f;

    [Header("Player")]
    public Transform player;

    private Texture2D fogTexture;
    private Color32[] pixels;
    private Renderer rend;
    private bool pixelsChanged = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        //Create (only alpha) fog texture that will be manually chnaged at runtime
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.Alpha8, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp; //Does not tile, it clamps edges

        pixels = new Color32[textureSize * textureSize];

        //Start with 255 alpha, which means fully fogged
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 255);

        //Upload initial pixel data to GPU
        fogTexture.SetPixels32(pixels);
        fogTexture.Apply(); 

        //Use this (texture) as the material for the fog
        rend.material.mainTexture = fogTexture;
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
    /// This function will convert the world position to fog texture space, then finds the pixel coordinates (of player),
    /// and then it change the alpha of those pixels (in the radius, of player) to 0, so that the fog becomes clear.
    /// 
    /// Once the are is revealed, it stays revealed, which leads to a revealed path.
    /// </summary>
    void RevealAtPosition(Vector3 worldPos)
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
        float TPixPerWorldX  =(float)textureSize / worldSize.x;
        float TPixPerWorldY  =(float)textureSize / worldSize.y;
        int radiusX = Mathf.CeilToInt(revealRadius * TPixPerWorldX);
        int radiusY = Mathf.CeilToInt(revealRadius * TPixPerWorldY);

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
