using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DynamicWeatherSystem : MonoBehaviour
{


    /// <summary>
    /// This lets us know if there is more than one EnvironmentUpdater present at once.
    /// </summary>
    [System.NonSerialized]
    public static DynamicWeatherSystem instance = null;

    [System.Serializable]
    public struct WeatherStateSettings
    {

        [Tooltip("Intensity of the sun. (Directional)")]
        [Range(0, 20)]
        public float sun_intensity;
        [Tooltip("Intensity of the skylight. (Environment Lighting)")]
        [Range(0, 20)]
        public float skylight_intensity;
        [Tooltip("Thickness of the fog")]
        [Range(0, 0.1f)]
        public float fog_density;
        [Tooltip("Color of the fog")]
        public Color fog_color;
    };

    #region Helper_Functions

    public float GetBaseWeight(float[] weights)
    {
        float rtrn = 1;
        for (int i = 0; i < weights.Length; i++)
            rtrn -= weights[i];
        return Mathf.Max(rtrn, 0.0f);
    }

    public float[] GetWeights()
    {
        float[] dynamicWeights = {
            RainWeight,
            SnowWeight
        };

        dynamicWeights =  new float[] {
            GetBaseWeight(dynamicWeights),
            RainWeight,
            SnowWeight
        };

        float sum = 0;
        foreach (float f in dynamicWeights)
            sum += f;
        for (int i = 0; i < dynamicWeights.Length; i++)
            dynamicWeights[i] /= sum;

        foreach (float f in dynamicWeights)
            Debug.Log(f);
        return dynamicWeights;
    }

    public WeatherStateSettings GetBlendedState_selective(WeatherStateSettings[] weatherStateSettings, float[] weights)
    {
        float sunintensity = 0;
        float skyintensity = 0;
        float fogdensity = 0;
        Color fogcolor = new Color(0, 0, 0);

        for (int i = 0; i < weatherStateSettings.Length; i++)
        {
            sunintensity += weatherStateSettings[i].sun_intensity * weights[i];
            skyintensity += weatherStateSettings[i].skylight_intensity * weights[i];
            fogdensity += weatherStateSettings[i].fog_density * weights[i];
            fogcolor += weatherStateSettings[i].fog_color * weights[i];
        }

        return new WeatherStateSettings() {
            sun_intensity = sunintensity,
            skylight_intensity = skyintensity,
            fog_density = fogdensity,
            fog_color = fogcolor
        };
    }

    #endregion

    public WeatherStateSettings GetBlendedState()
    {
        return GetBlendedState_selective(
            new WeatherStateSettings[] {
                Sunny, Raining, Snowing
            },
            GetWeights()
        );
    }

    [Header("Weather State")]
    [Tooltip("Daynight cycle. 0.5 is noon, 0 and 1 are midnight, 0.3 is 7am, 0.8 is 7pm")]
    [Range(0,1)]
    public float DayNightWeight = 0.35f;
    [Tooltip("If and how hard it is raining")]
    [Range(0, 1)]
    public float RainWeight = 0f;
    [Tooltip("If and how hard it is snowing")]
    [Range(0, 1)]
    public float SnowWeight = 0f;

    [Header("State Settings")]
    [Tooltip("What the weather should look like when it is sunny")]
    public WeatherStateSettings Sunny = new WeatherStateSettings() {
        sun_intensity = 1.2f,
        skylight_intensity = 1f,
        fog_density = 0.003f,
        fog_color = new Color(0.7f, 0.7f, 0.7f)
    };
    /*[Tooltip("What the weather should look like when it is night")]
    public WeatherStateSettings Night = new WeatherStateSettings()
    {
        sun_intensity = 1.0f,
        skylight_intensity = 0.5f,
        fog_density = 0.003f,
        fog_color = new Color(0.7f, 0.7f, 0.7f)
    };*/
    [Tooltip("What the weather should look like when it is raining")]
    public WeatherStateSettings Raining = new WeatherStateSettings()
    {
        sun_intensity = 0.03f,
        skylight_intensity = 0.05f,
        fog_density = 0.03f,
        fog_color = new Color(0.45f, 0.45f, 0.5f)
    };
    [Tooltip("What the weather should look like when it is snowing")]
    public WeatherStateSettings Snowing = new WeatherStateSettings()
    {
        sun_intensity = 0.02f,
        skylight_intensity = 0.1f,
        fog_density = 0.03f,
        fog_color = new Color(0.5f, 0.5f, 0.5f)
    };
    [Tooltip("Moon uses same intensity as the sun, but weighted by this amount.")]
    [Range(0, 3)]
    public float MoonWeightIntensity = 0.4f;

    [Header("Raining Settings")]
    [Tooltip("This is the max spawn rate that the rain emitter will use.")]
    [Range(0,30000)]
    public float rain_maxparticles = 20000;
    [Tooltip("The raining effect")]
    public ParticleSystem rainEffect;

    [Header("Snowing Settings")]
    [Tooltip("This is the max spawn rate that the rain emitter will use.")]
    [Range(0, 10000)]
    public float snow_maxparticles = 5000;
    [Tooltip("The snowing effect")]
    public ParticleSystem snowEffect;

    [Header("References")]
    [Tooltip("The Material that this scene's sky should use.")]
    public Material SkyMaterial;
    [Tooltip("The lightsource that is the sun")]
    public Light Sun;
    [Tooltip("The lightsource that is the moon")]
    public Light Moon;

    /// <summary>
    /// Awake is called before start. This makes sure that this happens before a frame is rendered.
    /// </summary>
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("There are multiple 'DynamicWeatherSystem' scripts loaded (only one RenderingSettings lighting environment can be loaded at a time). Overriding environment...");
        }

        instance = this;

        Initialize();
        UpdateWeather();
    }

    private void Initialize()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        if (SkyMaterial)
            RenderSettings.skybox = SkyMaterial;

        /*if (SkyMaterial && SkyMaterial.HasProperty("_Exposure"))
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.2117647f, 0.227451f, 0.2588235f);
            RenderSettings.ambientEquatorColor = new Color(0.1137255f, 0.1254902f, 0.1333333f);
            RenderSettings.ambientGroundColor = new Color(0.04705882f, 0.04313726f, 0.03529412f);
        }
        else
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        }*/
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
    }

    private void UpdateWeather()
    {
        WeatherStateSettings currentState = GetBlendedState();

        if (Sun)
        {
            Sun.intensity = currentState.sun_intensity;
            Sun.transform.rotation = Quaternion.Euler(DayNightWeight * 360 - 90, 0, 0);
        }

        if (Moon)
        {
            Moon.intensity = currentState.sun_intensity * MoonWeightIntensity;
        }

        if (SkyMaterial && SkyMaterial.HasProperty("_Exposure"))
        {
            SkyMaterial.SetFloat("_Exposure", currentState.skylight_intensity);
        }

        if (rainEffect)
        {
            ParticleSystem.EmissionModule emission = rainEffect.emission;
            emission.rateOverTimeMultiplier = rain_maxparticles * RainWeight * RainWeight;
        }

        if (snowEffect)
        {
            ParticleSystem.EmissionModule emission = snowEffect.emission;
            emission.rateOverTimeMultiplier = snow_maxparticles * SnowWeight * SnowWeight;
        }

        RenderSettings.ambientIntensity = currentState.skylight_intensity;
        RenderSettings.fogColor = currentState.fog_color;
        RenderSettings.fogDensity = currentState.fog_density;
    }

    private void Update()
    {
#if UNITY_EDITOR
        UpdateEditor();
#endif
    }


#if UNITY_EDITOR

    [Header("Editor")]
    public bool UpdateWeatherInEditor = false;
    public bool SimulateEffectsInEditor = true;

    private void UpdateEditor()
    {
        if (SimulateEffectsInEditor)
        {
            if (rainEffect)
            {
                rainEffect.Simulate(Time.deltaTime);
            }

            if (snowEffect)
            {
                snowEffect.Simulate(Time.deltaTime);
            }
        }
        else
        {
            if (rainEffect)
            {
                rainEffect.Clear();
            }

            if (snowEffect)
            {
                snowEffect.Clear();
            }
        }
    }

    private void Reset()
    {
        Initialize();
        UpdateWeather();

        if (Sun)
        {
            Sun = GetComponentInChildren<Light>();
        }
    }
    /// <summary>
    /// Anytime that a spcific instance of this script changes IN THE EDITOR, this will update the scene view to show the changes
    /// </summary>
    private void OnValidate()
    {
        Initialize();
        UpdateWeather();
    }
#endif
}
