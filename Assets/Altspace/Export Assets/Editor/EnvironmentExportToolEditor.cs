using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


[CustomEditor(typeof(EnvironmentExportTool))]
public class EnvironmentExportToolEditor : Editor
{
    private string tempSceneFile = "";
	private string outAssetBundleName = "";
	
	private const string macAssetBundleFolder = "OSX",
						pcAssetBundleFolder = "Windows",
						androidAssetBundleFolder = "Android";

	private string AssetBundleFileExtension
	{
		get {
			var fileExtension = ".unity3d";
			
			if (Application.unityVersion.StartsWith("5"))
			{
				fileExtension = ".unity5";
			}
			return fileExtension;
		}
	}

	private EnvironmentExportTool environmentExportTool;

	private string TestingSpaceSid = "";

	private bool showAmazon = true;
	private bool showTesting = true;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		environmentExportTool = (EnvironmentExportTool)target;

		//GUILayout.BeginVertical();


		showTesting = EditorGUILayout.Foldout(showTesting, "Test");
		if (showTesting)
		{

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Test Space Sid");
			TestingSpaceSid = EditorGUILayout.TextField(TestingSpaceSid, GUILayout.MaxWidth(200));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Backup Settings", GUILayout.MaxWidth(200)))
			{
				BackupStartupSettings();
			}
			if (GUILayout.Button("Quick Build", GUILayout.MaxWidth(200)))
			{

				if (!InitializeFilePaths(environmentExportTool.assetBundleName))
				{
					Debug.LogError("Unable to save out scene, could not upload to S3.");
				}

				EditorApplication.SaveScene(EditorApplication.currentScene);

				if (!SaveOutScene())
				{
					Debug.LogError("Ran into error trying to save out scene.");
				}

				if (!SaveOutAssetBundle(GetCurrentPlatform(), false))
				{
					Debug.LogError("Unable to save out asset bundle");
				}
			}

			if (GUILayout.Button("Quick Build & Test", GUILayout.MaxWidth(200)))
			{

				if (!InitializeFilePaths(environmentExportTool.assetBundleName))
				{
					Debug.LogError("Unable to save out scene, could not upload to S3.");
				}

				EditorApplication.SaveScene(EditorApplication.currentScene);

				if (!SaveOutScene())
				{
					Debug.LogError("Ran into error trying to save out scene.");
				}
				
				if (!SaveOutAssetBundle(GetCurrentPlatform(), false))
				{
					Debug.LogError("Unable to save out asset bundle");
				}

				SaveStartupSettings();

				TestEnvironmentInClient();
			}
			EditorGUILayout.EndHorizontal();
		}

