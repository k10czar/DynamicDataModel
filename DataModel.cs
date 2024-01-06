using System;
using UnityEngine;

[CreateAssetMenu( fileName = "DataModel", menuName = "Data/Model", order = 1 )]
public class DataModel : ScriptableObject
{
    [SerializeField] DataVariable[] _variables;
#if UNITY_EDITOR
    [SerializeField] DataVariable[] _dynamicThumbSequence;
    [SerializeField] Texture2D _thumb;
#endif

    public int VariablesCount => _variables.Length;
    public DataVariable GetVariable( int index ) => _variables[index];

    public Texture2D GetThumb( DataInstance data )
    {
#if UNITY_EDITOR
        if( _dynamicThumbSequence != null )
        {
            for( int i = 0; i < _dynamicThumbSequence.Length; i++ )
            {
                var dynamicThumbVar = _dynamicThumbSequence[i];
                var varData = data.GetVariableInstance( dynamicThumbVar );
                if( varData != null )
                {
                    // Debug.Log( $"{data.NameOrNull()}.GetThumb() from {dynamicThumbVar.ToStringColored()} {varData.ToStringOrNull()}" );
                    if( varData is DataInstanceRef refToSO )
                    {
                        
                        // Debug.Log( $"{data.NameOrNull()}.GetThumb() from {dynamicThumbVar.ToStringColored()} {varData.ToStringOrNull()} {refToSO.ToStringOrNull()}" );
                        if( refToSO != null && refToSO.Data != null )
                        {
                            var thumb = refToSO.Data.GetThumb();
                            if( thumb != null ) return thumb;
                        }
                    }
                    else if( varData is DataInstanceRefCollection collectionRefToSO )
                    {
                        var count = collectionRefToSO.DataCount;
                        for( int j = 0; j < count; j++ )
                        {
                            var dataCol = collectionRefToSO.GetData( j );
                            if( dataCol == null ) continue;
                            var thumb = dataCol.GetThumb();
                            if( thumb != null ) return thumb;
                        }
                    }
                    else if( varData is ImageData img )
                    {
                        if( img.Texture != null ) return img.Texture;
                    }
                }
            }
        }
        return _thumb;
#else
        return null;
#endif
    }
}
