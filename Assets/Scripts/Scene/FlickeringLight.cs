using UnityEngine;
namespace Scene
{
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        [Header("Flicker Settings")]
        public float MinIntensity = 0.5f;
        public float MaxIntensity = 1.5f;
        public float FlickerSpeed = 0.1f; // Saniye cinsinden, ne kadar hızlı flicker yapsın
        public float FlickerRandomness = 0.05f; // Flicker aralığına rastgelelik ekler
        public float MinWait = 8f; // Flicker başlamadan önce minimum bekleme süresi
        public float MaxWait = 12f; // Flicker başlamadan önce maksimum bekleme süresi
        public float FlickerDuration = 1f; // Flicker'ın toplam süresi
        public bool AlternateMinMax = false; // Random yerine sırayla min/max yapar

        private Light _light;
        private float _flickerEndTime;

        private bool _lastWasMin = false;

        void Awake()
        {
            _light = GetComponent<Light>();
            if (_light == null)
            {
                _light = GetComponentInChildren<Light>();
            }
        }

        void Start()
        {
            _light.intensity = MaxIntensity;
            StartCoroutine(FlickerRoutine());
        }

        System.Collections.IEnumerator FlickerRoutine()
        {
            while (true)
            {
                // Bekleme süresi
                float wait = Random.Range(MinWait, MaxWait);
                _light.intensity = MaxIntensity;
                yield return new WaitForSeconds(wait);

                // Flicker başlasın
                _flickerEndTime = Time.time + FlickerDuration;
                while (Time.time < _flickerEndTime)
                {
                    float newIntensity;
                    if (AlternateMinMax)
                    {
                        // Sırayla min ve max intensity uygula
                        newIntensity = (_lastWasMin ? MaxIntensity : MinIntensity);
                        _lastWasMin = !_lastWasMin;
                    }
                    else
                    {
                        newIntensity = Random.Range(MinIntensity, MaxIntensity);
                    }
                    _light.intensity = newIntensity;
                    float nextFlicker = FlickerSpeed + Random.Range(-FlickerRandomness, FlickerRandomness);
                    yield return new WaitForSeconds(Mathf.Max(0.01f, nextFlicker));
                }
                // Flicker bitti, intensity normale dönsün
                _light.intensity = MaxIntensity;
            }
        }
    }
}
