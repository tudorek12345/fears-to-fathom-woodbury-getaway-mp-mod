using System;
using System.Collections.Generic;

namespace SoftMasking;

internal struct ClearListAtExit<T>(List<T> list) : IDisposable
{
	private List<T> _list = list;

	public void Dispose()
	{
		_list.Clear();
	}
}
