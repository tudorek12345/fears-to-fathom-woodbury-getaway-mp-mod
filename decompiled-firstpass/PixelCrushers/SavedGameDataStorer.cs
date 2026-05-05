using System.Collections;
using UnityEngine;

namespace PixelCrushers;

public abstract class SavedGameDataStorer : MonoBehaviour
{
	public virtual float progress { get; protected set; }

	public abstract bool HasDataInSlot(int slotNumber);

	public abstract void StoreSavedGameData(int slotNumber, SavedGameData savedGameData);

	public abstract SavedGameData RetrieveSavedGameData(int slotNumber);

	public abstract void DeleteSavedGameData(int slotNumber);

	public virtual IEnumerator StoreSavedGameDataAsync(int slotNumber, SavedGameData savedGameData)
	{
		StoreSavedGameData(slotNumber, savedGameData);
		progress = 1f;
		yield break;
	}
}
