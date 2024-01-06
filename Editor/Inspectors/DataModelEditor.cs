using K10.EditorGUIExtention;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;

using static Colors.Console;

[CustomEditor( typeof( DataModel ), true )]
public class DataModelEditor : Editor
{
    SerializedProperty _variablesProp;
    SerializedProperty _dynamicThumbSequenceProp;
    SerializedProperty _thumbProp;

    bool _thumbRegion = false;
    bool _creationRegion = false;

    static string _newVarName = "NewVar";
    static string _newInstanceName = "NewDataInstance";

    // System.Type _typeRef = typeof(IDataInstance);

    readonly System.Type baseType = typeof(IDataInstance);
    int _baseTypeId = -1;
    int _selectedTypeId = 0;
    System.Type[] _validTypes = null;
    private string[] _validTypeNames = null;
    
    List<int> _toRemove = new List<int>();

    KReorderableList _list;

    bool _dirty = false;

    void OnEnable()
    {
        _newInstanceName = "new" + target.name;
        
        _variablesProp = serializedObject.FindProperty( "_variables" );
        _dynamicThumbSequenceProp = serializedObject.FindProperty( "_dynamicThumbSequence" );
        _thumbProp = serializedObject.FindProperty( "_thumb" );
        _list = new KReorderableList( serializedObject, _variablesProp, "Variables", IconCache.Get( "bricks" ).Texture, true, true, false, true );

        var rl = _list.List;
        rl.drawElementCallback += DrawElementCallback;
        rl.onRemoveCallback += OnRemoveCallback;
        rl.multiSelect = true;
        
        var baseType = typeof(IDataInstance);
        
        var ttps = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany( s => s.GetTypes() )
                    .Where( p => baseType.IsAssignableFrom(p) ).ToList();

        ttps.Remove( baseType );

        _validTypes = ttps.ToArray();

        // _baseTypeId = Mathf.Max( _validTypes.IndexOf( baseType ), 0 );
        _selectedTypeId = 0;

        _validTypeNames = _validTypes.ToList().ConvertAll( ( t ) => t.FullName ).ToArray();
    }

    private void OnRemoveCallback( ReorderableList list )
    {
        var inds = list.selectedIndices;
        Debug.Log( $"🗑 {"OnRemoveCallback".Colorfy( Negation )}( {string.Join( ", ", list.selectedIndices ).Colorfy( Numbers )} ) {(_dirty?"wasDirty".Colorfy( Negation ):"")}" );

        for( int i = inds.Count - 1; i >= 0; i-- )
        {
            var id = inds[i];
            var slot = _list.List.serializedProperty.GetArrayElementAtIndex( id );
            var objRef = slot.objectReferenceValue;
            var dvar = objRef as DataVariable;
            Debug.Log( $"🗑 {"OnRemoveCallback".Colorfy( Negation )}( {id.ToString().Colorfy( Numbers )} ) => {dvar.ToStringOrNull().Colorfy( TypeName )}" );
            AssetDatabase.RemoveObjectFromAsset(objRef);
            _variablesProp.DeleteArrayElementAtIndex(id);
            _dirty = true;
        }
    }

