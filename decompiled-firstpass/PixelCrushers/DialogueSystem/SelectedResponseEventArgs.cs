using System;

namespace PixelCrushers.DialogueSystem;

public class SelectedResponseEventArgs : EventArgs
{
	public Response response;

	public DialogueEntry DestinationEntry
	{
		get
		{
			if (response != null)
			{
				return response.destinationEntry;
			}
			return null;
		}
	}

	public SelectedResponseEventArgs(Response response)
	{
		this.response = response;
	}
}
