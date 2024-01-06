using UnityEngine;
using UnityEditor;
using K10.EditorGUIExtention;
using System.Collections.Generic;
using UnityEditorInternal;

// [CustomPropertyDrawer(typeof(YearlyValueData))]
public class YearlyValueDataCollectionDataPropertyDrawer : PropertyDrawer
{
    private KeyboardFocusWatch _focusWatchValue = new KeyboardFocusWatch();
    private KeyboardFocusWatch _focusWatchYear = new KeyboardFocusWatch();

    public override float GetPropertyHeight( SerializedProperty property, GUIContent label ) => EditorGUIUtility.singleLineHeight;

    public override void OnGUI( Rect area, SerializedProperty property, GUIContent label )
    {
        var valueProp = property.FindPropertyRelative( "value" );
        var yearProp = property.FindPropertyRelative( "year" );

        if( _focusWatchValue.CheckForLooseFocusEvent() ) OnEditEnd( property );
        GuiLabelWidthManager.New( 35 );
        EditorGUI.PropertyField( area.VerticalSlice( 0, 3, 2 ), valueProp, true );
        GuiLabelWidthManager.New( 12 );
        if( _focusWatchYear.CheckForLooseFocusEvent() ) OnEditEnd( property );
        EditorGUI.PropertyField( area.VerticalSlice( 2, 3 ), yearProp, new GUIContent( "@" ), true );
        GuiLabelWidthManager.Revert(2);
    }

    void OnEditEnd( SerializedProperty property )
    {
        Debug.Log($"{"Loose".Colorfy(Colors.Console.Negation)} focus on {property.propertyPath.Colorfy(Colors.Console.Names)}" );

        var parentPath = property.GetParentArrayPropPath();
        var path = parentPath.Substring( 0, parentPath.Length - "._dataCollection".Length );
        var prop = property.serializedObject.FindProperty( path );

        var obj = prop.GetInstance();
        if( obj is ISortable srt )
        {
            srt.Sort();
            EditorUtility.SetDirty( property.serializedObject.targetObject );
        }

        // for( int i = 0; i < arrayProp.arraySize; i++ )
        // {
        //     var ei = arrayProp.GetArrayElementAtIndex( i );
        //     var iyProp = ei.FindPropertyRelative( "year" );
        //     var ivProp = ei.FindPropertyRelative( "value" );
        //     for( int j = i+1; j < arrayProp.arraySize; j++ )
        //     {
        //         var ej = arrayProp.GetArrayElementAtIndex( j );
        //         var jyProp = ej.FindPropertyRelative( "year" );
        //         var jvProp = ei.FindPropertyRelative( "value" );
        //         var iy = iyProp.intValue;
        //         var iv = ivProp.longValue;
        //         var jy = jyProp.intValue;
        //         var jv = jvProp.longValue;
        //         if( jy > iy || ( jy == iy && jv > iv ) )
        //         {
        //             var auxV = jv;
        //             var auxY = jy;
        //             jyProp.intValue = iyProp.intValue;
        //             jvProp.longValue = ivProp.longValue;
        //             iyProp.intValue = auxY;
        //             ivProp.longValue = auxV;
        //         }
        //     }
        // }
    }
}
