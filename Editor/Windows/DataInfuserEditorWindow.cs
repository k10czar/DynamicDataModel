using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

using static Colors.Console;

public class DataInfuserEditorWindow : EditorWindow
{
	DataModel _model;
	List<DataVariable> _modelVariableSelected = new List<DataVariable>();
	List<DataVariable> _modelVariables = new List<DataVariable>();
	List<DataInstance> _filteredDatas = new List<DataInstance>();
	Dictionary<string,int> _posDict = new Dictionary<string,int>();
	List<DataInstance> _missingData = new List<DataInstance>();
	string _missingDataLog = null;
	string[] _modelVariableNames;
	string _dataSplit = "\\t";
	string _outerDataSplit = "\\n";

	string _data = "UNSETED";
	List<string[]> _parsedData = new List<string[]>();
	List<int> _dataId = new List<int>();

    private Vector2 _scroll;

    [MenuItem( "K10/Data/Infuser" )] private static void Init() { GetWindow<DataInfuserEditorWindow>( "Infuser" ); }

	void OnEnable()
	{
		FinDataWithModel();
	}


	private void OnGUI()
	{
		GUILayout.BeginHorizontal();

		var newModel = EditorGUILayout.ObjectField( GUIContent.none, _model, typeof(DataModel), false, GUILayout.Width( 240 ) ) as DataModel;
		if( newModel != _model )
		{
			_model = newModel;
			_modelVariableSelected.Clear();
			FinDataWithModel();
			UpdateVariables();
		}
		if( _model != null )
		{
			for( int i = 0; i < _modelVariableSelected.Count; i++ )
			{
				if( GUILayout.Button( "-", GUILayout.Width( 16 ) ) ) 
				{
					_modelVariableSelected.RemoveAt( i );
					i--;
					continue;
				}
				var selectedVar = _modelVariableSelected[i];
				var id = selectedVar != null ? _modelVariables.IndexOf( selectedVar ) : -1;
				var newId = EditorGUILayout.Popup( id + 1, _modelVariableNames, GUILayout.Width( 240 ) );
				newId--;
				if( newId != id ) _modelVariableSelected[i] = ( newId >= 0 ) ? _modelVariables[newId] : null;
			}
			if( GUILayout.Button( "+", GUILayout.Width( 16 ) ) ) _modelVariableSelected.Add( null );
		}
		
		if( GUILayout.Button( "Pre Proccess", GUILayout.Width( 100 ) ) ) 
		{
			PreProccessData();
		}
		
		if( GUILayout.Button( "Respec Text", GUILayout.Width( 100 ) ) ) 
		{
			RespecText();
		}

		EditorGUILayout.LabelField( "Data Splitters:", GUILayout.Width( 80 ) );
		_outerDataSplit = EditorGUILayout.TextField( _outerDataSplit, GUILayout.Width( 80 ) );
		_dataSplit = EditorGUILayout.TextField( _dataSplit, GUILayout.Width( 80 ) );
		
		GuiColorManager.New( Colors.LightSalmon );
		if( GUILayout.Button( "Execute Data", GUILayout.Width( 100 ) ) ) 
		{
			PreProccessData();
			ExecuteData();
		}
		GuiColorManager.Revert();

		GUILayout.EndHorizontal();
        K10.EditorGUIExtention.SeparationLine.Horizontal();
		_scroll = EditorGUILayout.BeginScrollView( _scroll );

		var elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		var remainingHeight = position.height - 2 * elementHeight;
		var w = EditorGUIUtility.currentViewWidth;

		GUILayout.BeginHorizontal();
		var gw = GUILayout.Width(w/3);
		GUILayout.BeginVertical( GUILayout.Width(w/3) );
		var areaMinHeight = remainingHeight;
		// if( _missingData.Count > 0 )
		// {
		// 	areaMinHeight -= elementHeight * ( 1 + _missingData.Count );
		// }
		_data = EditorGUILayout.TextArea( _data, gw,GUILayout.MinHeight( areaMinHeight ), GUILayout.ExpandHeight( true ) );
		// if( _missingData.Count > 0 )
		// {
		// 	EditorGUILayout.LabelField( _missingDataLog, gw, GUILayout.ExpandHeight( true ) );
		// }
		GUILayout.EndVertical();
		GUILayout.BeginVertical();
		EditorGUI.BeginDisabledGroup( true );
		if( _parsedData != null || _parsedData.Count > 0 )
		{
			var varsToRead = _modelVariableSelected.Count;
            for (int i = 0; i < _parsedData.Count; i++)
			{
				var dataID = _dataId[i];
				var found = dataID >= 0 && dataID < _filteredDatas.Count;
                var line = _parsedData[i];
				var lessVars = line.Length < varsToRead + 1;
				var error = !found || lessVars;
                GUILayout.BeginHorizontal( GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( error ) 
				{
					GUILayout.Label( lessVars ? "LV" : "NF", GUILayout.Width(28) );
					GuiColorManager.New( lessVars ? Negation : Colors.Orange );
				}
				else 
				{
					GUILayout.Label( dataID.ToString(), GUILayout.Width(28) );
				}
                for (int j = 0; j < line.Length; j++)
                {
                    string dataSplited = line[j];
                    EditorGUILayout.TextField( GUIContent.none, dataSplited );
                }
                DataInstance data = found ? _filteredDatas[dataID] : null;
				EditorGUILayout.ObjectField( GUIContent.none, data, typeof(DataInstance), false );
				// EditorGUILayout.TextField( GUIContent.none, line );
				GUILayout.EndHorizontal();
				if( error ) GuiColorManager.Revert();
			}
		}
		else
		{
			EditorGUILayout.TextArea( "No parsed data", GUILayout.Width(w/2), GUILayout.MinHeight( remainingHeight ) );
		}
		EditorGUI.EndDisabledGroup();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();

		EditorGUILayout.EndScrollView();
	}

	private void FinDataWithModel()
	{
		_filteredDatas.Clear();
		_missingData.Clear();
		_parsedData.Clear();
		_dataId.Clear();
		_missingData.Clear();

		if( _model == null ) return;
		
		var allDatas = AssetDatabaseUtils.GetAll<DataInstance>();

		var SB = new StringBuilder();
		_posDict.Clear();
		SB.Append( Colorfy.OpenTag( Colors.ArcticLime ) );
		foreach( var data in allDatas )
		{
			if( data.Model != _model ) continue;
			var id = _filteredDatas.Count;
			var dataName = data.name.ToLower();
			_posDict[dataName] = id;
			_filteredDatas.Add( data );
			SB.Append( $"{dataName}, " );
			// SB.Append( $"{dataName}[{id}], " );
		}
		SB.Append( Colorfy.CloseTag() );

		Debug.Log( $"Filtered {_filteredDatas.Count.ToString().Colorfy(Numbers)} {_model.NameOrNull().Colorfy( TypeName )} DataInstances\n{SB.ToString()}" );
	}

    private void ExecuteData()
    {
		var varsToRead = _modelVariableSelected.Count;
		for (int i = 0; i < _parsedData.Count; i++)
		{
			var dataID = _dataId[i];
			var found = dataID >= 0 && dataID < _filteredDatas.Count;
			DataInstance data = found ? _filteredDatas[dataID] : null;
			if( data == null )
			{
				Debug.Log( $"Cannot execute command {i} that points to _filteredDatas[{dataID}]" );
				continue;
			}
			var settedSome = false;
			var line = _parsedData[i];
			Debug.Log( $"Executing command {i} {string.Join("|",line)} {data.NameOrNull()}" );
			for (int j = 1; j < line.Length && j <= varsToRead; j++)
			{
				string dataSplited = line[j];
				var dvar = _modelVariableSelected[j-1];
				var setted = false;
				if( j == varsToRead && j + 1 < line.Length ) setted = data.SetVariableData( dvar, ( dataSplited, line[j+1] ) );
				else setted = data.SetVariableData( dvar, dataSplited );
				Debug.Log( $"Executing command {i} {string.Join("|",line)} {data.NameOrNull()}.SetVariableData( {dataSplited} )" );
				settedSome |= setted;
				if( setted ) Debug.Log( $"{"SetVariableData".Colorfy( Verbs )} {dataSplited.Colorfy( Numbers )} on {data}.{dvar}, executing command {i.ToString().Colorfy( Numbers )}" );
				else Debug.Log( $"{"Not setted".Colorfy( Negation )} {dataSplited.Colorfy( Numbers )} on {data}.{dvar}, executing command {i.ToString().Colorfy( Numbers )}" );
			}
			if( settedSome ) EditorUtility.SetDirty( data );
		}

		Debug.Log( $"{"Infuser".Colorfy( Interfaces )} {"executed".Colorfy( Verbs )} {_parsedData.Count.ToString().Colorfy( Numbers )} command on {_model.ToStringOrNull().Colorfy( TypeName )}" );

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
    }

    private void RespecText()
    {
		_data = _data.Replace( $" {System.Environment.NewLine}", "\n" );
		_data = _data.Replace( " \n", "\n" );
    }

    void UpdateVariables()
	{
		if( _model == null ) return;

		_modelVariables.Clear();
		
		var varsCount = _model.VariablesCount;
		_modelVariableNames = new string[ varsCount + 1 ];

		_modelVariableNames[0] = "(none)";

		for( int i = 0; i < varsCount; i++ )
		{
			var dvar = _model.GetVariable( i );
			_modelVariables.Add( dvar );
			_modelVariableNames[i+1] = dvar.ToStringOrNull();
		}
	}

	static string ProccessSplitData( string data )
	{
		return data.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\r", "\r");
	}

	void PreProccessData()
	{
		if( _model == null ) return;

		var _preParsedData = _data.Split( ProccessSplitData( _outerDataSplit ), System.StringSplitOptions.RemoveEmptyEntries );
		_parsedData.Clear();
		_dataId.Clear();
		
		_missingData.Clear();
		_missingData.AddRange( _filteredDatas );
		
		var innerSplit = ProccessSplitData( _dataSplit );
        for (int i = 0; i < _preParsedData.Length; i++)
		{
            string line = _preParsedData[i];
            var datas = line.Split( innerSplit );
			if( datas.Length == 0 ) 
			{
				Debug.LogError( $"Ignored line: {line}" );
				continue;
			}
			var name = datas[0] = datas[0].ToLower();
			_parsedData.Add( datas );
			var id = -1;
			if( _posDict.TryGetValue( name, out var posID ) )
			{
				id = posID;
				try
				{
					var dataRef = _filteredDatas[posID];
					_missingData.Remove( dataRef );
				}
				catch( System.Exception ex )
				{
					Debug.LogError( ex.Message );
				}
			}
			if( datas.Length == 0 ) 
			{
				Debug.LogError( $"Cannot find data with name: {name}" );
				continue;
			}
			_dataId.Add( id );
		}

		var SB = new StringBuilder();
		if( _missingData.Count == 0 ) SB.AppendLine( $"All {_model.NameOrNull().Colorfy( TypeName )} are in the {"infuser".Colorfy( Interfaces )} {"prompt".Colorfy( Numbers )}" );
		else if( _missingData.Count == 1 ) SB.AppendLine( $"{_missingData.Count.ToString().Colorfy( Numbers )} DataInstance of {_model.NameOrNull().Colorfy( TypeName )} is not in the {"infuser".Colorfy( Interfaces )} {"prompt".Colorfy( Numbers )}:" );
		else SB.AppendLine( $"{_missingData.Count.ToString().Colorfy( Numbers )} DataInstances of {_model.NameOrNull().Colorfy( TypeName )} are not in the {"infuser".Colorfy( Interfaces )} {"prompt".Colorfy( Numbers )}:" );
		SB.Append( Colorfy.OpenTag( Colors.ArcticLime ) );
		for (int i = 0; i < _missingData.Count; i++) { SB.Append( _missingData[i].NameOrNull() ); if( i + 1 < _missingData.Count ) SB.Append( ", " ); }
		SB.Append( Colorfy.CloseTag() );
		// for (int i = 0; i < _missingData.Count; i++) { SB.AppendLine( $"  -{_missingData[i].NameOrNull()}" ); }
		_missingDataLog = SB.ToString();
		Debug.Log( _missingDataLog );
	}
}