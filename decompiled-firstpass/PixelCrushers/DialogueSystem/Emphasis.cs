using UnityEngine;

namespace PixelCrushers.DialogueSystem;

public struct Emphasis
{
	public int startIndex { get; set; }

	public int length { get; set; }

	public Color color { get; set; }

	public bool bold { get; set; }

	public bool italic { get; set; }

	public bool underline { get; set; }

	public Emphasis(int startIndex, int length, Color color, bool bold, bool italic, bool underline)
	{
		this = default(Emphasis);
		this.startIndex = startIndex;
		this.length = length;
		this.color = color;
		this.bold = bold;
		this.italic = italic;
		this.underline = underline;
	}
}
