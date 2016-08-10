using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Mechanisms related to fast platform switching through the maintenance of a cache for the Unity Library
/// directory as built on different platforms.
///
/// Generally, users will have to run LibraryCache\initialize.ps1 to set up the Library junction prior to using this stuff,
/// because we can't remove the existing Library folder and replace it with a junction while the editor is running.
/// </summary>
static class AltspacePlatformSwitch
{
	/// <summary>
	/// The root of the Unity project.
	/// </summary>
	public static string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

	/// <summary>
	/// The path to the Library of the project. This path may be a directory (by Unity default). When using the cache,
	/// this path is a junction pointing to one of the platform-specific Library folders underneath CacheRoot.
	/// </summary>
	public static string LibraryPath = Path.Combine(ProjectRoot, "Library");

	/// <summary>
	/// The cache directory where platform-specific Library folders will live.
	/// </summary>
	public static string CacheRoot = Path.Combine(ProjectRoot, "LibraryCache");

	/// <summary>
	/// Whether the platform cache is set up or not.
	/// Cache doesn't work on MacOS currently.
	/// </summary>
	public static bool IsCacheEnabled { get { return (Application.platform != RuntimePlatform.OSXEditor && JunctionPoint.Exists(LibraryPath)); } }

	/// <summary>
	/// Switches platforms to the target by pointing the Library junction to the appropriate platform-specific Library cache.
	/// </summary>
	public static void SwitchToPlatform(BuildTarget target)
	{
		var targetName = Enum.GetName(typeof(BuildTarget), target);
		var targetPath = Path.Combine(CacheRoot, targetName);

		// note that we do something sane when current==target

		if (Directory.Exists(targetPath)) // do we have a cached version of the future library folder?
		{
			Debug.LogFormat("Using cached library folder at {0}.", targetPath);
			JunctionPoint.Create(LibraryPath, targetPath, true);
			EditorUserBuildSettings.SwitchActiveBuildTarget(target);
		}
		else
		{
			// strategy:
			// 1. copy current library dir to new platform library cache
			// 2. switch junction to point to it
			// 3. switch editor to rebuild new platform assets there
			Debug.LogFormat("Generating cached {0} library at {1}.", targetName, targetPath);
			UnityEditor.FileUtil.CopyFileOrDirectory(LibraryPath, targetPath);
			JunctionPoint.Create(LibraryPath, targetPath, true);
			EditorUserBuildSettings.SwitchActiveBuildTarget(target);
		}
	}

	/// <summary>
	/// Whether there is a non-junction directory at path.
	/// </summary>
	private static bool IsDirectory(string path)
	{
		return Directory.Exists(path) && !JunctionPoint.Exists(path);
	}

	private static bool CheckCacheEnabled()
	{
		if (!IsCacheEnabled)
			Debug.LogErrorFormat(
				"Platform cache not initialized; please close Unity and run LibraryCache\\initialize.ps1 -Platform {0}.",
				Enum.GetName(typeof(BuildTarget), EditorUserBuildSettings.activeBuildTarget));

		return IsCacheEnabled;
	}

	// a convenient menu option for artists, who may not have their library cache set up
	[MenuItem("AltspaceVR/Platform/Set Up Platform Switching Folder")]
	private static void CreateLibraryCacheFolder()
	{
		if (!Directory.Exists(CacheRoot))
		{
			Directory.CreateDirectory(CacheRoot);
			Debug.Log("Created library cache folder.");
		}
		else
		{
			Debug.LogWarning("Library cache folder already exists so I didn't create it.");
		}
		var powerShellCopyLocation = Path.Combine(CacheRoot, "initialize.ps1");
		var powerShellSourceLocation = Path.Combine(ProjectRoot, "Assets/Altspace/Shared Assets/Editor/initialize.ps1");
		if (!File.Exists(powerShellCopyLocation))
		{
			File.Copy(powerShellSourceLocation, powerShellCopyLocation);
			Debug.Log("Copied PowerShell into cache folder.");
		}
		else
		{
			Debug.LogWarning("Powershell script for fast platform switching already exists in its proper location.");
		}

		// copy batch files too
		var batFileWindows = Path.Combine(CacheRoot, "FastPlatformSetUpWindows.bat");
		if (!File.Exists(batFileWindows))
		{
			File.Copy(Path.Combine(ProjectRoot, "Assets/Altspace/Shared Assets/Editor/FastPlatformSetUpWindows.bat"),
				batFileWindows);
			Debug.Log("Copied fast platform switching batch file into the cache folder");
		}
		else
		{
			Debug.LogWarning("Fast plaform switching batch file already exists in its proper location.");
		}

		EditorUtility.DisplayDialog("Done!", "Please consult the PlatformSwitchReadMe document.", "Ok");
	}

	[MenuItem("AltspaceVR/Platform/Switch to Android")]
	private static void SwitchToAndroid()
	{
		if (CheckCacheEnabled())
			SwitchToPlatform(BuildTarget.Android);
	}

	[MenuItem("AltspaceVR/Platform/Switch to Windows")]
	private static void SwitchToWindows()
	{
		if (CheckCacheEnabled())
			SwitchToPlatform(BuildTarget.StandaloneWindows);
	}
}
