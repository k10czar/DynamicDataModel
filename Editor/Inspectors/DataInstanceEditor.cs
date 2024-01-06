using UnityEngine;
using UnityEditor;
using K10.EditorGUIExtention;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static Colors.Console;

[CustomEditor( typeof( DataInstance ), true )]
public class DataInstanceEditor : Editor
{
    private static readonly float BUTTON_HEIGHT_VALUE = EditorGUIUtility.singleLineHeight * 2;
    private static readonly Color ERROR_TINT_COLOR = Color.red;
    private static readonly Color WARNING_TINT_COLOR = Color.yellow;
    
    private static readonly GUILayoutOption BUTTON_HEIGHT = GUILayout.Height( EditorGUIUtility.singleLineHeight * 2 );
    private static readonly GUILayoutOption GIGANTIC_BUTTON_HEIGHT = GUILayout.Height( 80 );

    SerializedProperty _modelProp;
    SerializedProperty _variablesProp;
    
    HashSet<DataVariable> _modelVariablesSet = new HashSet<DataVariable>();
    HashSet<DataVariable> _instanceVariablesSet = new HashSet<DataVariable>();
    List<DataVariable> _modelVariablesList = new List<DataVariable>();
    List<int> _toRemove = new List<int>();

    void OnEnable()
    {
        _modelProp = serializedObject.FindProperty( "_model" );
        _variablesProp = serializedObject.FindProperty( "_variables" );
        BuildVisitedSet();
    }

    void BuildVisitedSet()
    {
        _instanceVariablesSet.Clear();
        var varCount = ( _variablesProp != null && _variablesProp.isArray ) ? _variablesProp.arraySize : 0;
        for( int i = 0; i < varCount; i++ )
        {
            var slot = _variablesProp.GetArrayElementAtIndex( i );
            var variable = slot.FindPropertyRelative( "_variable" );
            var variableRef = variable.objectReferenceValue as DataVariable;
            if( variableRef == null ) continue;
            _instanceVariablesSet.Add( variableRef );
        }
    }

    void BuildVariablesData( DataModel model )
    {
        _modelVariablesSet.Clear();
        _modelVariablesList.Clear();
        if( model != null )
        {
            var varDefCount = model.VariablesCount;
            for( int i = 0; i < varDefCount; i++ )
            {
                var variable = model.GetVariable( i );
                _modelVariablesSet.Add( variable );
                _modelVariablesList.Add( variable );
            }
        }
    }
    
    private void DrawAddVariableButton(DataVariable variable)
    {
        if( variable == null ) return;

        var addVariable = GUILayout.Button(new GUIContent($" {variable.TypeRef.ToStringOrNull()} {variable.name}", IconCache.Get("add").Texture), BUTTON_HEIGHT);

        if (addVariable)
        {
            var id = _variablesProp.arraySize;
            _variablesProp.InsertArrayElementAtIndex(id);
            var newElement = _variablesProp.GetArrayElementAtIndex(id);
            newElement.FindPropertyRelative("_variable").objectReferenceValue = variable;
            StartVariableData(variable, newElement);
            BuildVisitedSet();
        }
    }

