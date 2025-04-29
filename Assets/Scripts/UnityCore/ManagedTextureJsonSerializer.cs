using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using Core.Json;
using System.IO;

namespace UnityCore
{
    public class ManagedTextureJsonSerializer : IJsonPropertySerializer<Texture2D>
    {
        private const string NamePropertyName = "Name";
        private const string PathPropertyName = "Path";

        private static TextureManager mTextureManager;
        /// <summary>
        /// A texture manager that will be used to serialize/deserialize objects.
        /// If no TextureManager is assigned to this property, an attempt is made
        /// to find it in the scene
        /// </summary>
        public static TextureManager TextureManager
        {
            // Can't really justify on why did I do it like this but I'm sure there's a reason...
            get => mTextureManager ?? GameObject.FindFirstObjectByType<TextureManager>();
            set => mTextureManager = value;
        }

        public string ToJson(object obj)
        {
            if (obj == null) return "null";

            Texture2D texture = obj as Texture2D;
            if (texture == null) throw new ArgumentException("The object to serialize is not of type Texture2D");

            FileTexture fileTexture = TextureManager.FindTexture(texture);
            string texturePath = fileTexture.Path;
            if (texturePath is { })
            {
                if (DefaultJsonSerializer.Default.CustomParameters.TryGetValue("DestinationPath", out object path))
                {
                    texturePath = Path.Combine(Path.GetDirectoryName(path as string), texturePath);
                }
            }

            StringBuilder json = new StringBuilder(128); // pretty arbitrary
            JsonSerializerUtility.BeginObject(json);
            // So actually using string literals as property names makes it more likely for old JSON's
            // to survive another code refactoring (i guess)
            JsonSerializerUtility.SerializeDefault(json, NamePropertyName, fileTexture.Texture.name);
            JsonSerializerUtility.SerializeDefault(json, PathPropertyName, texturePath);
            JsonSerializerUtility.EndObject(json);

            return json.ToString();
        }

        public object FromJson(string json, Type type, bool ignoreUnknownProperties = false)
        {
            if (type != typeof(Texture2D)) throw new ArgumentException("Type should be Texture2D");

            Dictionary<string, string> data = JsonSerializerUtility.GetProperties(json);

            string name = (string)DefaultJsonSerializer.Default.FromJson(data[NamePropertyName], typeof(string));
            string path = (string)DefaultJsonSerializer.Default.FromJson(data[PathPropertyName], typeof(string));

            FileTexture fileTexture = path == null ? TextureManager.FindTexture(name) : TextureManager.AddTexture(path, name);

            return fileTexture.Texture;
        }

        public void FromJsonOverwrite(string json, object obj, bool ignoreUnknownProperties = false)
        {
            throw new NotImplementedException();
        }
    }
}