namespace ConfigSerialization
{
    public class InvokableMethod : System.Attribute
    {
        public string Name { get; }

        public InvokableMethod(string name = null)
        {
            Name = name;
        }
    }
}