	public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_modelProp, true);
        var model = _modelProp.objectReferenceValue as DataModel;
        var hasModel = model != null;

        // var nation = target as DataInstance;
        K10.EditorGUIExtention.SeparationLine.Horizontal();
        BuildVariablesData( model );

        if( hasModel )
        {
            ModelValidationButtons();
            NotInModelVariablesValidationButtons();
            ModelSortValidation();
        }

        var varCount = (_variablesProp != null && _variablesProp.isArray) ? _variablesProp.arraySize : 0;
        int modelVarId = 0;

        GuiLabelWidthManager.New(80);

        _toRemove.Clear();

        for (int i = 0; i < varCount; i++)
        {
            var slot = _variablesProp.GetArrayElementAtIndex(i);
            var variable = slot.FindPropertyRelative("_variable");
            var variableRef = variable.objectReferenceValue as DataVariable;

            if( hasModel && modelVarId < model.VariablesCount )
            {
                bool isLastVar = false;
                if( modelVarId > 0 )
                {
                    var lastVar = model.GetVariable( modelVarId - 1 );
                    isLastVar = lastVar == variableRef;
                }
                if( !isLastVar )
                {
                    while( modelVarId < model.VariablesCount && model.GetVariable( modelVarId ) != variableRef )
                    {
                        var modelVariable = model.GetVariable( modelVarId );
                        DrawAddVariableButton( modelVariable );
                        modelVarId++;
                    }
                    modelVarId++;
                }
            }

            EditorGUILayout.BeginHorizontal();
            var data = slot.FindPropertyRelative("_data");
            var isOnTheModel = variableRef != null && _modelVariablesSet.Contains(variableRef) || !hasModel;
            var nullData = data.managedReferenceValue == null;

            System.Type type = typeof(IDataInstance);
            if (variableRef != null) type = variableRef.TypeRef.Type;
            if( data.managedReferenceValue != null && !type.IsInstanceOfType( data.managedReferenceValue ) ) data.managedReferenceValue = null;

            if ( !isOnTheModel ) GuiColorManager.New( ERROR_TINT_COLOR );
            else if( nullData ) GuiColorManager.New( WARNING_TINT_COLOR );

            var removeVar = GUILayout.Button("Remove Var", GUILayout.Width(80));
            // var delete = IconButton.Layout( 24, "trash" );
            // GUILayout.Label( variableRef.name, GUILayout.Width( 64 ) );
            if( removeVar )
            {
                _toRemove.Add(i);
                var dataTypeDebug = "UNSETTED".Colorfy(Negation);
                var dataDebug = "NULL".Colorfy(Negation);
                try
                {
                    var refData = data.managedReferenceValue;
                    if( refData != null ) 
                    {
                        dataTypeDebug = refData.GetType().ToStringOrNull().Colorfy(TypeName);
                        dataDebug = EditorJsonUtility.ToJson( refData ).FormatAsJson( "  " );
                    }
                }
                catch( System.Exception ex )
                {
                    Debug.LogError( $"☢ {ex.Message} when 💣 {"Removed".Colorfy(Negation)} variable {variableRef.ToStringOrNull().Colorfy( Interfaces )} with data {dataTypeDebug} \n{dataDebug}");
                }
                Debug.Log( $"💣 {"Removed".Colorfy(Negation)} variable {variableRef.ToStringOrNull().Colorfy( Interfaces )} with data {dataTypeDebug} \n{dataDebug}");
            }

            var width = EditorGUIUtility.currentViewWidth;
            var restW = width - 76;
            EditorGUILayout.ObjectField(variable, GUIContent.none);
            // EditorGUILayout.ObjectField( variable, GUIContent.none, GUILayout.Width( restW * .33333f ) );


            if (data.managedReferenceValue == null)
            {
                if (GUILayout.Button("Create Data", GUILayout.Width(90)))
                {
                    var types = System.AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany( s => s.GetTypes() )
                                .Where( p => type.IsAssignableFrom(p) && !p.IsAbstract );
                    
                    var count = types.Count();
                    if( count <= 1 )
                    {
                        var selectedType = type;
                        foreach (var t in types) selectedType = t;
                        try
                        {
                            data.managedReferenceValue = System.Activator.CreateInstance( selectedType );
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"🌋 Fail to create {selectedType.FullName} with exception:{ex}");
                        }
                        Debug.Log( $"🏗 Created {selectedType.ToStringOrNull().Colorfy(TypeName)} that is the only {"non".Colorfy(Negation)} {"Abstract".Colorfy(Abstraction)} 🦖 type that implements " + type.ToStringOrNull().Colorfy(Interfaces) );
                    }
                    else
                    {
                        GenericMenu menu = new GenericMenu();

                        foreach (var t in types)
                        {
                            var pathAtt = t.GetCustomAttribute<CreationPathAttribute>();
                            var tParsed = ( pathAtt != null ? pathAtt.Path : t.ToStringOrNull() ).Replace( ".", "/" );

                            GenericMenu.MenuFunction2 onTypedElementCreatedInside = ( parameter ) => 
                            {
                                var tp = (System.Type)parameter;
                                Debug.Log( $"🏗 Create {tp.ToStringOrNull().Colorfy( Color.yellow )} on {data.serializedObject.targetObject.NameOrNull().Colorfy( Colors.Orange )} at {data.propertyPath.Colorfy( Color.green )}" );
                                var serObj = data.serializedObject;
                                serObj.Update();
                                data.managedReferenceValue = System.Activator.CreateInstance( tp );
                                serObj.ApplyModifiedProperties();
                            };
                            menu.AddItem( new GUIContent( tParsed ), false, onTypedElementCreatedInside, t );
                        }

                        menu.ShowAsContext();

                        Debug.Log( $"📚 {type.ToStringOrNull().Colorfy(Interfaces)} has {count.ToString().Colorfy(Numbers)} concrete {"subtypes".Colorfy(Keyword)}, choose on menu 📑 one of those types:\n  -<color=#{ColorUtility.ToHtmlStringRGB(TypeName)}>" + string.Join($"</color>;\n  -<color=#{ColorUtility.ToHtmlStringRGB(TypeName)}>", types ) + "</color>\n\n");
                        return;
                    }
                }
            }
            else
            {//explosion
                if (GUILayout.Button( "Clear Data", GUILayout.Width(90)))
                {
                    Debug.Log( $"🧹 {"Cleared".Colorfy(Negation)} {data.managedReferenceValue.GetType().ToStringOrNull().Colorfy(TypeName)} data on variable {variableRef.ToStringOrNull().Colorfy( Interfaces )} \n{EditorJsonUtility.ToJson( data.managedReferenceValue ).FormatAsJson( "  " ) }");
                    data.managedReferenceValue = null;
                }
            }

            // if( type == typeof(DataInstance) ) ScriptableObjectField.Layout( data, type, variableRef.name, GUILayout.Width( restW * .66666f ) );
            // else 
            // ScriptableObjectField.InsideLayout( data, type, variableRef.name, GUILayout.Width( restW * .66666f ) );
            EditorGUILayout.EndHorizontal();
            DrawChildProps(data);

            if( !isOnTheModel || nullData ) GuiColorManager.Revert();
        }
        
        while( hasModel && modelVarId < model.VariablesCount )
        {
            modelVarId++;
            DrawAddVariableButton( model.GetVariable( modelVarId - 1 ) );
        }

        for( int i = _toRemove.Count - 1; i >= 0; i-- )
        {
            var id = _toRemove[i];
            _variablesProp.DeleteArrayElementAtIndex( id );
        }
        _toRemove.Clear();

        GuiLabelWidthManager.Revert();

        var copy = _variablesProp.arraySize > 0;
        var content = copy ? new GUIContent($" duplicate last variable", IconCache.Get("copy").Texture) : new GUIContent($" add custom variable", IconCache.Get("add").Texture);
        var addVariable = GUILayout.Button( content, BUTTON_HEIGHT );
        if( addVariable )
        {
            var id = _variablesProp.arraySize;
            _variablesProp.InsertArrayElementAtIndex(id);
            // var newElement = _variablesProp.GetArrayElementAtIndex(id);
        }

        K10.EditorGUIExtention.SeparationLine.Horizontal();
        serializedObject.ApplyModifiedProperties();

        if( GUILayout.Button( "Run variable dependency propagation" ) )
        {
            EditorAssetValidationProcessWindow.RunAssetValidationInAll( typeof(DataInstance) );
        }

        // DrawDefaultInspector();
    }

    private void ModelValidationButtons()
    {
        BuildVisitedSet();

        int missingVarCount = 0;
        foreach (var variable in _modelVariablesList)
        {
            if (_instanceVariablesSet.Contains(variable)) continue;
            missingVarCount++;
        }

        if (missingVarCount > 0)
        {
            // GUILayout.Label("Add missing variables", K10GuiStyles.basicCenterStyle);
            var fullModelMissing = (missingVarCount == _modelVariablesList.Count);
            var buttonMsg = fullModelMissing ? $"Build Entire Model(Add all {missingVarCount} variables)" : $"Add {missingVarCount} missing variables";
            var buildModel = GUILayout.Button(new GUIContent(buttonMsg, IconCache.Get("checklist").Texture), GIGANTIC_BUTTON_HEIGHT);
            if (buildModel)
            {
                foreach (var variable in _modelVariablesList)
                {
                    if (_instanceVariablesSet.Contains(variable)) continue;
                    var id = _variablesProp.arraySize;
                    _variablesProp.InsertArrayElementAtIndex(id);
                    var newElement = _variablesProp.GetArrayElementAtIndex(id);
                    newElement.FindPropertyRelative("_variable").objectReferenceValue = variable;
                    StartVariableData(variable, newElement);
                    BuildVisitedSet();
                }
            }
            // foreach (var variable in _modelVariablesList)
            // {
            //     if (_instanceVariablesSet.Contains(variable)) continue;

            //     var addVariable = GUILayout.Button(new GUIContent($" {variable.TypeRef.ToStringOrNull()} {variable.name}", IconCache.Get("add").Texture), BUTTON_HEIGHT);

            //     if (addVariable)
            //     {
            //         var id = _variablesProp.arraySize;
            //         _variablesProp.InsertArrayElementAtIndex(id);
            //         var newElement = _variablesProp.GetArrayElementAtIndex(id);
            //         newElement.FindPropertyRelative("_variable").objectReferenceValue = variable;
            //         StartVariableData(variable, newElement);
            //         BuildVisitedSet();
            //     }
            // }
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button(new GUIContent("Model is fully implemented ✅", IconCache.Get("checklist").Texture), GIGANTIC_BUTTON_HEIGHT );
            EditorGUI.EndDisabledGroup();
        }
        K10.EditorGUIExtention.SeparationLine.Horizontal();
    }

    private static void StartVariableData(DataVariable variable, SerializedProperty newElement)
    {
        var type = variable.TypeRef.Type;
        var types = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && !p.IsAbstract);

        var count = types.Count();
        if (count <= 1)
        {
            var selectedType = type;
            foreach (var t in types) selectedType = t;
            try
            {
                newElement.FindPropertyRelative("_data").managedReferenceValue = System.Activator.CreateInstance(selectedType);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"📵 Fail to create {selectedType.FullName.Colorfy(Color.yellow)} with exception:{ex.ToStringOrNull().Colorfy(Colors.Orange)}");
            }
        }
        else
        {
            Debug.Log($"🤔 Cannot decide what type to create from the 📚 {count.ToString().Colorfy(Color.cyan)} types possibles for the variable {variable.ToStringOrNull().Colorfy(Color.yellow)} choose the type at instance");
        }
    }

    private static void DrawChildProps( SerializedProperty data )
    {
        string lastArray = null;
        foreach (var innProp in data)
        {
            if (innProp is SerializedProperty sp)
            {
                if (sp.isArray)
                {
                    lastArray = sp.propertyPath + ".Array.";
                }
                else if (lastArray != null)
                {
                    var isInnerArrayProp = sp.propertyPath.StartsWith(lastArray);
                    if (!isInnerArrayProp) lastArray = null;
                    else continue;
                }

                EditorGUILayout.PropertyField(sp, true);
                if( sp.propertyType == SerializedPropertyType.ObjectReference )
                {
                    var objRef = sp.objectReferenceValue;
                    if( objRef is Texture2D texture )
                    {
                        float inspectorWidth = EditorGUIUtility.currentViewWidth;

                        var w = Mathf.Min( inspectorWidth - 32, texture.width );
                        var h = texture.height * w / texture.width;
                        GUILayout.Label( texture, GUILayout.Width( w ), GUILayout.Height( h ) );
                    }
                }
            }
        }
    }

    private void ModelSortValidation()
    {
        bool sorted = true;
        var varCount = (_variablesProp != null && _variablesProp.isArray) ? _variablesProp.arraySize : 0;

        int itModel = 0;
        bool inModelIt = true;

        var sortLog = string.Empty;

        for (int i = 0; i < varCount; i++)
        {
            var slot = _variablesProp.GetArrayElementAtIndex(i);
            var variable = slot.FindPropertyRelative("_variable");
            var variableRef = variable.objectReferenceValue as DataVariable;

            var inModel = _modelVariablesSet.Contains( variableRef );
            if( inModel )
            {
                if( !inModelIt )
                {
                    sorted = false;
                    sortLog = $"❌ Cannot have out of model 🧾 before in model variable: {variableRef.ToStringOrNull().Colorfy( Interfaces )}";
                    break;
                }

                bool foundAfter = false;
                while( _modelVariablesList.Count > itModel )
                {
                    var modelVar = _modelVariablesList[ itModel ];
                    if( variableRef == modelVar ) 
                    {
                        foundAfter = true;
                        break;
                    }
                    itModel++;
                }

                if( !foundAfter )
                {
                    sorted = false;
                    sortLog = $"🔍 Does not found {variableRef.ToStringOrNull().Colorfy( Interfaces )} in model 🧾 or is {"out".Colorfy(Negation)} of order 🔀";
                    break;
                }
            }
            inModelIt = inModel;
        }

        if( sorted ) return;
        
        int moves = 0;
        // GUILayout.Label( sortLog );
        // if (GUILayout.Button(new GUIContent($"Sort variables", IconCache.Get("sort").Texture), BUTTON_HEIGHT))
        // {
            var it = 0;
            for( int i = 0; i < _modelVariablesList.Count; i++ )
            {
                var modelVariable = _modelVariablesList[i];
                for( int j = 0; j < varCount; j++ )
                {
                    var slot = _variablesProp.GetArrayElementAtIndex(j);
                    var variable = slot.FindPropertyRelative("_variable");
                    var instanceVariableRef = variable.objectReferenceValue as DataVariable;
                    if( instanceVariableRef == modelVariable )
                    {
                        if( j != it )
                        {
                            _variablesProp.MoveArrayElement( j, it );
                            moves++;
                        }
                        it++;
                    }
                }
            }
        // }
        // K10.EditorGUIExtention.SeparationLine.Horizontal();

        Debug.Log( $"🔀 Sorted variables on {target.NameOrNull().Colorfy( Interfaces )} moving {moves.ToString().Colorfy( Numbers )} variables\n{"Sort Reason".Colorfy(Negation)}: {sortLog}" );
    }

    private void NotInModelVariablesValidationButtons()
    {
        var notInModelVariables = 0;
        var varCount = (_variablesProp != null && _variablesProp.isArray) ? _variablesProp.arraySize : 0;
        for (int i = 0; i < varCount; i++)
        {
            var slot = _variablesProp.GetArrayElementAtIndex(i);
            var variable = slot.FindPropertyRelative("_variable");
            var variableRef = variable.objectReferenceValue as DataVariable;
            var isOnTheModel = variableRef != null && _modelVariablesSet.Contains(variableRef);
            if (isOnTheModel) continue;
            notInModelVariables++;
        }

        if (notInModelVariables > 0)
        {
            GuiColorManager.New(ERROR_TINT_COLOR);
            if (GUILayout.Button( new GUIContent( $" Remove {notInModelVariables} not in the model 🧾 Variables", IconCache.Get("broom32").Texture), BUTTON_HEIGHT))
            {
                _toRemove.Clear();
                for (int i = 0; i < varCount; i++)
                {
                    var slot = _variablesProp.GetArrayElementAtIndex(i);
                    var variable = slot.FindPropertyRelative("_variable");
                    var variableRef = variable.objectReferenceValue as DataVariable;
                    var isOnTheModel = variableRef != null && _modelVariablesSet.Contains(variableRef);
                    if (isOnTheModel) continue;
                    _toRemove.Add(i);
                }
                for (int i = _toRemove.Count - 1; i >= 0; i--)
                {
                    var id = _toRemove[i];
                    _variablesProp.DeleteArrayElementAtIndex(id);
                }
                _toRemove.Clear();
            }
            GuiColorManager.Revert();
            K10.EditorGUIExtention.SeparationLine.Horizontal();
        }
    }
}