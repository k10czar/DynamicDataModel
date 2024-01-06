public interface IDataDependent
{
    bool Feed( object t );
    System.Type GetDependentType();
}

// public interface IDataDependent<T> : IDataDependent where T : IDataInstance
// {
//     bool Feed( T t );
//     // public System.Type GetDependentType() => typeof(T);
// }