		#region Amazon
		showAmazon = EditorGUILayout.Foldout(showAmazon, "Upload");
		if (showAmazon)
		{
			{
				

				//display properties as uneditable labels
				GUILayout.BeginHorizontal();
				GUILayout.Label("bucketName");
				GUILayout.Label(environmentExportTool.bucketName);
				GUILayout.Space(10f);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("cognitoRegion");
				GUILayout.Label(environmentExportTool.cognitoRegion.ToString());
				GUILayout.Space(10f);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("s3Region");
				GUILayout.Label(environmentExportTool.s3Region.ToString());
				GUILayout.Space(10f);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("cognitoIdentityPool");
				GUILayout.Label(environmentExportTool.cognitoIdentityPool.ToString());
				GUILayout.Space(10f);
				GUILayout.EndHorizontal();
			}

			
			EditorGUILayout.BeginHorizontal();
			environmentExportTool.buildPCAssetBundle = GUILayout.Toggle(environmentExportTool.buildPCAssetBundle, "Windows");
			environmentExportTool.buildMacAssetBundle = GUILayout.Toggle(environmentExportTool.buildMacAssetBundle, "OSX");
			environmentExportTool.buildAndroidAssetBundle = GUILayout.Toggle(environmentExportTool.buildAndroidAssetBundle, "Android");
			EditorGUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Build Bundles", GUILayout.MaxWidth(200)))
			{
				if (!InitializeFilePaths(environmentExportTool.assetBundleName))
				{
					Debug.LogError("Unable to save out scene, could not upload to S3.");
				}

				EditorApplication.SaveScene(EditorApplication.currentScene);

				//save out tmp scene
				if (!SaveOutScene())
				{
					Debug.LogError("Ran into error trying to save out scene.");
				}
				if (environmentExportTool.buildPCAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.PC))
				{
					Debug.LogError("Unable to save out asset bundle for PC (Windows).");
				}
				if (environmentExportTool.buildMacAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.MAC))
				{
					Debug.LogError("Unable to save out asset bundle for Mac.");
				}
				if (environmentExportTool.buildAndroidAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.ANDROID))
				{
					Debug.LogError("Unable to save out asset bundle for Android.");
				}
			}

			if (GUILayout.Button("Upload to Amazon", GUILayout.MaxWidth(200)))
			{
				//check that we are not in the middle of another export
				if (environmentExportTool.IsCurrentlyUploading)
				{
					Debug.LogError("Cannot start another upload until previous one finishes!");
					return;
				}

				if (!EditorApplication.isPlaying)
				{
					Debug.LogError("You must Play the UnityEditor first in order to upload to Amazon S3!");
					return;
				}

				if (!InitializeFilePaths(environmentExportTool.assetBundleName))
				{
					Debug.LogError("Unable to save out scene, could not upload to S3.");
					return;
				}

				Stack<EnvironmentExportTool.PlatformDetail> platformsForUploading = new Stack<EnvironmentExportTool.PlatformDetail>();
				if (environmentExportTool.buildPCAssetBundle)
				{
					platformsForUploading.Push(new EnvironmentExportTool.PlatformDetail(EnvironmentExportTool.Platform.PC,
											   AssetBundleDirForPlatform(EnvironmentExportTool.Platform.PC)
											   + environmentExportTool.assetBundleName + AssetBundleFileExtension));
				}
				if (environmentExportTool.buildMacAssetBundle)
				{
					platformsForUploading.Push(new EnvironmentExportTool.PlatformDetail(EnvironmentExportTool.Platform.MAC,
											   AssetBundleDirForPlatform(EnvironmentExportTool.Platform.MAC)
											   + environmentExportTool.assetBundleName + AssetBundleFileExtension));
				}
				if (environmentExportTool.buildAndroidAssetBundle)
				{
					platformsForUploading.Push(new EnvironmentExportTool.PlatformDetail(EnvironmentExportTool.Platform.ANDROID,
											   AssetBundleDirForPlatform(EnvironmentExportTool.Platform.ANDROID)
											   + environmentExportTool.assetBundleName + AssetBundleFileExtension));
				}
				environmentExportTool.UploadAndPublishAssetBundle(platformsForUploading);
			}
			GUILayout.EndHorizontal();
		}
