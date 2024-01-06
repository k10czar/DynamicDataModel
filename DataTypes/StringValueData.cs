using UnityEngine;

[System.Serializable]
public class StringValueData : IDataInstance
{
    [SerializeField] protected string _value;

    public virtual bool TrySet( object obj, DataInstance contextualData )
    {
        return obj.TrySetOn( ref _value );
    }
}
