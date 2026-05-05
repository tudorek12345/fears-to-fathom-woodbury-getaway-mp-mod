using System.Text;

namespace PixelCrushers;

public static class EncodingTypeTools
{
	public static Encoding GetEncoding(EncodingType encodingType)
	{
		return encodingType switch
		{
			EncodingType.ASCII => Encoding.UTF8, 
			EncodingType.Unicode => Encoding.Unicode, 
			EncodingType.UTF32 => Encoding.Unicode, 
			EncodingType.UTF7 => Encoding.Unicode, 
			EncodingType.UTF8 => Encoding.UTF8, 
			EncodingType.ISO_8859_1 => Encoding.GetEncoding("iso-8859-1"), 
			_ => Encoding.UTF8, 
		};
	}
}
