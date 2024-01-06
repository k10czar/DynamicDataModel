using System.Collections.Generic;
using UnityEngine;

public class TextureColorExtractedData : IDataInstance, IDataDependent
{
    [SerializeField] float _colorPercentageCut = .015f;
	[SerializeField] List<Weighted<Color>> _colors;
    
    public System.Type GetDependentType() => typeof(ImageData);

    public bool Feed( object obj )
    {
        if( _colors != null && _colors.Count > 0 ) return false;
        if( obj is ImageData imageData )
        {
            ProccessFlagData( imageData?.Texture ?? null );
            return true;
        }
        return false;
    }

    public bool TrySet( object obj, DataInstance contextualData )
    {
        if( _colors != null || _colors.Count > 0 ) return false;
        Debug.Log( $"TextureColorExtractedData.TrySet( {obj.ToStringOrNull()} )" );
        if( obj is ImageData imageData && Feed( imageData ) ) return true;
        if( obj is Texture2D texture )
        {
            ProccessFlagData( texture );
            return true;
        }
        if( obj.TrySetOn( ref _colors, ref _colorPercentageCut ) ) return true;
        if( obj.TrySetOn( ref _colorPercentageCut ) ) return true;
        if( obj.TrySetOn( ref _colors ) ) return true;
        return false;
    }

    void ProccessFlagData( Texture2D texture )
    {
        if( texture == null )
        {
            if( _colors != null ) _colors.Clear();
            return;
        }

        if( UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsCompressedFormat( texture.format ) )
        {
            return;
        }

        var colorCounter = new Dictionary<uint,int>();

        var w = texture.width;
        var h = texture.height;
        var pixels = w * h;

        byte[] rawTextureData = texture.GetRawTextureData();
        var rawLen = rawTextureData.Length;

        var format = texture.format;

        var bytesPerPixel = BytesPerPixel( format );
        var maxI = rawTextureData.Length / bytesPerPixel;
        var minPixels = _colorPercentageCut * pixels;

        for( int i = 0; i < pixels; i++ )
        {
            var code = Encode32( rawTextureData, i, bytesPerPixel );
            if( !colorCounter.TryGetValue( code, out var count ) ) count = 0;
            count++;
            colorCounter[code] = count;
        }
        
        var opaquePixels = 0;
        var readedPixels = 0;
        var validPixels = 0;

        List<Weighted<Color>> validColors = new List<Weighted<Color>>();
        List<Weighted<Color>> invalidColors = new List<Weighted<Color>>();
        List<Weighted<Color>> transparentColors = new List<Weighted<Color>>();

        foreach( var kvp in colorCounter )
        {
            var count = kvp.Value;
            var code = kvp.Key;
            readedPixels += count;
            var color = Decode32( code, format );
            var opaque = color.a > .9f;
            if( !opaque ) continue;
            opaquePixels += count;
            if( count <= minPixels ) continue;
            validPixels += count;
        }
        
        var transparentPixels = readedPixels - opaquePixels;

        foreach( var kvp in colorCounter )
        {
            var count = kvp.Value;
            var code = kvp.Key;
            var validCount = count > minPixels;
            var color = Decode32( code, format );
            var opaque = color.a > .9f;
            if( !opaque ) 
            {
                transparentColors.Add( new Weighted<Color>( color, ((float)count)/transparentPixels ) );
                continue;
            }
            var data = new Weighted<Color>( color, ((float)count)/opaquePixels );
            if( !validCount ) 
            {
                invalidColors.Add( data );
                continue;
            }
            validColors.Add( data );
        }

        validColors.Sort( Weighted.Descending );
        invalidColors.Sort( Weighted.Descending );
        transparentColors.Sort( Weighted.Descending );

        if( _colors == null ) _colors = new List<Weighted<Color>>();

        var SB = new System.Text.StringBuilder();
        SB.AppendLine( $"TextureColorExtractedData pixels:{pixels} RawData.Length:{rawTextureData.Length} format:{format}({bytesPerPixel}) minPixels:{minPixels} maxI:{maxI} colors:{validColors.Count} discarded colors:{invalidColors.Count}" );
        foreach( var color in validColors )
        {
            SB.AppendLine( $"{ConsoleProgressBar.Create( color.Weight, 10, true ).Colorfy( color.Value )} - {Mathf.RoundToInt(color.Weight * readedPixels)}px" );
            _colors.Add( color );
        }
        SB.AppendLine( $"------------ {invalidColors.Count} discarded colors ------------" );
        foreach( var color in invalidColors )
        {
            SB.AppendLine( $"{ConsoleProgressBar.Create( color.Weight, 10, true ).Colorfy( color.Value )} - {Mathf.RoundToInt(color.Weight * readedPixels)}px" );
        }
        SB.AppendLine( $"------------ {transparentColors.Count} transparent colors ------------" );
        foreach( var color in transparentColors )
        {
            SB.AppendLine( $"{ConsoleProgressBar.Create( color.Weight, 10, true ).Colorfy( color.Value )} - {Mathf.RoundToInt(color.Weight * readedPixels)}px" );
        }
        
        Debug.Log( SB.ToString() );
    }

