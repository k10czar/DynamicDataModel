using UnityEngine;

[System.Serializable]
public class StringToggleValueData : StringValueData
{
    [SerializeField] bool _toggle;

    public override bool TrySet( object obj, DataInstance contextualData )
    {
        if( obj.TrySetOn( ref _value, ref _toggle ) ) return true;
        return base.TrySet( obj, contextualData );
    }
}
