using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Reflection;

public class DataExtractorEditorWindow : EditorWindow
{
	string selectionLabel = "";
	System.Type _commonAncestor = null;

	Dictionary<System.Type,int> _typeCounter = new Dictionary<System.Type, int>();
	Dictionary<DataModel,int> _modelCounter = new Dictionary<DataModel, int>();

	string _data = "UNSETED";
	string _format = "{0} {1}";
	StringBuilder _sb = new StringBuilder();
    private Vector2 _scroll;

	List<List<int>> _vars = new List<List<int>>(){ new List<int>{ 0 }, new List<int>{ 0 } };

    [MenuItem( "K10/Data/Extractor" )] private static void Init() { GetWindow<DataExtractorEditorWindow>( "Extractor" ); }

    void Update()
	{
		Repaint();
	}

	private void OnGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label( selectionLabel );
		GUILayout.EndHorizontal();

        K10.EditorGUIExtention.SeparationLine.Horizontal();

		if( _commonAncestor != null )
		{
			GUILayout.Label( string.Join( "\\", GetAncestors( _commonAncestor ).ToList().ConvertAll( ( t ) => t.FullName ).ToArray() ) );
		}

		bool isDirty = false;
		
        K10.EditorGUIExtention.SeparationLine.Horizontal();
		var newFormat = GUILayout.TextArea( _format );
		if( newFormat != _format )
		{
			_format = newFormat;
			isDirty = true;
		}
        K10.EditorGUIExtention.SeparationLine.Horizontal();

		if( _commonAncestor != null )
		{
			for( int vi = 0; vi < _vars.Count; vi++ )
			// foreach( var path in _vars )
			{
				GUILayout.BeginHorizontal();
				if( GUILayout.Button( "-", GUILayout.Width( 16 ) ) ) 
				{
					_vars.RemoveAt( vi );
					vi--;
					continue;
				}
				var path = _vars[vi];
				var type = _commonAncestor;
				for( int i = 0; i < path.Count; i++ )
				{
					var id = path[i] + 1;
					var fields = GetMembers( type );
					var newID = EditorGUILayout.Popup( id, GetFieldsSelectionNames( fields ), GUILayout.Width( 256 ) );
					newID--;
					if( path[i] != newID )
					{
						isDirty = true;
						if( newID < 0 ) 
						{
							path.RemoveRange( i, path.Count - i );
							break;
						}
						else
						{
							path[i] = newID;
							path.RemoveRange( i + 1, path.Count - ( i + 1 ) );
						}
					}
					var selected = fields[newID];
					if( selected is FieldInfo fi ) type = fi.FieldType;
					if( selected is PropertyInfo pi ) type = pi.PropertyType;
				}
				var newFields = GetMembers( type );
				if( newFields != null && newFields.Length > 0 )
				{
					var newID = EditorGUILayout.Popup( 0, GetFieldsSelectionNames( newFields ), GUILayout.Width( 256 ) );
					newID--;
					if( newID >= 0 )
					{
						path.Add( newID );
					}
				}
				GUILayout.EndHorizontal();
			}
			if( GUILayout.Button( "AddVariable", GUILayout.Width( 256 ) ) ) 
			{
				_vars.Add( new List<int>() );
			}
		}

		_scroll = EditorGUILayout.BeginScrollView( _scroll );

