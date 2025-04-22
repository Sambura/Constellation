using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ConstellationUI
{
    /// <summary>
    /// This class represents a dialog window, that has optional (title, `Ok` and `Cancel` buttons)
    /// This class allows the object to be dragged with mouse, since it inherits MonoDraggable
    /// The movement of MonoDialog is automatically restricted so that it cannot go outside the parent object
    /// This behaviour can be modified by setting MonoDraggable's property `RestrictMovement`
    /// </summary>
    public class MonoDialog : MonoDraggable
    {
        /// <summary>
        /// Optional. Default `ok` button. All handlers are connected automatically
        /// </summary>
        [SerializeField] protected UnityEngine.UI.Button _okButton;
        /// <summary>
        /// Optional. Default `cancel` button. All handlers are connected automatically
        /// </summary>
        [SerializeField] protected UnityEngine.UI.Button _cancelButton;
        /// <summary>
        /// Optional. Default dialog title
        /// </summary>
        [SerializeField] protected TextMeshProUGUI _titleLabel;

        /// <summary>
        /// Default dialog title. Initialized from _titleLabel's text on Awake
        /// </summary>
        protected string _defaultDialogTitle;

        /// <summary>
        /// A WindowsManager object that manages this dialog. Usually is set by appropriate 
        /// WindowsManager automatically
        /// </summary>
        public WindowsManager Manager { get; set; }
        /// <summary>
        /// Whether the dialog is enabled and its GameObject is active in hierarchy
        /// </summary>
        public bool DialogActive => (this != null ? gameObject.activeInHierarchy : false) && enabled;
        // note: the null check above is checking whether we are destroyed or not (custom unity != operator)

        /// <summary>
        /// A callback that is executed once when the dialog is closing via CloseDialog
        /// `Ok` and `Cancel` buttons trigger this callback as well
        /// Once the callback is executed, this property is reset to null
        /// The second argument of the callback is dialog result - true if `Ok` button was pressed,
        /// and `false` otherwise
        /// Callback should return true, if the dialog can be closed, or `false` if it should stay opened
        /// If the dialog stays opened, the callback is not reset to null
        /// </summary>
        public OnDialogClosingHandler OnDialogClosing { get; set; }

        /// <summary>
        /// Raised each time ShowDialog is called
        /// </summary>
        public event DialogOpenedHandler DialogOpened;
        /// <summary>
        /// Raised each time the dialog closes completely
        /// </summary>
        public event DialogClosedHandler DialogClosed;

        protected override void Awake()
        {
            base.Awake();

            RestrictMovement = true;
            _okButton?.onClick.AddListener(OkButtonClick);
            _cancelButton?.onClick.AddListener(CancelButtonClick);
            _defaultDialogTitle = _titleLabel?.text;
        }

        protected virtual void OnDestroy()
        {
            _okButton?.onClick.RemoveListener(OkButtonClick);
            _cancelButton?.onClick.RemoveListener(CancelButtonClick);
        }

        /// <summary>
        /// Listener for `Ok` button click
        /// </summary>
        public virtual void OkButtonClick() => CloseDialog(true);

        /// <summary>
        /// Listener for `Cancel` button click
        /// </summary>
        public virtual void CancelButtonClick() => CloseDialog(false);

        /// <summary>
        /// Shows this dialog window, by making its gameObject active
        /// </summary>
        /// <param name="title">The title for the dialog. Value of null means the default title</param>
        /// <param name="onClose">Callback that is assigned to `OnDialogClosed` property. 
        ///     The callback gets two arguments - sender MonoDialog, and the result bool. The result bool is 
        ///     specific for each dialog, but generally result is false when a 'Cancel' button is clicked
        ///     on dialog, and true when 'Ok' is clicked. The callback should return true if the dialog can
        ///     be closed, and false if dialog should stay active </param>
        public virtual void ShowDialog(string title = null, OnDialogClosingHandler onClose = null)
        {
            gameObject.SetActive(true);
            OnDialogClosing = onClose;

            if (_titleLabel)
            {
                _titleLabel.text = title ?? _defaultDialogTitle;
            }

            DialogOpened?.Invoke(this);
        }

        /// <summary>
        /// Closes this dialog window by disabling its gameObject and raising DialogClosed event
        /// </summary>
        /// <param name="result">The result of the dialog - true (success) or false (cancel)</param>
        public virtual void CloseDialog(bool result)
        {
            if (OnDialogClosing != null && OnDialogClosing.Invoke(this, result) == false) return;
            OnDialogClosing = null;
            gameObject.SetActive(false);
            DialogClosed?.Invoke(this, result);
        }

        /// <summary>
        /// Close dialog without triggering any registered callbacks
        /// </summary>
        public virtual void CloseDialog()
        {
            OnDialogClosing = null;
            gameObject.SetActive(false);
        }
    }

    public delegate void DialogOpenedHandler(MonoDialog sender);
    public delegate void DialogClosedHandler(MonoDialog sender, bool isOk);
    public delegate bool OnDialogClosingHandler(MonoDialog sender, bool isOk);
}