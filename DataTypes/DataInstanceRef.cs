using UnityEngine;

[System.Serializable]
public class DataInstanceRef : IDataInstance
{
    [SerializeField] DataInstance _data;

    public DataInstance Data => _data;

    public override string ToString() => _data.ToStringOrNull();

    public bool TrySet( object obj, DataInstance contextualData )
    {
        if( obj is string str )
        {
            var data = DataInstance.EditorFindFromCode( str );
            if( data != null ) 
            {
                _data = data;
                return true;
            }
        }
        if( obj is DataInstance di )
        {
            _data = di;
            return true;
        }
        return obj.TrySetOn( ref _data );
    }
}
