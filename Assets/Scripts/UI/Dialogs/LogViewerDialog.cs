using UnityEngine;
using TMPro;

namespace ConstellationUI
{
    public class LogViewerDialog : MonoDialog
    {
        [SerializeField] private TMP_InputField _logOutput;
        [SerializeField] private float _scrollSensitivity = 20;
        [SerializeField] private float _textUpdateMaxFrequency = 2;

        // The reason for updating the text like this is to save performance.
        // If multiple messages are appended to the text every frame, redrawing
        // text on each new update take too much time, leading to unplayable fps
        private bool _updateText;
        private float _lastTextUpdate;
        private string _text;

        public string Text
        {
            get => _text;
            set { _text = value; _updateText = true; }
        }

        private void UpdateText(string newText, bool force = false)
        {
            if (!force && Time.time - _lastTextUpdate < 1 / _textUpdateMaxFrequency) return;

            // TMP is broken, so this line ensures that scroll position is reset
            if (newText.Length < _logOutput.text.Length) ScrollToBeginning();
            _logOutput.text = newText;
            ScrollToEnd();
            _lastTextUpdate = Time.time;
            _updateText = false;
        }

        protected override void Awake()
        {
            base.Awake();
            _logOutput.scrollSensitivity = _scrollSensitivity;
            _updateText = true;
        }

        protected override void Update()
        {
            base.Update();
            if (_updateText) UpdateText(_text);
        }

        public void ScrollToBeginning() => Scroll(float.PositiveInfinity);

        public void ScrollToEnd() => Scroll(float.NegativeInfinity);

        public void Scroll(float deltaPixels)
        {
            // No, there is no way to do this better. Well, unless TMP gets an update
            // ScrollSensitivity manipulations are questionable tho. They do cause some canvas
            // dirty rebuild trickery, but I guess it is not really a massive issue
            float oldSensitivity = _logOutput.scrollSensitivity;
            _logOutput.scrollSensitivity = 1;
            _logOutput.OnScroll(new UnityEngine.EventSystems.PointerEventData
                (UnityEngine.EventSystems.EventSystem.current)
            { scrollDelta = new Vector2(0, deltaPixels) }
            );
            _logOutput.scrollSensitivity = oldSensitivity;
        }
    }
}