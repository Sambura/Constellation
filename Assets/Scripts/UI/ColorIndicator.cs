using UnityEngine;

namespace ConstellationUI
{
    public class ColorIndicator : LabeledUIElement
    {
        [Header("Objects")]
        [SerializeField] private UnityEngine.UI.Image _image;

        public Color Color
        {
            get => _image.color;
            set => _image.color = value;
        }
    }
}