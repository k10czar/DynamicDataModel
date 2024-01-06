using UnityEngine;

[System.Serializable]
public class DataInstanceRefWeightedCollection : IDataInstance
{
    [SerializeField] Weighted<DataInstance>[] _dataCollection;

    public int DataCount => _dataCollection.Length;
    public DataInstance GetData( int index ) => GetWeightedData( index )?.Value ?? null;
    public Weighted<DataInstance> GetWeightedData( int index ) => index < DataCount && index >= 0 ? _dataCollection[index] : null;

    public DataInstance GetRandom() => GetData( Weighted.RandomID( _dataCollection ) );

    public bool TrySet( object obj, DataInstance contextualData )
    {
        if( obj is string str )
        {
            string[] splitedData = null;
            if( str.Contains( "% " ) ) splitedData = str.Split( "% " );
            if( str.Contains( "%" ) ) splitedData = str.Split( "%" );
            if( splitedData.Length == 2 )
            {
                var data = DataInstance.EditorFindFromCode( splitedData[1] );
                if( data != null && float.TryParse( splitedData[0], out var pct ) ) 
                {
                    var weight = pct / 100;
                    var contains = false;
                    for( int i = 0; i < _dataCollection.Length; i++ ) 
                    {
                        var itData = _dataCollection[i];
                        contains |= itData.Value == data;
                        if( !contains ) continue;
                        itData.SetWeight( weight );
                        break;
                    }
                    if( !contains ) _dataCollection = _dataCollection.With( new Weighted<DataInstance>( data, weight ) );
                    return true;
                }
            }
        }
        // if( obj is IEnumerable<DataInstance> dis )
        // {
        //     _dataCollection = dis.ToArray();
        //     return true;
        // }
        // else if( obj is DataInstance di )
        // {
        //     _dataCollection = new DataInstance[]{ di };
        //     return true;
        // }
        return obj.TrySetOn( ref _dataCollection );
    }
}