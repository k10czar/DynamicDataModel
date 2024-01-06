using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static Colors.Console;

[System.Serializable]
public class DataInstanceRefCollection : IDataInstance
{
    [SerializeField] DataInstance[] _dataCollection;

    public int DataCount => _dataCollection.Length;
    public DataInstance GetData( int index ) => _dataCollection[index];

    string[] FIND_OR_CREATE_COMMANDS = new string[]{ "FindOrCreate", "FOC" };

    private bool TryAdd( DataInstance data )
    {
        if( _dataCollection?.Contains( data ) ?? false ) return false;
        _dataCollection = _dataCollection.With( data );
        return true;
    }

    public bool TrySet( object obj, DataInstance contextualData )
    {
        if( obj is string str )
        {
            if( str.IsCommand( out var parameter, FIND_OR_CREATE_COMMANDS ) )
            {
                var code = parameter.Split( ':' );
                if( code.Length >= 2 )
                {
                    var added = false;
                    var modelName = code[0];
                    var elements = code[1].Split( ',', System.StringSplitOptions.RemoveEmptyEntries );
                    var model = AssetDatabaseUtils.GetFirst<DataModel>( modelName );
                    for( int i = 0; i < elements.Length; i++ )
                    {
                        var elementName = elements[i].SanitizeFileName();
                        var elementRef = DataInstance.EditorFindFromCode( $"{elementName}:{modelName}" );
                        if( elementRef == null )
                        {
#if UNITY_EDITOR
                            var contextPath = UnityEditor.AssetDatabase.GetAssetPath( contextualData );
                            int pi = contextPath.Length - 1;
                            for( ; pi >= 0; pi-- ) if( contextPath[pi] == '/' || contextPath[pi] == '\\' ) break;
                            contextPath = contextPath.Substring( 0, pi + 1 );
                            var path = $"{contextPath}{modelName}/{elementName}".SanitizePathName();
		                    elementRef = ScriptableObjectUtils.Create<DataInstance>( path, false, false );
                            if( elementRef != null )
                            {
                                elementRef.SetModel( model );
                                UnityEditor.EditorUtility.SetDirty( elementRef );
                                Debug.Log( $"Created {path.Colorfy(Names)} because {"Fail".Colorfy( Negation )} find element: {elementName.Colorfy( TypeName )}:{modelName.Colorfy( Interfaces )}" );
                            }
                            else Debug.LogError( $"{"Fail".Colorfy( Verbs )} to create {path.Colorfy(Names)}" );
#endif
                        }
                        added |= TryAdd( elementRef );
                    }
                    return added;
                }
                Debug.LogError( $"{"TrySet".Colorfy( Verbs )}( {obj.ToStringOrNull().Colorfy( TypeName )} ) {"Fail".Colorfy( Negation )} to parse command: {str.Colorfy( Names )}\nsintaxe: \"{FIND_OR_CREATE_COMMANDS[0]}:model:...\"\nCurrent: {string.Join( ":", code )}" );
                return false;
            }
            var data = DataInstance.EditorFindFromCode( str );
            if( data != null ) 
            {
                TryAdd( data );
                return true;
            }
        }
        if( obj is IEnumerable<DataInstance> dis )
        {
            _dataCollection = dis.ToArray();
            return true;
        }
        else if( obj is DataInstance di )
        {
            _dataCollection = new DataInstance[]{ di };
            return true;
        }
        return obj.TrySetOn( ref _dataCollection );
    }
}
