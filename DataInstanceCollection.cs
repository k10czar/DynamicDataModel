using UnityEngine;

public class DataInstanceCollection : ScriptableObject
{
    [SerializeField] DataModel _model;
    [SerializeField] DataInstance[] _data;
}