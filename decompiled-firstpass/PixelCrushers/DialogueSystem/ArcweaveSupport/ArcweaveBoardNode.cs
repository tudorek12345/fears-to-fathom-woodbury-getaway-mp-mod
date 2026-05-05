using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.ArcweaveSupport;

public class ArcweaveBoardNode
{
	public string guid;

	public Board board;

	public List<ArcweaveBoardNode> children = new List<ArcweaveBoardNode>();

	public ArcweaveBoardNode parent;

	public ArcweaveBoardNode(string guid, Board board, ArcweaveBoardNode parent)
	{
		this.guid = guid;
		this.board = board;
		this.parent = parent;
	}
}
