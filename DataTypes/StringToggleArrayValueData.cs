using UnityEngine;

[System.Serializable]
public class StringToggleArrayValueData : IDataInstance
{
    [SerializeField,TextArea] string[] _dataCollection;

    public bool TrySet( object obj, DataInstance contextualData )
    {
        return obj.TrySetOn( ref _dataCollection );
    }
}