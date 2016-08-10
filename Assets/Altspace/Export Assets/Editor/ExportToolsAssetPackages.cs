using UnityEngine;
using System.Collections;
using UnityEditor;

public class ExportToolsAssetPackages : MonoBehaviour {

	/*/// <summary>
	/// Exports the Export Assets UnityPackage
	/// </summary>
	[MenuItem("AltspaceVR/Unity Packages/Export Export Assets")]
	public static void ExportExportAssets()
	{
		AssetDatabase.ExportPackage(new[]
		{
			"Assets/Altspace/Export Assets",
			"Assets/AWSUnitySDK"
		} , "ExportAssets.unitypackage", ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
	}*/

	/// <summary>
	/// Imports Client Packages
	/// </summary>
	[MenuItem("AltspaceVR/Unity Packages/Import Client Packages")]
	public static void ImportClientPackages()
	{
		var clientRepo = ExportToolsAssetPackages.GetClientRepoPath();
		AssetDatabase.ImportPackage(clientRepo + "Packages/SharedAssets.unitypackage", true);
	}

	/// <summary>
	/// Import vender assets. You apparently can't import two packages at once!
	/// </summary
	[MenuItem("AltspaceVR/Unity Packages/Import Client Vendor Packages")]
	public static void ImportClientVendorPackages()
	{
		var clientRepo = ExportToolsAssetPackages.GetClientRepoPath();
		AssetDatabase.ImportPackage(clientRepo + "Packages/VendorAssets.unitypackage", true);
	}

	private static string GetClientRepoPath()
	{
		var clientRepo = "../UnityClient/";
		if (!System.IO.Directory.Exists(clientRepo + "Packages"))
		{
			EditorUtility.DisplayDialog("Could not find UnityClient", "Your UnityClient folder could not be found. Please select it in the next window.", "Continue");
			clientRepo = EditorUtility.OpenFolderPanel("Select your UnityClient folder", "", "UnityClient") + "/";
		}
		return clientRepo;
	}

	/// <summary>
	/// Exports the ExportTools Package
	/// </summary>
	[MenuItem("AltspaceVR/Unity Packages/Export ExportTools Package")]
	public static void ExportExportToolsPackage()
	{
		AssetDatabase.ExportPackage("Assets", "Packages/ExportTools.unitypackage", ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
	}
}
