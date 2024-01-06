using UnityEngine;
using UnityEditor;

using static Colors.Console;

[CustomEditor( typeof( DataVariable ), true )]
public class DataVariableEditor : Editor
{
    SerializedProperty _typeRefProp;
    SerializedProperty _modelProp;
    SerializedProperty _dependentVariableProp;
    
    void OnEnable()
    {
        _typeRefProp = serializedObject.FindProperty( "_typeRef" );
        _modelProp = serializedObject.FindProperty( "_model" );
        _dependentVariableProp = serializedObject.FindProperty( "_dependentVariable" );
    }
    
	public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField( _typeRefProp );

        var GUID = _typeRefProp.FindPropertyRelative( "GUID" );
        var _typeNameAndAssembly = _typeRefProp.FindPropertyRelative( "_typeNameAndAssembly" );
        
        bool needModelDef = DataVariable.IsModelRequired( _typeNameAndAssembly.stringValue );
        if( !needModelDef ) _modelProp.objectReferenceValue = null;
        EditorGUI.BeginDisabledGroup( !needModelDef );
        EditorGUILayout.PropertyField( _modelProp );
        EditorGUI.EndDisabledGroup();

        var type = System.Type.GetType( _typeNameAndAssembly.stringValue );
        bool variableDepended = DataVariable.IsVariableDependent( type );
        if( !variableDepended ) _dependentVariableProp.objectReferenceValue = null;
        EditorGUI.BeginDisabledGroup( !variableDepended );
        EditorGUILayout.PropertyField( _dependentVariableProp );
        if( _dependentVariableProp.objectReferenceValue != null && !DataVariable.IsVariableDependencyValid( _dependentVariableProp.objectReferenceValue as DataVariable, type ) ) 
        {
            Debug.LogError( $"{_dependentVariableProp.objectReferenceValue.ToStringOrNull().Colorfy( Interfaces )} is {"not".Colorfy( Negation )} instance of {type.ToStringOrNull().Colorfy( TypeName )}" );
            _dependentVariableProp.objectReferenceValue = null;
        }

        EditorGUI.EndDisabledGroup();
        serializedObject.ApplyModifiedProperties();

        if( variableDepended && GUILayout.Button( "Run variable dependency propagation" ) )
        {
            EditorAssetValidationProcessWindow.RunAssetValidationInAll( typeof(DataInstance) );
        }
    }
}