    private void DrawElementCallback( Rect rect, int index, bool isActive, bool isFocused )
    {
        var slot = _list.List.serializedProperty.GetArrayElementAtIndex( index );

        var objRef = slot.objectReferenceValue;
        var name = objRef.name;


        var targetObject = slot.objectReferenceValue;
        SerializedObject nestedObject = new SerializedObject(targetObject);
        var typeProp = nestedObject.FindProperty( "_typeRef" );
        
        var typeNameAndAssembly = typeProp.FindPropertyRelative( "_typeNameAndAssembly" );
        
        bool needModelDef = DataVariable.IsModelRequired( typeNameAndAssembly.stringValue );
        bool variableDepended = DataVariable.IsVariableDependent( typeNameAndAssembly.stringValue );

        var lineH = EditorGUIUtility.singleLineHeight;

        var newName = GUI.TextField( rect.VerticalSlice( 0, 3 ).RequestHeight( lineH ).CutLeft( lineH ), objRef.name );
        if( string.Compare( name, newName ) != 0 )
        {
            objRef.name = newName;
            _dirty = true;
            Debug.Log( $"🛐 {"Changed".Colorfy( Verbs )} varaible {"name".Colorfy(Keyword)} from {name.Colorfy( Interfaces )} to {newName.Colorfy( TypeName )}" );
        }
        var modelProp = nestedObject.FindProperty( "_model" );
        if( needModelDef ) 
        {
            var modelRect = rect.VerticalSlice( 2, 3 );
            if( modelProp.objectReferenceValue == null )
            {
                var iconSize = 18;
                var create = IconButton.Draw( modelRect.RequestLeft( iconSize ).RequestHeight( iconSize ), "match", 'C' );
                if( create ) 
                {
                    modelProp.objectReferenceValue = ScriptableObjectUtils.CreateSibiling<DataModel>( target, newName, false, false );
                    EditorGUIUtility.PingObject( modelProp.objectReferenceValue );
                    _dirty = true;
                }
                modelRect = modelRect.CutLeft( iconSize );
            }
            EditorGUI.PropertyField( modelRect.RequestHeight( lineH ), modelProp, GUIContent.none, false );
        }
        else modelProp.objectReferenceValue = null;
        var typeRect = rect.VerticalSlice( 1, 3, needModelDef ? 1 : 2 ).RequestHeight( lineH );
        EditorGUI.PropertyField( typeRect, typeProp, GUIContent.none, false );
        if( IconButton.Draw( rect.RequestLeft( lineH ).RequestHeight( lineH ), "search" ) ) 
        {
            EditorGUIUtility.PingObject( targetObject );
			// EditorUtility.FocusProjectWindow();
			// Selection.activeObject = targetObject;
        }
        nestedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        _dirty = false;
        serializedObject.Update();

        var varCount = (_variablesProp != null && _variablesProp.isArray) ? _variablesProp.arraySize : 0;
        for (int i = varCount - 1; i >= 0; i--)
        {
            var slot = _variablesProp.GetArrayElementAtIndex(i);
            var objRef = slot.objectReferenceValue;
            if (objRef != null) continue;
            _variablesProp.DeleteArrayElementAtIndex(i);
        }

        Rect controlRect = EditorGUILayout.GetControlRect();
        var nameW = EditorGUIUtility.currentViewWidth * .333333f;

        AddVarTemplate(nameW);
        K10.EditorGUIExtention.SeparationLine.Horizontal();
        _list.DoLayoutList();

        // K10.EditorGUIExtention.SeparationLine.Horizontal();

        for (int i = _toRemove.Count - 1; i >= 0; i--)
        {
            var id = _toRemove[i];
            _variablesProp.DeleteArrayElementAtIndex(id);
        }
        _toRemove.Clear();

        GUILayout.Space(EditorGUIUtility.singleLineHeight);
        DrawThumbDefinitionRegion();
        GUILayout.Space(EditorGUIUtility.singleLineHeight);
        DrawInstanceCreationRegion();

        serializedObject.ApplyModifiedProperties();

        // DrawDefaultInspector();

        SaveIfDirty();
    }

