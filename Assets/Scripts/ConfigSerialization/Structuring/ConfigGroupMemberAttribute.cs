using System;

namespace ConfigSerialization.Structuring
{
    public class ConfigGroupMemberAttribute : Attribute
    {
        private string _groupId;
        private string _parentId;
        private int _groupIndex;
        private int _parentIndex;

        public string GroupName { get; private set; }
        public int GroupIndex
        {
            get => _groupIndex;
            private set => _groupIndex = value;
        }
        public int ParentIndex 
        { 
            get => _parentIndex;
            private set => _parentIndex = value;
        }
        public string GroupId
        {
            get => _groupId;
            set
            {
                _groupId = value;
                CheckGroupIds();
            }
        }
        public string ParentId
        {
            get => _parentId;
            set
            {
                _parentId = value;
                CheckGroupIds();
            }
        }
        public int SetDisplayIndex { get => DisplayIndex ?? 0; set => DisplayIndex = value; }
        public int? DisplayIndex { get; protected set; }
        public ConfigGroupLayout Layout { get; set; } = ConfigGroupLayout.Default;
        public bool? Indent { get; set; }
        public bool SetIndent { get => Indent ?? true; set => Indent = value; }

        private void CheckGroupIds()
        {
            if (_groupId == ParentId && _groupId != null)
                throw new InvalidOperationException("Cannot set ParentId and GroupId the same value");
        }

        private void CheckGroupIndices()
        {
            if (GroupIndex == ParentIndex)
                throw new ArgumentException("Group cannot be a child of itself");
            if (GroupIndex < 0) throw new ArgumentException("Group index should be non-negative");
        }

        public ConfigGroupMemberAttribute(int groupIndex = 0, int parentIndex = -1)
        {
            _groupIndex = groupIndex;
            _parentIndex = parentIndex;
            CheckGroupIndices();
        }

        public ConfigGroupMemberAttribute(string groupName, int groupIndex = 0, int parentIndex = -1)
        {
            GroupName = groupName;
            _groupIndex = groupIndex;
            _parentIndex = parentIndex;
            CheckGroupIndices();
        }
    }

    public enum ConfigGroupLayout { Default, Vertical, Horizontal }
}
