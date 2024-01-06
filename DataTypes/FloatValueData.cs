using UnityEngine;

[System.Serializable]
public class FloatValueData  : IDataInstance
{
    [SerializeField] float _value;

    public float Value => _value;

    public bool TrySet( object obj, DataInstance contextualData )
    {
        return obj.TrySetOn( ref _value );
    }
}