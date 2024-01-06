using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static Colors.Console;


[CreateAssetMenu( fileName = "DataInstance", menuName = "Data/Instance", order = 1 )]
public class DataInstance : ScriptableObject, IEditorAssetValidationProcess
{
    // [SerializeField] bool _canHoldNonModelVars = false; // Do Asset Validation
    [SerializeField] DataModel _model;
    [SerializeField] VariableSlot[] _variables;

    public DataModel Model => _model;

    public int VariablesCount => _variables.Length;
    public IDataInstance GetVariable(int index, out DataVariable variable)
    {
        var slot = _variables[index];
        variable = slot.Variable;
        return slot.Data;
    }

    [System.Serializable]
    private struct VariableSlot
    {
        [SerializeField] DataVariable _variable;
        [SerializeReference] IDataInstance _data;

        public readonly DataVariable Variable => _variable;
        public readonly IDataInstance Data => _data;

        public void InitVar( DataVariable variable )
        {
            if( _variable != variable ) 
            {
                _data = null;
                _variable = variable;
            }
            InitVarData();
        }

        public void InitVarData()
        {
            if( _variable == null || _variable.TypeRef == null || _variable.TypeRef.Type == null ) return;
            _data = System.Activator.CreateInstance( _variable.TypeRef.Type ) as IDataInstance;
        }

        public override string ToString()
        {
            return $"[ {_variable.ToStringOrNull()}:{_data.ToStringOrNull()} ]";
        }
    }

#if UNITY_EDITOR
    public bool EDITOR_ExecuteAssetValidationProcess()
    {
        bool changed = false;
        var model = _model;
        if( _model != null )
        {
            for( int i = 0; i < _model.VariablesCount; i++ )
            {
                var modelVariable = _model.GetVariable( i );
                var depVar = modelVariable.DependentVariable;
                if( depVar == null ) continue;
                var varData = GetVariableInstance( depVar );
                if( varData == null ) continue;
    
                IDataDependent dataWithDependency = null;

                for( int vi = 0; vi < _variables.Length; vi++ )
                {
                    var variable = _variables[vi];
                    if( variable.Variable != modelVariable ) continue;
                    dataWithDependency = variable.Data as IDataDependent;
                }

                if( dataWithDependency == null )
                {
                    var newSlot = new VariableSlot();
                    newSlot.InitVar( modelVariable );
                    _variables = _variables.With( newSlot );
                    dataWithDependency = newSlot.Data as IDataDependent;
                }

                var dataChanged = dataWithDependency.Feed( varData );
                changed |= dataChanged;
                if( dataChanged ) Debug.Log( $"{name.Colorfy( Names )} has {modelVariable.ToStringColored()} dependent on {depVar.ToStringColored()} and has changed data" );
            }
        }
        else
        {
            for( int i = 0; i < _variables.Length; i++ )
            {
                var variable = _variables[i];
                var tVar = variable.Variable;
                var depVar = tVar.DependentVariable;
                if( depVar == null ) continue;
                var varData = GetVariableInstance( depVar );
                if( varData == null ) continue;
                var dataWithDependency = variable.Data as IDataDependent;
                if( dataWithDependency == null )
                {
                    var isDependent = typeof( IDataDependent).IsAssignableFrom( tVar.TypeRef.Type );
                    if( !isDependent ) continue;
                    variable.InitVarData();
                    dataWithDependency = variable.Data as IDataDependent;
                    if( dataWithDependency == null ) continue;
                    Debug.Log( $"{name.Colorfy( Names )} has {variable.Variable.ToStringColored()} dependent on {depVar.ToStringColored()} and hasnt data before" );
                    _variables[i] = variable;
                }
                changed |= dataWithDependency.Feed( varData );
            }
        }
        return changed;
    }
#endif

    private static readonly StringBuilder SB = new StringBuilder();

    public static DataInstance EditorFindFromCode( string code )
    {
        if( !code.Contains( ":" ) ) return null;
        var fields = code.Split( ":" );
        if( fields.Length != 2 ) return null;
        var dataName = fields[0];
        var modelName = fields[1];
        return EditorFind( dataName, modelName );
    }

    public static DataInstance EditorFind( string name, string modelName = null )
    {
#if UNITY_EDITOR
        var lowerName = modelName?.ToLower() ?? null;
        string[] assetNames = UnityEditor.AssetDatabase.FindAssets( $"{name} t:{typeof(DataInstance).Name}" );
        SB.Clear();
        var modelNameDebug = modelName != null ? modelName.Colorfy( TypeName ) : "NULL".Colorfy( Negation );
        for( int i = 0; i < assetNames.Length; i++ )
        {
            var SOpath = UnityEditor.AssetDatabase.GUIDToAssetPath( assetNames[i] );
            if( !SOpath.AsPathIsFilename( name ) ) continue;
            var element = UnityEditor.AssetDatabase.LoadAssetAtPath<DataInstance>( SOpath );
            if( modelName != null && element.Model != null && string.Compare( element.Model.name, modelName, System.StringComparison.OrdinalIgnoreCase ) != 0 ) 
            {
                SB.AppendLine( $"   {SOpath.Colorfy( Names )}" );
                continue;
            }
            Debug.Log( $"{"EditorFind".Colorfy( Verbs )}( {name.Colorfy( Keyword )}, {modelNameDebug} ) found @ {SOpath.Colorfy( Names )}" );
            return element;
        }
        Debug.LogError( $"{"EditorFind".Colorfy( Verbs )}( {name.ToStringOrNull().Colorfy( Keyword )}, {modelNameDebug} ) cannot find any {"DataInstance".Colorfy( TypeName )} that does match booth criteria\nCandidates with matching name:\n{SB.ToString()}" );
#else
        SB.Clear();
        Debug.LogError( $"{"EditorFind".Colorfy( Verbs )}( {name.ToStringOrNull().Colorfy( Keyword )}, {modelNameDebug} ) and this method cannot be called outside the editor" );
#endif
        return null;
    }

