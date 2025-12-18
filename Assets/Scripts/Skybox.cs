using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This Script forces the skybox to be whatever is assigned because when loading a level from the main menu it doesn't keep light settings

public class LevelLightingSetter : MonoBehaviour
{
    [SerializeField] private Material levelSkybox;
    [SerializeField] private float ambientIntensity = 1f;

    void Start()
    {
        if (levelSkybox != null)
        {
            RenderSettings.skybox = levelSkybox;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = ambientIntensity;

        DynamicGI.UpdateEnvironment();
    }
}
