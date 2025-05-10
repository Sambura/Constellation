using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityCore;

namespace ConstellationUI
{
    public class TexturePicker : MonoDialog
    {
        [Header("Objects")]
        [SerializeField] private Image _previewFrame;
        [SerializeField] private GameObject _textureListEntryPrefab;
        [SerializeField] private RectTransform _texturesListView;
        [SerializeField] private FileDialog _fileDialog;
        [SerializeField] private TextureManager _textureManager;

        public System.Action<Texture2D> OnTextureChanged { get; set; }
        public event System.Action<Texture2D> TextureChanged;
        public Texture2D Texture
        {
            get => _texture;
            set
            {
                if (_texture == value) return;
                _texture = value;
                _previewFrame.sprite = TextureManager.ToSprite(value);
                TextureChanged?.Invoke(value);
            }
        }

        private readonly List<GameObject> _listEntryObjects = new List<GameObject>();
        private TextIconListEntry _selected;
        private Texture2D _texture;

        protected override void Awake()
        {
            base.Awake();

            _previewFrame.sprite = TextureManager.ToSprite(Texture);
            DialogOpened += x => UpdateSpriteList();
            DialogOpened += x => _textureManager.TexturesChanged += UpdateSpriteList;
            DialogClosed += (x, y) => _textureManager.TexturesChanged -= UpdateSpriteList;
            TextureChanged += InvokeOnTextureChanged;
        }

        private void InvokeOnTextureChanged(Texture2D texture) => OnTextureChanged?.Invoke(texture);

        private void OnListEntryClicked(TextIconListEntry entry)
        {
            if (_selected) _selected.Highlighted = false;
            _selected = entry;
            _selected.Highlighted = true;
            Texture = _selected.EntryData as Texture2D;
        }

        public void UpdateSpriteList()
        {
            foreach (GameObject file in _listEntryObjects) Destroy(file);
            _listEntryObjects.Clear();
            _selected = null;

            foreach (FileTexture fileTexture in _textureManager.Textures)
            {
                Texture2D texture = fileTexture.Texture;
                GameObject newFileObject = Instantiate(_textureListEntryPrefab, _texturesListView);
                TextIconListEntry listEntry = newFileObject.GetComponent<TextIconListEntry>();
                listEntry.LabelText = texture.name;
                listEntry.Icon.sprite = TextureManager.ToSprite(texture);
                listEntry.EntryData = texture;

                UnityEngine.UI.Button button = newFileObject.GetComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(() => OnListEntryClicked(listEntry));
                _listEntryObjects.Add(newFileObject);
            }
        }
        
        public void OpenAddNewSpriteDialog()
        {
            _fileDialog.FileFilters = new List<FileDialog.FileFilter>()
            {
                new FileDialog.FileFilter() { Description = "Image files", Pattern = "*.png" },
                new FileDialog.FileFilter() { Description = "Image files", Pattern = "*.jpg|*.jpeg" },
                new FileDialog.FileFilter() { Description = "All files", Pattern = "*" }
            };
            _fileDialog.ShowDialog("Select image file", OnFileDialogClosing);
            _fileDialog.SyncCurrentDirectory(this);

            bool OnFileDialogClosing(MonoDialog sender, bool isOk)
            {
                if (!isOk) return true;

                if (System.IO.File.Exists(_fileDialog.FileName) == false)
                {
                    _fileDialog.Manager.ShowMessageBox("Failure", "Selected file could not be loaded, since it was not found.",
                        StandardMessageBoxIcons.Error, _fileDialog);
                    return false; // Do not close dialog, since the invalid file was selected
                }

                Manager.ShowInputFieldDialog("Enter new sprite's name:", System.IO.Path.GetFileName(_fileDialog.FileName),
                    OnInputFieldDialogClosing, true, this);

                return true;
            }

            bool OnInputFieldDialogClosing(InputFieldDialog inputFieldDialog, bool isOk)
            {
                if (!isOk) return true;

                if (string.IsNullOrWhiteSpace(inputFieldDialog.InputString))
                {
                    Manager.ShowMessageBox("Error", "Invalid sprite name", StandardMessageBoxIcons.Error, inputFieldDialog);
                    return false;
                }

                try
                {
                    _textureManager.AddTexture(_fileDialog.FileName, inputFieldDialog.InputString);
                }
                catch (TextureAddException e)
                {
                    if (e.Member == TextureAddError.NameExists)
                    {
                        Manager.ShowMessageBox("Error", "Sprite with specified name already exists", StandardMessageBoxIcons.Error, inputFieldDialog);
                        return false;
                    } else if (e.Member == TextureAddError.TextureExists)
                    {
                        Manager.ShowMessageBox("Error", "This texture was already added to the list", StandardMessageBoxIcons.Error, inputFieldDialog);
                        return true;
                    }

                    throw e;
                }

                UpdateSpriteList();
                OnListEntryClicked(_listEntryObjects[^1].GetComponent<TextIconListEntry>());
                return true;
            }
        }

        public void RemoveSelected()
        {
            if (_selected == null || _textureManager.Textures.Count <= 1) return;
            // We could use `Texture` property instead, but we don't have guarantees on that it 
            // is currently selected in the list
            _textureManager.RemoveTexture(_selected.EntryData as Texture2D);
            UpdateSpriteList();
            _selected = null;
            OnListEntryClicked(_listEntryObjects[0].GetComponent<TextIconListEntry>());
        }
    }
}
