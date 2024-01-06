using UnityEngine;

[System.Serializable]
public class ULongValueData : IDataInstance
{
    [SerializeField] protected ulong _value;

    public virtual bool TrySet( object obj, DataInstance contextualData )
    {
        return obj.TrySetOn( ref _value );
    }
}
