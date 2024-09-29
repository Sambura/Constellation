namespace Core
{
    public interface IObjectConverter<T>
    {
        public T Convert(object input);
    }
}