#endregion


		//GUILayout.EndVertical();
    }

	public static EnvironmentExportTool.Platform GetCurrentPlatform()
	{
		if (Application.platform == RuntimePlatform.WindowsEditor)
			return EnvironmentExportTool.Platform.PC;
		else
			return EnvironmentExportTool.Platform.MAC;
	}
	

	public bool InitializeFilePaths(string assetBundleName)
    {
        if (assetBundleName == "")
        {
            Debug.LogError("Must provide a name or path for unity5 file to be save and uploaded!");
            return false;
        }
		outAssetBundleName = assetBundleName;
		string exportDirectory = GetExportDirectory();
        if (!Directory.Exists(exportDirectory))
        {
            Directory.CreateDirectory(exportDirectory);
        }

        tempSceneFile = exportDirectory + Path.DirectorySeparatorChar + assetBundleName + ".unity";

        return true;
    }

	private string GetExportDirectory()
	{
		string[] exportDirParts = { "Assets", "Altspace", "Export" };
		return string.Join(Path.DirectorySeparatorChar.ToString(), exportDirParts);
	}

	private string AssetBundleDirForPlatform(EnvironmentExportTool.Platform platform)
	{
		string exportDir = pcAssetBundleFolder;
		if (platform == EnvironmentExportTool.Platform.MAC)
		{
			exportDir = macAssetBundleFolder;
		}
		else if (platform == EnvironmentExportTool.Platform.ANDROID)
		{
			exportDir = androidAssetBundleFolder;
		}
		return GetExportDirectory() + Path.DirectorySeparatorChar + exportDir + Path.DirectorySeparatorChar;
	}
	
    public bool SaveOutScene()
    {
	    var originalActiveScene = EditorSceneManager.GetActiveScene();
        string originalActiveScenePath = originalActiveScene.path;
	    EditorSceneManager.SaveScene(originalActiveScene);
		bool success = false;
	    success = EditorSceneManager.SaveScene(originalActiveScene, tempSceneFile);

		if (success)
		{		
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(); // necessary?
			EditorSceneManager.CloseScene(originalActiveScene, true);
			var tempScene = EditorSceneManager.OpenScene(tempSceneFile);
			EditorSceneManager.SetActiveScene(tempScene);
			DestroyImmediate(GameObject.Find("Tools"));
			EditorSceneManager.SaveScene(tempScene);
			EditorSceneManager.CloseScene(tempScene, true);
			originalActiveScene = EditorSceneManager.OpenScene(originalActiveScenePath);
			EditorSceneManager.SetActiveScene(originalActiveScene);
		}
		return success;
    }

	public bool SaveOutAssetBundle(EnvironmentExportTool.Platform platform, bool shouldCompress = true)
    {
        if (tempSceneFile.Length != 0)
        {
            string[] scenes = { tempSceneFile };
			AssetBundleBuild [] buildMap = { new AssetBundleBuild() };
			
			buildMap[0].assetNames = scenes;
			buildMap[0].assetBundleName = outAssetBundleName;
			buildMap[0].assetBundleVariant = "unity5";
			BuildTarget target = BuildTarget.StandaloneWindows;
			if (platform == EnvironmentExportTool.Platform.MAC)
			{
				target = BuildTarget.StandaloneOSXUniversal;
			}
			else if (platform == EnvironmentExportTool.Platform.ANDROID)
			{
				target = BuildTarget.Android;
			}
			string outputDir = AssetBundleDirForPlatform(platform);
			if (!Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			BuildAssetBundleOptions options = shouldCompress ? BuildAssetBundleOptions.None : BuildAssetBundleOptions.UncompressedAssetBundle;

			BuildPipeline.BuildAssetBundles(outputDir, buildMap, options, target);
            Debug.Log("Asset Bundle exported to: " + outputDir + ".");
            return true;
        }

        return false;
	}

	public void TestEnvironmentInClient()
	{
		if (string.IsNullOrEmpty(TestingSpaceSid))
		{
			EditorUtility.DisplayDialog("Space Sid Required", "You must enter a valid Space Sid.", "Ok");
			return;
		}
		SaveStartupSettings();
		string url = "altspace:/";
		Application.OpenURL(url);
	}

	public void SaveStartupSettings()
	{
		var settings = new StartupSettingsJson();
		settings.asset_bundle_scene = environmentExportTool.assetBundleName;
		settings.initial_space_sid = TestingSpaceSid;
		settings.asset_bundle_path = "file:" + Path.AltDirectorySeparatorChar + Path.AltDirectorySeparatorChar + Path.GetFullPath(AssetBundleDirForPlatform(GetCurrentPlatform()) + environmentExportTool.assetBundleName.ToLower() + AssetBundleFileExtension).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		var json = JsonUtility.ToJson(settings);
		File.WriteAllText(Paths.StartupSettings, json);
	}

	public void BackupStartupSettings()
	{
		if (File.Exists(Paths.StartupSettings))
		{
			FileUtil.ReplaceFile(Paths.StartupSettings, Paths.StartupSettingsBackup);
		}
	}

	public class StartupSettingsJson
	{
		public string initial_space_sid;
		public string asset_bundle_path;
		public string asset_bundle_scene;
	}

}
