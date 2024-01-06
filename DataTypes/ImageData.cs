using UnityEngine;

[System.Serializable]
public class ImageData : IDataInstance
{
	[SerializeField] Texture2D _texture;

    public Texture2D Texture => _texture;

    public bool TrySet( object obj, DataInstance contextualData )
    {
        if( obj is Texture2D texture )
        {
            _texture = texture;
            return true;
        }
        return obj.TrySetOn( ref _texture );
    }
}
