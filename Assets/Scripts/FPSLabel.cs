using System.Collections;
using UnityEngine;
using TMPro;

public class FPSLabel : MonoBehaviour
{
    [SerializeField] private FPSCounter counter;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float updateFrequency = 30;
    [SerializeField] private string outputFormat = "FPS: {0:0.0}";

    private void Start()
    {
        StartCoroutine(Updater());
    }

    private IEnumerator Updater()
	{
        WaitForSeconds wait = new WaitForSeconds(1 / updateFrequency);

        while (true)
		{
            float fps = counter.CurrentFps;
            label.text = string.Format(outputFormat, fps);
            label.color = counter.IsValid ? Color.white : Color.red;
            yield return wait;
		}
	}
}