    private void DrawThumbDefinitionRegion()
    {
        GuiColorManager.New(new Color(.85f, .85f, .85f));
        GuiColorManager.New(new Color(.9f, .5f, .5f));
        _thumbRegion = EditorGUILayout.Foldout(_thumbRegion, "Deprecated thumb definition");
        GuiColorManager.Revert();
        if (_thumbRegion)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            // EditorGUILayout.LabelField( "Deprecated thumb definition", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField(_dynamicThumbSequenceProp);
            EditorGUILayout.PropertyField(_thumbProp);
            GUILayout.EndVertical();
        }
        // EditorGUILayout.EndToggleGroup();
        GuiColorManager.Revert();
    }

    private void DrawInstanceCreationRegion()
    {
        _creationRegion = EditorGUILayout.Foldout(_creationRegion, "Instance Creator");
        if (!_creationRegion) return;
        GUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.BeginVertical();
        EditorGUILayout.LabelField("Instance name:", EditorStyles.boldLabel);
        _newInstanceName = GUILayout.TextField(_newInstanceName);
        GUILayout.EndVertical();
        // var create = GUILayout.Button(new GUIContent($"🏗 Create new instance ➕", IconCache.Get("brick").Texture), GUILayout.Height(32));
        var create = GUILayout.Button($"🔥 Create new instance ➕", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2 + 2 * EditorGUIUtility.standardVerticalSpacing));
        GUILayout.EndHorizontal();
        if (create)
        {
			var data = ScriptableObjectUtils.CreateSibiling<DataInstance>( target, _newInstanceName );
            data.SetModel( target as DataModel );
			EditorUtility.SetDirty( data );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

	public static T CreateSibiling<T>( UnityEngine.Object sibiling, string name, bool focus = false ) where T : ScriptableObject
    {
        var modelPath = AssetDatabase.GetAssetPath( sibiling );
        var path = modelPath.Remove( modelPath.Length - ( ".asset".Length + sibiling.name.Length )  ) + name;
		var newSO = ScriptableObjectUtils.Create<T>( path );
        Debug.Log( $"🏗 {"Create".Colorfy( Verbs )} new {typeof(T).FullName.Colorfy( TypeName )} @ {path.Colorfy( Keyword )}" );
        return newSO;
    }

    private void SaveIfDirty()
    {
        if( !_dirty ) return;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _dirty = false;
    }

    private void AddVarTemplate( float nameW )
    {
        var defaultSpace = 4;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(defaultSpace);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(defaultSpace);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        // IconCache.Get( "brick" ).Layout( (int)EditorGUIUtility.singleLineHeight );
        _newVarName = GUILayout.TextField(_newVarName, GUILayout.Width(nameW));

        var nullType = _selectedTypeId == _baseTypeId;
        if (nullType) GuiColorManager.New(Color.red);
        _selectedTypeId = EditorGUILayout.Popup(GUIContent.none, _selectedTypeId, _validTypeNames);
        if (nullType) GuiColorManager.Revert();
        EditorGUILayout.EndHorizontal();
        var typeName = _validTypeNames != null && _selectedTypeId >= 0 && _selectedTypeId < _validTypeNames.Length ? _validTypeNames[_selectedTypeId] : "NULL";
        EditorGUILayout.BeginHorizontal();
        // IconCache.Get( "add" ).Layout( 32 );
        var create = GUILayout.Button(new GUIContent($" Add new Varaible:{typeName} {_newVarName} ➕", IconCache.Get("brick").Texture), GUILayout.Height(32));
        // IconCache.Get( "add" ).Layout( 32 );
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.Space(defaultSpace);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(defaultSpace);
        EditorGUILayout.EndVertical();


        GUILayout.Space(defaultSpace);
        
        if (create)
        {
            var typeRef = _validTypes[_selectedTypeId];

            bool success = true;
            try
            {
                var rootFilePath = AssetDatabase.GetAssetPath(target);
                if (!rootFilePath.StartsWith("Assets/") && !rootFilePath.StartsWith("Assets\\")) rootFilePath = "Assets/" + rootFilePath;

                var rootFile = AssetDatabase.LoadAssetAtPath(rootFilePath, typeof(ScriptableObject));
                if (rootFile != null)
                {
                    var asset = ScriptableObject.CreateInstance<DataVariable>();
                    asset.name = _newVarName;
                    AssetDatabase.AddObjectToAsset(asset, rootFile);
                    asset.SetType(typeRef);
                    var varCount = (_variablesProp != null && _variablesProp.isArray) ? _variablesProp.arraySize : 0;
                    _variablesProp.InsertArrayElementAtIndex(varCount);
                    var addedElement = _variablesProp.GetArrayElementAtIndex(varCount);
                    addedElement.objectReferenceValue = asset;
                    _dirty = true;
                }
            }
            catch( System.Exception ex )
            {
                success = false;
                Debug.Log( $"❌🏗❌ {"Fail".Colorfy(Negation)} to Create new variable {typeName.Colorfy( TypeName )} {_newVarName.Colorfy( Interfaces )}\n{ex}" );
            }
            if (success) Debug.Log($"🏗 {"Create".Colorfy( Verbs )} new variable {typeName.Colorfy(TypeName)} {_newVarName.Colorfy(Interfaces)}");
        }
    }

    void DrawNestedInspector( Object targetObject )
    {
        // EditorGUILayout.Space();
        // EditorGUILayout.LabelField("Nested Inspector", EditorStyles.boldLabel);

        // Use serialized object to draw the nested inspector
        SerializedObject nestedObject = new SerializedObject(targetObject);

        // Begin the vertical layout for the nested inspector
        // EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Iterate through all properties and draw them
        SerializedProperty prop = nestedObject.GetIterator();
        bool enterChildren = true;
        prop.NextVisible(enterChildren);
        while (prop.NextVisible(enterChildren))
        {
            EditorGUILayout.PropertyField( prop, GUIContent.none, true );
            enterChildren = false;
        }

        // End the vertical layout for the nested inspector
        // EditorGUILayout.EndVertical();

        // Apply modifications to the nested object
        nestedObject.ApplyModifiedProperties();
    }
}
