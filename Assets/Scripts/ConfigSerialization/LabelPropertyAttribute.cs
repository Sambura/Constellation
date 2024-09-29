using System;
using Core;

namespace ConfigSerialization
{
    /// <summary>
    /// Creates a label that displays text in a format {propertyName}: {propertyValue} <br/>
    /// Allows to disable displaying property name and lets adjust propertyValue's color <br/>
    /// </summary>
    public class LabelPropertyAttribute : ConfigProperty
    {
        public IObjectConverter<string> RawConverter { get; set; }
        public bool DisplayPropertyName { get; set; } = true;
        public string TextColor { get; set; } = null;

        /// <summary>
        /// Specify converter type to use custom conversion of property value to string. The type should implement IObjectConverter[string]
        /// </summary>
        public LabelPropertyAttribute(Type converterType = null, string name = null, bool hasEvent = true) : base(name, hasEvent)
        {
            RawConverter = Activator.CreateInstance(converterType) as IObjectConverter<string>;
        }

        public static IObjectConverter<string> GetDefaultConverter(string color = null) => new DefaultConverter(color);

        public IObjectConverter<string> GetConverter(string propertyName)
        {
            IObjectConverter<string> converter = RawConverter ?? GetDefaultConverter(TextColor);
            return DisplayPropertyName ? new NamedConverter(converter, propertyName, TextColor) : converter;
        }

        private static string ColorizeString(string text, string color)
        {
            if (string.IsNullOrEmpty(color)) return text;
            return $"<color={color}>{text}</color>";
        }

        private class NamedConverter : IObjectConverter<string>
        {
            private readonly IObjectConverter<string> Converter;
            private readonly string Name;
            private readonly string Color;

            public string Convert(object input) {
                return $"{Name}: {ColorizeString(Converter.Convert(input), Color)}";
            }

            public NamedConverter(IObjectConverter<string> converter, string name, string color)
            {
                Converter = converter;
                Name = name;
                Color = color;
            }
        }

        private class DefaultConverter : IObjectConverter<string>
        {
            readonly private string Color;

            public string Convert(object input) => ColorizeString(input.ToString(), Color);

            public DefaultConverter(string color = null) { Color = color; }
        }
    }
}
