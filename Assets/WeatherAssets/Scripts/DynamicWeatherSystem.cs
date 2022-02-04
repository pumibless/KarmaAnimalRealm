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

    #region public_variables

    [Header("Weather State")]
    [Tooltip("Daynight cycle. 0.5 is noon, 0 and 1 are midnight, 0.3 is 7am, 0.8 is 7pm")]
    [Range(0, 1)]
    [SerializeField] // Private+Serialized: Only should be updated within this script and in the editor.
    private float m_DayNightWeight = 0.35f;
    [Tooltip("If and how hard it is raining")]
    [Range(0, 1)]
    [SerializeField] // Private+Serialized: Only should be updated within this script and in the editor.
    private float m_RainWeight = 0f;
    [Tooltip("If and how hard it is snowing")]
    [Range(0, 1)]
    [SerializeField] // Private+Serialized: Only should be updated within this script and in the editor.
    private float m_SnowWeight = 0f;

    [Header("Dynamic Weather")]
    [Tooltip("If this script should dynamically update weather on its own.")]
    [SerializeField] // Private+Serialized: Only should be updated within this script and in the editor.
    private bool m_UseDynamicWeather = true;
    [Tooltip("If not, then the weather will start at the current settings.")]
    public bool RandomizeWeatherOnAwake = false;
    [Tooltip("The length (in seconds) one Day/Night cycle")]
    public float DayLength = 300f;
    [Tooltip("Used to describe how often the weather should change.")]
    [Min(0.5f)]
    public Vector2 WeatherDurationRange = new Vector2(100f, 300f);
    [Tooltip("Used to describe the seconds that the weather should spend changing (blending) states.")]
    public float DefaultBlendDuration = 20f;

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
    [Range(0, 30000)]
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

    #endregion

    #region Properties

    public bool UseDynamicWeather
    {
        get { return m_UseDynamicWeather; }
        set
        {
            if (value)
            {
                // Reset the weather counter when activating dynamic weather.
                weatherCounter = GetRandomWeatherDelay();
            }
            m_UseDynamicWeather = value;
        }
    }

    public float DayNightWeight
    {
        get { return m_DayNightWeight; }
        set
        {
            if (UseDynamicWeather)
            {
                Debug.LogWarning("Trying to updated time of day while weather is automatically updated. Consider using SetDayTime(float).");
            }
            m_DayNightWeight = value;
        }
    }

    public float RainWeight
    {
        get { return m_RainWeight; }
        set
        {
            m_RainWeight = value;
            if (UseDynamicWeather)
            {
                Debug.LogWarning("Trying to updated Weather while weather is automatically updated. (Recomended to do this explicitly or use SetWeatherState(float,WeatherState))");
            }
        }
    }
    public float SnowWeight
    {
        get { return m_SnowWeight; }
        set
        {
            m_SnowWeight = value;
            if (UseDynamicWeather)
            {
                Debug.LogWarning("Trying to updated Weather while weather is automatically updated. (Recomended to do this explicitly or use SetWeatherState(float,WeatherState))");
            }
        }
    }

    #endregion

    #region nonsaved_privates

    /// <summary>
    /// Keeps track of how long the current weather state has been active
    /// </summary>
    private float weatherCounter = 0;
    /// <summary>
    /// The number of weather states that this script uses.
    /// </summary>
    private static readonly int weatherStateCount = System.Enum.GetValues(typeof(WeatherStates)).Length;
    /// <summary>
    /// Keeps track of the current weather blend instance.
    /// </summary>
    [System.NonSerialized]
    private IEnumerator[] weatherBlendCoroutines = null;

    /// <summary>
    /// Keeps track of the current daytime blend instance.
    /// </summary>
    private IEnumerator daytimeBlendCoroutine = null;

    #endregion

    #region core_functionalities
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

        if (weatherBlendCoroutines == null)
        {
            weatherBlendCoroutines = new IEnumerator[weatherStateCount];
            for (int i = 0; i < weatherStateCount; i++)
                weatherBlendCoroutines[i] = null;
        }

        if (RandomizeWeatherOnAwake)
        {
            weatherCounter = -1f;
        }
        else weatherCounter = GetRandomWeatherDelay();
    }

    private void UpdateWeather()
    {
        WeatherStateSettings currentState = GetBlendedState();

        float sunset_weight = currentState.sun_intensity * Mathf.InverseLerp(0.8f, 0.7f, DayNightWeight) * Mathf.InverseLerp(0.2f, 0.3f, DayNightWeight);
        if (Sun)
        {
            Sun.intensity = currentState.sun_intensity * sunset_weight;
            Sun.transform.rotation = Quaternion.Euler(DayNightWeight * 360 - 90, 0, 0);
        }

        if (Moon)
        {
            Moon.intensity = currentState.sun_intensity * MoonWeightIntensity * (1 - sunset_weight);
            Moon.transform.rotation = Quaternion.Euler(DayNightWeight * 360 + 90, 0, 0);
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

        if (!UpdateWeatherInEditor) return;
#endif

        bool updateWeather = false;
        if (daytimeBlendCoroutine != null)
        {
            daytimeBlendCoroutine.MoveNext();
            updateWeather = true;
        }

        foreach (IEnumerator c in weatherBlendCoroutines)
        {
            if (c != null)
            {
                c.MoveNext();
                updateWeather = true;
            }
        }

        if (updateWeather) UpdateWeather();
        if (!UseDynamicWeather) return;

        weatherCounter -= Time.deltaTime;

        if (weatherCounter <= 0)
        {
            weatherCounter = GetRandomWeatherDelay();

            float[] ws = new float[weatherStateCount];
            for (int i = 0; i < weatherStateCount; i++) ws[i] = 0;

            
            for (int i = 1; i < weatherStateCount + 1; i++)
            {
                float random = Random.value;

                if (random < 0.5f) break;

                int state = Random.Range(0, weatherStateCount);
                random = Random.value;

                Debug.Log(state + ": " + weatherStateCount);
                ws[state] = random;
            }

            for (int i = 0; i < weatherStateCount; i++)
            {
                SetWeatherState(ws[i], (WeatherStates)i, DefaultBlendDuration);
            }
        }

        m_DayNightWeight += Time.deltaTime / DayLength;
        while (DayNightWeight >= 1) m_DayNightWeight -= 1;
    }

    #endregion

    #region public_functions
    public void SetDayTime(float daytime) { SetDayTime(daytime, DefaultBlendDuration); }
    public void SetDayTime(float daytime, float blendTime)
    {
        daytimeBlendCoroutine = DaytimeBlend_Coroutine(daytime, blendTime);
    }

    public void SetWeatherState(float value, WeatherStates type) { SetWeatherState(value, type, DefaultBlendDuration); }
    public void SetWeatherState(float value, WeatherStates type, float blendTime)
    {
        int type_i = (int)type;

        if(UseDynamicWeather) weatherCounter = GetRandomWeatherDelay();

        weatherBlendCoroutines[type_i] = WeatherBlend_Coroutine(value, type_i, blendTime);
    }

    #endregion

    #region Helper_Functions

    /**********************************************************/
    /*                      Coroutines                        */

    private IEnumerator WeatherBlend_Coroutine(float target, int type_i, float blendTime)
    {
        float original = BaseWeights[type_i];
        float counter = 0;
        do
        {
            counter += Time.deltaTime;
            SetBaseWeight(Mathf.Lerp(original, target, counter / blendTime), (WeatherStates) type_i);
            
            yield return null;
        } while (counter < blendTime);

        weatherBlendCoroutines[type_i] = null;
    }

    private IEnumerator DaytimeBlend_Coroutine(float daytime, float blendTime)
    {
        float originalTime = DayNightWeight;
        float counter = 0;
        do
        {
            counter += Time.deltaTime;
            m_DayNightWeight = Mathf.Lerp(originalTime, daytime, counter / blendTime);
            
            yield return null;
        } while (counter < blendTime);

        daytimeBlendCoroutine = null;
    }

    /**********************************************************/

    public float GetDayWeight(float[] weights)
    {
        float rtrn = 1;
        for (int i = 0; i < weights.Length; i++)
            rtrn -= weights[i];
        return Mathf.Max(rtrn, 0.0f);
    }

    private float[] BaseWeights { get { return new float[] {
        RainWeight,
        SnowWeight
    }; } }

    private void SetBaseWeight(float value, WeatherStates type)
    {
        switch(type)
        {
            case WeatherStates.Rain:
                m_RainWeight = value;
                return;
            case WeatherStates.Snow:
                m_SnowWeight = value;
                return;
        }
    }

    private float[] GetAllWeights()
    {
        float[] base_W = BaseWeights;
        float[] dynamicWeights = new float[weatherStateCount + 1];
        dynamicWeights[0] = GetDayWeight(base_W);
        for (int i = 0; i < weatherStateCount; i++)
            dynamicWeights[i + 1] = base_W[i];

        float sum = 0;
        foreach (float f in dynamicWeights)
            sum += f;
        for (int i = 0; i < dynamicWeights.Length; i++)
            dynamicWeights[i] /= sum;

        return dynamicWeights;
    }

    private WeatherStateSettings GetBlendedState_selective(WeatherStateSettings[] weatherStateSettings, float[] weights)
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

        return new WeatherStateSettings()
        {
            sun_intensity = sunintensity,
            skylight_intensity = skyintensity,
            fog_density = fogdensity,
            fog_color = fogcolor
        };
    }

    private WeatherStateSettings GetBlendedState()
    {
        return GetBlendedState_selective(
            new WeatherStateSettings[] {
                Sunny, Raining, Snowing
            },
            GetAllWeights()
        );
    }

    private float GetRandomWeatherDelay() { return Mathf.Lerp(WeatherDurationRange.x, WeatherDurationRange.y, Random.value); }

#endregion

    /// <summary>
    /// This struct helps manage the data for general weather states (Sunny, raining, snowing...).
    /// </summary>
    [System.Serializable] // Can be saved/edited in the editor
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

    /// <summary>
    /// Identifies the specific weather settings that the weather system has
    /// </summary>
    public enum WeatherStates
    {
        Rain = 0,
        Snow = 1,
    }


    #region editor
#if UNITY_EDITOR

    [Header("Editor")]
    public bool UpdateWeatherInEditor = false;
    public bool SimulateEffectsInEditor = true;

    private void UpdateEditor()
    {
        if (SimulateEffectsInEditor)
        {
            if (rainEffect)
                rainEffect.Simulate(Time.deltaTime);

            if (snowEffect)
                snowEffect.Simulate(Time.deltaTime);
        }
        else
        {
            if (rainEffect)
                rainEffect.Clear();

            if (snowEffect)
                snowEffect.Clear();
        }

        if (!UpdateWeatherInEditor)
        {
            for (int i = 0; i < weatherBlendCoroutines.Length; i++)
                weatherBlendCoroutines[i] = null;
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
#endregion
}
