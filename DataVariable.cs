
using System;
using UnityEngine;

using static Colors.Console;

public static class DataVariableExtensions
{
    public static string ToStringColored(this DataVariable dvar)
    {
        if( dvar == null ) return "NULL".Colorfy( Negation );
        return $"{dvar.TypeRef?.Type?.FullName.Colorfy( TypeName ) ?? "NOT_SETTED".Colorfy( Negation )} {dvar.name.Colorfy( Interfaces )}";
    }
}

public class DataVariable : ScriptableObject
{
    // const string ASSEMBLY = "DataModel";
    [SerializeField,TypeReferences.Inherits(typeof(IDataInstance))] TypeReferences.TypeReference _typeRef;
    [SerializeField] DataVariable _dependentVariable;
    [SerializeField] DataModel _model;

    public TypeReferences.TypeReference TypeRef => _typeRef;
    public DataModel Model => _model;
    public DataVariable DependentVariable => _dependentVariable;

    public void SetType( Type typeRef )
    {
        if( _typeRef == null ) _typeRef = new TypeReferences.TypeReference( typeRef );
        else _typeRef.Type = typeRef;
    }

    static string[] _modelTypeNames = null;
    static Type[] _modelTypes = null; //new Type[]{ typeof( DataInstanceRef ), typeof( DataInstanceRefCollection ), typeof( DataInstanceRefWeightedCollection ) };

    public static bool IsModelRequired( Type type )
    {
        StartModelTypes();
        for (int i = 0; i < _modelTypes.Length; i++) if (type == _modelTypes[i]) return true;
        return false;
    }

    private static void StartModelTypes()
    {
        if (_modelTypes == null) _modelTypes = new Type[] { typeof(DataInstanceRef), typeof(DataInstanceRefCollection), typeof(DataInstanceRefWeightedCollection) };
    }

    public static bool IsVariableDependent( Type type )
    {
        if( type == null ) return false;
        // Debug.Log( $"IsVariableDependent( {type.ToStringOrNull().Colorfy( Colors.Console.Types )} ) ? {type.GetInterface( typeof(IDataDependent).Name ).ToStringOrNull()}" );
        if( type.GetInterface( typeof(IDataDependent).Name ) != null ) return true;
        return false;
    }

    public static bool IsVariableDependent( string typeNameAndAssembly )
    {
        if( string.IsNullOrEmpty( typeNameAndAssembly ) ) return false;
        var type = Type.GetType( typeNameAndAssembly );
        return IsVariableDependent( type );
    }

    public static bool IsModelRequired( string typeNameAndAssembly )
    {
        if( _modelTypeNames == null )
        {
            StartModelTypes();
            _modelTypeNames = new string[ _modelTypes.Length ];
            for( int i = 0; i < _modelTypes.Length; i++ ) _modelTypeNames[i] = FormatTypeNameAndAssembly( _modelTypes[i] );
        }
        for( int i = 0; i < _modelTypeNames.Length; i++ ) if( string.CompareOrdinal( typeNameAndAssembly, _modelTypeNames[i] ) == 0 ) return true;
        return false;
    }

    private static string FormatTypeNameAndAssembly( Type type ) => $"{type.FullName}, {GetShortAssemblyName(type)}";
    public static string GetShortAssemblyName( Type type )
    {
        string assemblyFullName = type.Assembly.FullName;
        int commaIndex = assemblyFullName.IndexOf(',');
        return assemblyFullName.Substring(0, commaIndex);
    }

    public override string ToString() => $"{_typeRef?.Type?.FullName ?? "NOT_SETTED"} {name}";

    public static bool IsVariableDependencyValid( DataVariable dependentVariable, string typeNameAndAssembly )
    {
        if( dependentVariable == null ) return false;
        if( string.IsNullOrEmpty( typeNameAndAssembly ) ) return false;
        var type = Type.GetType( typeNameAndAssembly );
        return IsVariableDependencyValid( dependentVariable, type );
    }

    public static bool IsVariableDependencyValid( DataVariable dependentVariable, Type type )
    {
        if( dependentVariable == null ) return false;
        if( type == null ) return false;
        var varType = dependentVariable.TypeRef.Type;
        if( varType == null ) return false;
        var inst = System.Activator.CreateInstance( type );
        var dd = inst as IDataDependent;
        // Debug.Log( $"IsVariableDependencyValid( {dependentVariable.ToStringColored()}, {type.ToStringOrNull().Colorfy( Colors.Console.Types )} ) ? {inst.ToStringOrNull()} -> {dd.ToStringOrNull()}" );
        if( dd == null ) return false;
        // Debug.Log( $"{varType.ToStringOrNull().Colorfy( Colors.Console.Types )} IsInstanceOfType {dd.GetDependentType().ToStringOrNull().Colorfy( Colors.Console.Types )}" );
        return varType.IsAssignableFrom( dd.GetDependentType() );
    }
}