using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GradientImage : MonoBehaviour
{
    [SerializeField] private Gradient _gradient;
    private Image _image;
    private readonly int GradientTexPropertyId = Shader.PropertyToID("_Gradient");
    private readonly int KeysCountPropertyId = Shader.PropertyToID("_KeysCount");

    private Texture2D _gradientTexture;

    public Gradient Gradient
	{
        get => _gradient;
        set { _gradient = value; UpdateGradient(); } 
	}

    void Awake()
    {
        _image = GetComponent<Image>();
        _image.material = new Material(_image.material);
        _gradientTexture = new Texture2D(1, 1);
        _gradientTexture.wrapMode = TextureWrapMode.Clamp;

        UpdateGradient();
    }

    void UpdateGradient()
    {
        if (_gradientTexture == null) return;

        if (_gradient == null)
        {
            _gradientTexture.Resize(1, 1);
            _gradientTexture.SetPixel(0, 0, Color.white);
            _image.material.SetFloat(KeysCountPropertyId, 1);
        }
        else
        {
            GradientColorKey[] colorKeys = _gradient.colorKeys;
            if (_gradientTexture.width != colorKeys.Length)
                _gradientTexture.Resize(colorKeys.Length, 1);
            for (int i = 0; i < colorKeys.Length; i++)
            {
                Color color = colorKeys[i].color;
                color.a = colorKeys[i].time;
                _gradientTexture.SetPixel(i, 0, color);
            }
            _image.material.SetFloat(KeysCountPropertyId, colorKeys.Length);
        }
        _gradientTexture.Apply();

        _image.material.SetTexture(GradientTexPropertyId, _gradientTexture);
    }
}
