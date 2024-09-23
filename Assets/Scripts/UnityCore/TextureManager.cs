using System.Collections.Generic;
using UnityEngine;

namespace UnityCore
{
    [System.Serializable]
    public class FileTexture
    {
        [SerializeField] private string _path;
        [SerializeField] private Texture2D _texture;

        public string Path { get => _path; set => _path = value; }
        public Texture2D Texture => _texture;
        public FileTexture(Texture2D texture, string path = null)
        {
            _texture = texture;
            Path = path;
        }
    }

    public enum TextureAddError { NameExists, TextureExists }

    public class TextureAddException : System.Exception
    {
        public TextureAddError Member { get; }
        public TextureAddException(TextureAddError member) : base() { Member = member; }
        public TextureAddException(TextureAddError member, string message) : base(message) { Member = member; }
    }

    public class TextureManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private List<FileTexture> _textures;

        /// <summary>
        /// A list of textures currently stored in this TextureManager
        /// </summary>
        public IReadOnlyList<FileTexture> Textures => _textures;

        /// <summary>
        /// Event that is invoked every time the Textures list changes (via TextureManager)
        /// </summary>
        public event System.Action TexturesChanged;

        /// <summary>
        /// Find a texture in the TextureManager's list with the given file path
        /// </summary>
        public FileTexture FindTextureByPath(string path) => _textures.Find(x => System.IO.Path.GetFullPath(x.Path) == System.IO.Path.GetFullPath(path));
        /// <summary>
        /// Find a given texture in the TextureManager's list. The textures are compared by reference
        /// </summary>
        public FileTexture FindTexture(Texture2D texture) => _textures.Find(x => x.Texture == texture);
        /// <summary>
        /// Find a texture in the TextureManager's list with the given name
        /// </summary>
        public FileTexture FindTexture(string name) => _textures.Find(x => x.Texture.name == name);

        /// <summary>
        /// Add a new texture to the TextureManager, stored at the specified path, 
        /// and optionally give it a custom name
        /// </summary>
        public FileTexture AddTexture(string path, string name = null)
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            byte[] data = System.IO.File.ReadAllBytes(fullPath);

            Texture2D texture = new Texture2D(0, 0);
            texture.LoadImage(data);
            texture.name = name ?? System.IO.Path.GetFileName(fullPath);

            // We could also check for whether we have a texture with the same path too, but 1) if the textuers
            // are different, chances are - paths are different too; 2) Even if the textures are somehow different,
            // duplicate paths shouldn't really break anything in the system, so might as well just ignore it
            FileTexture existing = FindTexture(texture.name);
            if (existing != null)
            {
                if (CompareTextures(existing.Texture, texture)) return existing;
                throw new TextureAddException(TextureAddError.NameExists);
            }
            if (FindTexture(texture) != null) throw new TextureAddException(TextureAddError.TextureExists);

            FileTexture namedTexture = new FileTexture(texture, fullPath);
            _textures.Add(namedTexture);
            TexturesChanged?.Invoke();
            return namedTexture;
        }

        /// <summary>
        /// Remove a given texture from the TextureManager's list and dispose of the Texture2D object
        /// </summary>
        public void RemoveTexture(Texture2D texture)
        {
            FileTexture namedTexture = FindTexture(texture);
            if (namedTexture == null) throw new System.ArgumentException("The specified texture does not exist in TextureManager");

            _textures.Remove(namedTexture);
            if (namedTexture.Path != null) Destroy(namedTexture.Texture);
            TexturesChanged?.Invoke();
        }

        /// <summary>
        /// Creates a generic sprite from the given texture
        /// </summary>
        public static Sprite ToSprite(Texture2D texture)
        {
            if (texture == null) return null; // I think these sprites are garbage collected, so its okay
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        private void Awake()
        {
            foreach (FileTexture fileTexture in _textures)
            {
                if (string.IsNullOrWhiteSpace(fileTexture.Path))
                    fileTexture.Path = null;
            }
        }

        /// <summary>
        /// Compares two textures pixel by pixel.
        /// Returns true if two textures have the same content
        /// </summary>
        public static bool CompareTextures(Texture2D a, Texture2D b)
        {
            if (a == b) return true;
            if (a.width != b.width || a.height != b.height) return false;

            var dataA = a.GetRawTextureData<byte>();
            var dataB = a.GetRawTextureData<byte>();
            //byte[] dataA = a.GetRawTextureData();
            //byte[] dataB = b.GetRawTextureData();

            if (dataA.Length != dataB.Length)
            {
                Debug.LogWarning("Textures with different data lengths encountered");
                return false;
            }

            for (int i = 0; i < dataA.Length; i++)
                if (dataA[i] != dataB[i]) return false;

            return true;
        }
    }
}