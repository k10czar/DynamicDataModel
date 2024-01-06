using System.Collections.Generic;
using UnityEngine;

public interface ISortable
{
    void Sort();
}

[System.Serializable]
public abstract class YearlyValueDataCollection<T> : IDataInstance, ISortable where T : System.IComparable<T>
{
    [SerializeField] List<Data> _dataCollection = new List<Data>();

    private static readonly DefaultSort DEFAULT_SORT = new DefaultSort();

    public int DataCount => _dataCollection.Count;
    public Data GetData( int index ) => _dataCollection[index];
    public bool GetDataFromYear( short year, ref T value )
    {
        for( int i = 0; i < _dataCollection.Count; i++ )
        {
            if( _dataCollection[i].year == year )
            {
                value = _dataCollection[i].value;
                return true;
            }
        }
        return false;
    }

    public void Sort()
    {
        _dataCollection.Sort( DEFAULT_SORT );
    }

    public bool GetMostRecent( ref Data data )
    {
        var hasData = DataCount > 0;
        if( hasData ) data = _dataCollection[0];
        return hasData;
    }

    public bool TrySet( object obj, DataInstance contextualData )
    {
        Debug.Log( $"YearlyValueDataCollection<{typeof(T).FullName}>.TrySet( {obj.ToStringOrNull()} )" );
        var auxData = new Data();
        if( obj.TrySetOn( ref auxData, true ) || obj.TrySetOn( ref auxData.year, ref auxData.value, true ) ) 
        {
            Debug.Log( $"TrySet<(short,{typeof(T).FullName})>( {obj.ToStringOrNull()} )" );
            Set( auxData );
            return true;
        }
        if( obj is System.Collections.IEnumerable enumerable ) 
        {
            Debug.Log( $"TrySet<IEnumerable>( {obj.ToStringOrNull()} )" );
            foreach( var entry in enumerable ) 
            {
                if( obj.TrySetOn( ref auxData ) || obj.TrySetOn( ref auxData.year, ref auxData.value ) ) 
                    Set( auxData );
            }
            return true;
        }
        return obj.TrySetOn( ref _dataCollection );
    }

    private void Set( Data entry )
    {
        Debug.Log( $"Set( {entry.year}, {entry.value} )" );
        var id = 0;
        bool replace = false;
        for( ; id < _dataCollection.Count; id++ )
        {
            var queriedYear = _dataCollection[id].year;
            if( queriedYear <= entry.year ) 
            {
                if( queriedYear == entry.year ) replace = true;
                break;
            }
        }
        if( replace ) _dataCollection[id] = entry;
        else _dataCollection.Insert( id, entry );
    }

    [System.Serializable]
    public struct Data
    {
        public T value;
        public short year;
    }

    public class DefaultSort : IComparer<Data>
    {
        public int Compare( Data x, Data y )
        {
            var yearComp = y.year.CompareTo( x.year );
            if( yearComp == 0 ) return y.value.CompareTo( x.value );
            return yearComp;
        }
    }
}