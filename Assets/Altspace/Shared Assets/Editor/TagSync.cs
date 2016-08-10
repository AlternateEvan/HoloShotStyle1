using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps tags and layers synced between projects
/// </summary>
static class TagSync
{

	private static string CurrentTagManagerPath = Path.Combine(Paths.ProjectSettings, "TagManager.asset");
	private static string SavedTagManagerPath = Path.Combine(Paths.SharedSettings, "TagManager.asset.txt");

	/// <summary>
	/// Saves the TagManager.asset file to the Shared Settings directory.
	/// </summary>
	[MenuItem("AltspaceVR/Tags and Layers/Save")]
	public static void SaveTagsAndLayers()
	{
		Debug.Log("Saving " + CurrentTagManagerPath + " to " + Paths.SharedSettings);
		UnityEditor.FileUtil.ReplaceFile(CurrentTagManagerPath, SavedTagManagerPath);
	}
	/// <summary>
	/// Loads the TagManager.asset file from the Shared Settings directory.
	/// </summary>
	[MenuItem("AltspaceVR/Tags and Layers/Load")]
	public static void LoadTagsAndLayers()
	{
		Debug.Log("Loading " + CurrentTagManagerPath + " from " + Paths.SharedSettings);
		UnityEditor.FileUtil.ReplaceFile(SavedTagManagerPath, CurrentTagManagerPath);
		var shouldRestart = EditorUtility.DisplayDialog("Restart Required",
			"Tags and layers have been loaded and will be imported the next time you restart the editor.", "Exit (Without Saving) ", "Restart Later");

		if(shouldRestart) EditorApplication.Exit(0);
	}
}
