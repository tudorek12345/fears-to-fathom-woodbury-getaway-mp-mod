namespace PixelCrushers;

public static class SceneNotifier
{
	public delegate void UnloadSceneDelegate(int sceneIndex);

	public static event UnloadSceneDelegate willUnloadScene;

	public static void NotifyWillUnloadScene(int sceneIndex)
	{
		SceneNotifier.willUnloadScene(sceneIndex);
	}

	static SceneNotifier()
	{
		SceneNotifier.willUnloadScene = delegate
		{
		};
	}
}
