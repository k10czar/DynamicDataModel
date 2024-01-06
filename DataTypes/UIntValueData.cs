using UnityEngine;

[System.Serializable]
public class UIntValueData  : IDataInstance
{
    [SerializeField] uint _value;

    public uint Value => _value;

    public bool TrySet( object obj, DataInstance contextualData )
    {
        return obj.TrySetOn( ref _value );
    }
}