    int BytesPerPixel( TextureFormat format )
    {
        switch( format )
        {
            case TextureFormat.Alpha8:
            case TextureFormat.R8:
                return 1;

            case TextureFormat.ARGB4444:
            case TextureFormat.RGB565:
            case TextureFormat.R16:
            case TextureFormat.RG16:
            case TextureFormat.RG32:
            case TextureFormat.RGBA4444:
                return 2;

            case TextureFormat.RGB24:
                return 3;

            case TextureFormat.RGBA32:
            case TextureFormat.ARGB32:
            case TextureFormat.BGRA32:
                return 4;

            default: throw new System.NotImplementedException( $"Not Implemented {format} encode" );
        }
    }

    uint Encode32( byte[] rawData, int pixelIndex, int bytesPerPixel )
    {
        var realId = pixelIndex * bytesPerPixel;
        
        uint data = 0;
        for( int i = 0; i < bytesPerPixel; i++ )
        {
            data |= (uint)rawData[realId+i] << ( i * 8 );
        }
        return data;
    }

    uint Encode32( byte[] rawData, int pixelIndex, TextureFormat format )
    {
        var bytesPerPixel = BytesPerPixel( format );
        return Encode32( rawData, pixelIndex, bytesPerPixel );
    }

    const float Max4 = (float)( ( 1 << 4 ) - 1 );
    const float Max5 = (float)( ( 1 << 5 ) - 1 );
    const float Max6 = (float)( ( 1 << 6 ) - 1 );
    const float Max8 = (float)( ( 1 << 8 ) - 1 );
    const float Max16 = (float)( ( 1 << 16 ) - 1 );
    const float Max32 = (float)( ( 1 << 32 ) - 1 );

    Color Decode32( uint value, TextureFormat format )
    {
        var a = 1f;
        var r = 0f;
        var g = 0f;
        var b = 0f;

        switch( format )
        {
            case TextureFormat.Alpha8:
                a = value / Max8;
                break;

            case TextureFormat.RGB24:
                r = GetBits( value, 0, 8 ) / Max8;
                g = GetBits( value, 8, 8 ) / Max8;
                b = GetBits( value, 16, 8 ) / Max8;
                break;

            case TextureFormat.R8:
                r = value / Max8;
                break;

            case TextureFormat.ARGB4444:
                a = GetBits( value, 0, 4 ) / Max4;
                r = GetBits( value, 4, 4 ) / Max4;
                g = GetBits( value, 8, 4 ) / Max4;
                b = GetBits( value, 16, 4 ) / Max4;
                break;

            case TextureFormat.RGB565:
                r = GetBits( value, 0, 5 ) / Max5;
                g = GetBits( value, 5, 6 ) / Max6;
                b = GetBits( value, 11, 5 ) / Max5;
                break;

            case TextureFormat.RG16:
                r = GetBits( value, 0, 8 ) / Max8;
                g = GetBits( value, 8, 8 ) / Max8;
                break;

            case TextureFormat.RG32:
                r = GetBits( value, 0, 16 ) / Max16;
                g = GetBits( value, 16, 16 ) / Max16;
                break;

            case TextureFormat.RGBA4444:
                r = GetBits( value, 0, 4 ) / Max4;
                g = GetBits( value, 4, 4 ) / Max4;
                b = GetBits( value, 8, 4 ) / Max4;
                a = GetBits( value, 12, 4 ) / Max4;
                break;

            case TextureFormat.RGBA32:
                r = GetBits( value, 0, 8 ) / Max8;
                g = GetBits( value, 8, 8 ) / Max8;
                b = GetBits( value, 16, 8 ) / Max8;
                a = GetBits( value, 24, 8 ) / Max8;
                break;

            case TextureFormat.ARGB32:
                r = GetBits( value, 8, 8 ) / Max8;
                g = GetBits( value, 16, 8 ) / Max8;
                b = GetBits( value, 24, 8 ) / Max8;
                a = GetBits( value, 0, 8 ) / Max8;
                break;

            case TextureFormat.BGRA32:
                b = GetBits( value, 0, 8 ) / Max8;
                g = GetBits( value, 8, 8 ) / Max8;
                r = GetBits( value, 16, 8 ) / Max8;
                a = GetBits( value, 24, 8 ) / Max8;
                break;

            default: throw new System.NotImplementedException( $"Not Implemented {format} decode" );
        }
        
        return new Color( r, g, b, a );
    }

    uint GetBits( uint raw, int startBit, int bits )
    {
        return (uint)( ( raw >> startBit ) % ( 1 << bits ) );
    }
}