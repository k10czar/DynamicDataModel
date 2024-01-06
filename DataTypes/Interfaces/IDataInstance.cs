using TypeReferences;

public interface IDataInstance
{
    bool TrySet( object obj, DataInstance contextualData );
}
