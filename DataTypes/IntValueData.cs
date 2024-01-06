using UnityEngine;

[System.Serializable]
public class IntValueData  : IDataInstance
{
    [SerializeField] int _value;

    public int Value => _value;

    public bool TrySet( object obj, DataInstance contextualData )
    {
        return obj.TrySetOn( ref _value );
    }
}