		if( isDirty )
		{
			UpdateData();
			// Debug.Log( "Changed" );
		} 
		GUILayout.TextArea( _data, GUILayout.ExpandHeight( true ) );
		EditorGUILayout.EndScrollView();
		// foreach( var kvp in _typeCounter )
		// {
		// 	GUILayout.Label( string.Join( ",", GetAncestors( kvp.Key ).ToList().ConvertAll( ( t ) => t.FullName ).ToArray() ) );
		// }
	}

	MemberInfo[] GetMembers( System.Type type )
	{
		List<MemberInfo> fields = new List<MemberInfo>();
		while( type != null )
		{
			fields.AddRange( type.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) );
			fields.AddRange( type.GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) );
			type = type.BaseType;
		}
		return fields.ToArray();
	}

	string[] GetFieldsSelectionNames( MemberInfo[] fieldInfos )
	{
		var stringedFields = fieldInfos.ToList().ConvertAll( ( f ) => f.Name );
		stringedFields.Insert( 0, "(none)" );
		return stringedFields.ToArray();
	}

	void UpdateData()
	{
		_sb.Clear();
		var objs = Selection.objects;
		var oLen = objs.Length;
		for( int i = 0; i < oLen; i++ )
		{
			var obj = objs[i];
			try
			{
				_sb.AppendLine( string.Format( _format, GetVars( obj ) ) );
			}
			catch( System.Exception ex )
			{
				_sb.AppendLine( ex.Message );
			}
		}
		_data = _sb.ToString();

		// if( _commonAncestor != null )
		// {
		// 	var properties = _commonAncestor.GetMembers( BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic );
		// 	var fields = _commonAncestor.GetFields( BindingFlags.Instance | BindingFlags.GetField | BindingFlags.Public | BindingFlags.NonPublic );
		// 	Debug.Log( $"Fields: {string.Join( ", ", fields.ToList().ConvertAll( ( f ) => f.Name ).ToArray() )}" );
		// 	Debug.Log( $"GetMembers: {string.Join( ", ", properties.ToList().ConvertAll( ( f ) => f.Name ).ToArray() )}" );
		// }
	}

    private string[] GetVars( object obj )
    {
		string[] strs = new string[ _vars.Count ];
		for( int vi = 0; vi < _vars.Count; vi++ )
		{
			var itObj = obj;
			var type = _commonAncestor;
			var path = _vars[vi];
			for( int i = 0; i < path.Count; i++ )
			{
				var members = GetMembers( type );
				var mi = members[path[i]];
				if( mi is FieldInfo fi ) itObj = fi.GetValue( itObj );
				if( mi is PropertyInfo pi ) itObj = pi.GetValue( itObj );
			}
			strs[vi] = itObj.ToStringOrNull();
		}
		return strs;
    }

    void OnEnable()
	{
		Selection.selectionChanged += OnSelectionChanged;
		OnSelectionChanged();
	}

	void OnDisable()
	{
		Selection.selectionChanged -= OnSelectionChanged;
	}

    private void OnSelectionChanged()
    {
		CountSelectedTypes( _typeCounter, _modelCounter );
		// _multipleTypes = IsMultipleTypeSelected( _typeCounter );
		selectionLabel = LabelLog( _typeCounter, _modelCounter );
		_commonAncestor = FindNearestCommonAncestor( _typeCounter.Keys );
		UpdateData();
    }

    public static Type FindNearestCommonAncestor( IEnumerable<Type> types )
    {
		if( types == null ) return null;

        List<Type> ancestors = null;
        List<Type> tempAncestors = new List<Type>();
		
		foreach( var t in types )
		{
			if( ancestors == null )
			{
				ancestors = new List<Type>();
				FillAncestors( t, ancestors );
				continue;
			}
			else
			{
				FillAncestors( t, tempAncestors );
				int it = 1;
				var tLen = tempAncestors.Count;
				var len = ancestors.Count;
				while( it <= tLen && it <= len && tempAncestors[ tLen - it ] == ancestors[ len - it ] ) { it++; }
				if( it <= len ) ancestors.RemoveRange( 0, ( len - it ) + 1 );
			}
		}

		if( ancestors != null && ancestors.Count > 0 ) return ancestors[0];

		return null;
    }

	private static void FillAncestors( Type type, List<Type> ancestors )
	{
        Type currentType = type;

		ancestors.Clear();
        while( currentType != null )
        {
            ancestors.Add( currentType );
            currentType = currentType.BaseType;
        }
	}

    private static Type[] GetAncestors( Type type )
    {
        var ancestors = new List<Type>();
        Type currentType = type;

        while( currentType != null )
        {
            ancestors.Add( currentType );
            currentType = currentType.BaseType;
        }

        return ancestors.ToArray();
    }

    private static void CountSelectedTypes( Dictionary<System.Type,int> types, Dictionary<DataModel,int> models )
    {
		var objs = Selection.objects;
		var oLen = objs.Length;
		types.Clear();
		models.Clear();
		for( int i = 0; i < oLen; i++ )
		{
			var obj = objs[i];
			var t = obj?.GetType();
			types[t] = 1 + ( types.TryGetValue( t, out var count ) ? count : 0 );
			
			if( obj is DataInstance di )
			{
				var model = di.Model;
				if( model != null ) models[model] = 1 + ( models.TryGetValue( model, out var mCount ) ? mCount : 0 );
			}
		}
    }

	private static bool IsMultipleTypeSelected( Dictionary<System.Type,int> types )
	{
		return types.Count > 1;
	}

	private static string LabelLog( Dictionary<System.Type,int> types, Dictionary<DataModel,int> models )
	{
		var countedTypes = types.Count;
		if( countedTypes > 1 )
		{
			var totalCount = 0;
			foreach( var kvp in types ) totalCount += kvp.Value;
			return $"Selected {totalCount} object(s) {string.Join( ", ", types.ToList().ConvertAll( ( kvp ) => TypeDebug( kvp, models ) ).ToArray() )}";
		}
		else if( countedTypes == 1 )
		{
			var kvp = types.FirstOrDefault();
			return $"Selected {TypeDebug(kvp, models)}";
		}
		return $"Selected no object";
	}

	static string TypeDebug( KeyValuePair<System.Type,int> kvp, Dictionary<DataModel,int> models )
	{
		if( kvp.Key == typeof(DataInstance) ) return $"{kvp.Value} {kvp.Key}( {ModelDebug( models )} )";
		return $"{kvp.Value} {kvp.Key}";
	}

	static string ModelDebug( Dictionary<DataModel,int> models )
	{
		var countedModels = models.Count;
		if( countedModels > 1 )
		{
			return $" {string.Join( ", ", models.ToList().ConvertAll( ( kvp ) => $"{kvp.Value} {kvp.Key}" ).ToArray() )}";
		}
		else if( countedModels == 1 )
		{
			var kvp = models.FirstOrDefault();
			return $" all models are {string.Join( ", ", models.ToList().ConvertAll( ( kvp ) => $"{kvp.Value} {kvp.Key}" ).ToArray() )}";
		}
		return string.Empty;
	}
}
