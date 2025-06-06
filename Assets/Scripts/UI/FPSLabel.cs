using System.Collections;
using UnityEngine;
using TMPro;
using UnityCore;

public class FPSLabel : MonoBehaviour
{
    [SerializeField] private StaticTimeFPSCounter _counter;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private float _updateFrequency = 30;
    [SerializeField] private string _outputFormat = "FPS: ";
    [SerializeField] private int _decimalPlaces = 1;

    private string _floatFormat;

    private void Start()
    {
        _floatFormat = "0." + new string('0', _decimalPlaces);
    }

    private void OnEnable()
    {
        StartCoroutine(Updater());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Updater()
    {
        WaitForSeconds wait = new WaitForSeconds(1 / _updateFrequency);

        float lastFps = -1;
        while (true)
        {
            float fps = (float)System.Math.Round(_counter.CurrentFps, _decimalPlaces);
            if (fps != lastFps)
                _label.text = _outputFormat + fps.ToString(_floatFormat);
            yield return wait;
        }
    }
}