    public void SetModel( DataModel model )
    {
        _model = model;
    }

    public bool SetVariableData( DataVariable variable, object obj )
    {
        Debug.Log( $"{name.Colorfy( Names )}.{"SetVariableData".Colorfy( Verbs )}( {variable.ToStringColored()}, {obj.ToStringOrNull().Colorfy( Numbers )} )" );
        if( variable == null ) return false;
        if( _variables != null )
        {
            for( int i = 0; i < _variables.Length; i++ )
            {
                var varSlot = _variables[i];
                if( variable != varSlot.Variable ) continue;
                // Debug.Log( $"{this.NameOrNull()}.GetVariableInstance( {variable.ToStringColored()} ) @[{i}] => {varSlot.Data.ToStringOrNull()}" );
                var data = varSlot.Data;
                if( data == null ) varSlot.InitVar( variable );
                var setted = varSlot.Data.TrySet( obj, this );
                if( setted )
                {
                    _variables[i] = varSlot;
                    return true;
                }
                return setted;
            }
        }
        if( _model != null )
        {
            var modelVarsCount = _model.VariablesCount;
            for( int i = 0; i < modelVarsCount; i++ )
            {
                var modelVariable = _model.GetVariable( i );
                if( variable != modelVariable ) continue;
                var newSlot = new VariableSlot();
                newSlot.InitVar( variable );
                var setted = newSlot.Data.TrySet( obj, this );
                var newVars = _variables?.ToList() ?? new List<VariableSlot>();
                newVars.Add( newSlot );
                _variables = newVars.ToArray();
                SortVariables();
                return setted;
            }
        }
        return false;
    }

// #if UNITY_EDITOR
//     // Texture2D DEFAULT_ICON_TEXTURE = null;

//     // public void OnValidate()
//     // {
//     //     var thumb = GetThumb();
//     //     if( thumb == null ) thumb = DEFAULT_ICON_TEXTURE ?? ( DEFAULT_ICON_TEXTURE = Resources.Load<Texture2D>( "Icons/DataInstance Icon" ) );
//     //     Debug.Log( $"{AssetDatabase.GetAssetPath(this)} icon: {UnityEditor.EditorGUIUtility.GetIconForObject( this ).NameOrNull()} to {thumb.NameOrNull()}" );
//     //     UnityEditor.EditorGUIUtility.SetIconForObject( this, thumb );
//     // }
// #endif

    public Texture2D GetThumb()
    {
#if UNITY_EDITOR
        if( _model != null ) return _model.GetThumb( this );
#endif
        return null;
    }

    public override string ToString()
    {
        return $"{_model.NameOrNull()} {{ {(_variables == null ? "NULL" : string.Join( ", ", _variables ))} }}";
    }

    public IDataInstance GetVariableInstance( DataVariable dvar )
    {
        if( dvar == null ) return null;
        for( int i = 0; i < _variables.Length; i++ )
        {
            var varSlot = _variables[i];
            if( dvar != varSlot.Variable ) continue;
            // Debug.Log( $"{this.NameOrNull()}.GetVariableInstance( {dvar.ToStringColored()} ) @[{i}] => {varSlot.Data.ToStringOrNull()}" );
            return varSlot.Data;
        }
        return null;
    }

    public IDataInstance GetVariableInstance( string varName, out DataVariable variable )
    {
        for( int i = 0; i < _variables.Length; i++ )
        {
            var varSlot = _variables[i];
            var varDef = varSlot.Variable;
            if( varDef == null ) continue;
            if( varDef.name != varName ) continue;
            variable = varDef;
            return varSlot.Data;
        }
        variable = null;
        return null;
    }

    public IDataInstance GetVariableInstance( string varName, System.Type type, out DataVariable variable )
    {
        var filterType = type != null;
        for( int i = 0; i < _variables.Length; i++ )
        {
            var varSlot = _variables[i];
            var varDef = varSlot.Variable;
            if( varDef == null ) continue;
            if( varDef.name != varName ) continue;
            if( filterType && ( type != ( varDef.TypeRef?.Type ?? null ) ) ) continue;
            variable = varDef;
            return varSlot.Data;
        }
        variable = null;
        return null;
    }

    public bool SortVariables()
    {
        var it = 0;
        int moves = 0;

        if( _model == null ) return false;

        var modelVarsCount = _model.VariablesCount;
        var varCount = _variables.Length;

        for( int i = 0; i < modelVarsCount; i++ )
        {
            var modelVariable = _model.GetVariable( i );
            for( int j = 0; j < varCount; j++ )
            {
                var slot = _variables[j];
                if( slot.Variable == modelVariable )
                {
                    if( j != it )
                    {
                        _variables[j] = _variables[it];
                        _variables[it] = slot;
                        moves++;
                    }
                    it++;
                }
            }
        }

        if( moves == 0 ) return false;

        Debug.Log( $"🔀 Sorted variables at {this.NameOrNull().Colorfy( Interfaces )} moving {moves.ToString().Colorfy( Numbers )} variables" );

        return true;
    }
}