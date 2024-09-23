using System;

namespace ConfigSerialization.Structuring
{
    /// <summary>
    /// Properties marked with this attribute control visibility of other groups depending on 
    /// property value. For example, this attribute placed on a bool property allows to toggle
    /// a group *on* when the property has value `true`, and toggle the group *off* when the value
    /// is `false`
    /// 
    /// Currently supports bool, int and Enum properties
    /// </summary>
    public class ConfigGroupToggleAttribute : Attribute
    {
        public object[] Mapping { get; private set; }

        /// <summary>
        /// Set to true to disable automatic reordering of UI controls (usually all groups listed in the constructor
        ///     are automatically positioned right after the UI control of the property having this attribute
        /// </summary>
        public bool DoNotReorder { get; set; } = false;

        /// <summary>
        /// List the groups to be toggled by the property as arguments. Allowed arguments are null,
        /// int (local group index) or string (global group ID), as well as an array of items with these types. 
        /// The first argument corresponds to the first property value, second - to the second, etc. Property 
        /// values depend on property type. Only one of the listed groups will be active at a time, depending 
        /// on the property value. You can specify the same group multiple times. Use array to specify several
        /// active groups per property value
        /// <br></br><br></br>
        /// When property has type bool, the first argument corresponds to `true`, and the second to `false` <br></br>
        /// When property has type int, the first argument corresponds to `0`, second to `1`, etc. <br></br>
        /// When property type is an enum, the first argument corresponds to an enum value with the smallest
        ///     unsigned numeric value, and so on in ascending order. This is typically the same order as is listed
        ///     in the enum definition, unless enum values are assigned custom values.
        /// <br></br><br></br>
        /// As an example, if you want to toggle group with index 3 *on* when the bool property has value `true`, use 
        /// [ConfigGroupToggle(3)] <br></br> 
        /// If you want to toggle group on when the property is `false` instead, use
        /// [ConfigGroupToggle(null, 3)] <br></br>
        /// Finally, to activate group 3 when the value is `true`, and group 5 when value is `false`, use
        /// [ConfigGroupToggle(3, 5)]
        /// </summary>
        public ConfigGroupToggleAttribute(params object[] groups)
        {
            Mapping = groups;
        }
    }
}
