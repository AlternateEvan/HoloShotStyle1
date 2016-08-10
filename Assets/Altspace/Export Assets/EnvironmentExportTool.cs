using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.Unity3D;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class EnvironmentExportTool : MonoBehaviour
{
	private AmazonS3Client s3Client = null;

	public enum Platform { PC = 0, MAC, ANDROID };

	[HideInInspector]
	public string bucketName = "environments.altvr.com";

	private const string cloudfrontBucketEndpoint = "dc1gsc5wc5y2l.cloudfront.net";
	private const string positronPublishApiUrl = "https://account.altvr.com/api/environment_asset_bundles";

	[HideInInspector]
	public AWSRegion cognitoRegion = AWSRegion.USEast1;

	[HideInInspector]
	public AWSRegion s3Region = AWSRegion.USWest1;

	[HideInInspector]
	public string cognitoIdentityPool = "us-east-1:07ecd39f-7ec8-4d09-a833-640cdcd2ac20";

	private GameObject AWSPrefabGO;

	public string assetBundleName;
	[HideInInspector]
	public bool buildPCAssetBundle = true;
	[HideInInspector]
	public bool buildMacAssetBundle = true;
	[HideInInspector]
	public bool buildAndroidAssetBundle = true;

	[HideInInspector]
	public string assetBundlePath;

	[HideInInspector]
	public bool IsCurrentlyUploading
	{
		get;
		private set;
	}

	private string generatedAssetBundlePath;
	private string generatedManifestPath;
	public struct PlatformDetail
	{
		public PlatformDetail(EnvironmentExportTool.Platform platform, string filename)
		{
			this.platform = platform;
			platformFilename = filename;
		}
		public EnvironmentExportTool.Platform platform;
		public string platformFilename;
	}
	private PlatformDetail currentPlatformDetail;
	private Stack<PlatformDetail> currentPlatformDetails;

	public void UploadAndPublishAssetBundle(Stack<PlatformDetail> platformDetails)
	{
		currentPlatformDetails = new Stack<PlatformDetail>(platformDetails);
		PlatformDetail poppedPlatform = currentPlatformDetails.Pop ();
		StartCoroutine(UploadAndPublishAssetBundle (poppedPlatform));
	}

	private void HandleRemainingAssetBundles()
	{
		if (currentPlatformDetails.Count > 0) {
			PlatformDetail poppedPlatform = currentPlatformDetails.Pop ();
			StartCoroutine(UploadAndPublishAssetBundle(poppedPlatform));
		} else {
			StopPlayingIfEditor();
		}
	}

	private IEnumerator UploadAndPublishAssetBundle(PlatformDetail platform)
	{
		if (IsCurrentlyUploading)
		{
			Debug.LogWarning("Currently uploading; please wait before requesting another upload.");
			yield return null;
		}

		IsCurrentlyUploading = true;

		string assetBundlePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + platform.platformFilename;
		string manifestPath = assetBundlePath + ".manifest";

		Debug.Log(String.Format("About to UploadAndPublishAssetBundle: {0}\n{1}", assetBundlePath, manifestPath));

		// PrintPolyCount();

		InitializeS3Client();

		generatedAssetBundlePath = GenerateS3AssetBundlePath(assetBundleName);
		generatedManifestPath = generatedAssetBundlePath + ".manifest";
		currentPlatformDetail = platform;

		//asset bundle
		PostFileToS3(assetBundlePath, generatedAssetBundlePath);

		while (IsCurrentlyUploading)
		{
			yield return new WaitForSeconds(0.5f);
		}

		//manifest file
		if (File.Exists(manifestPath))
		{
			IsCurrentlyUploading = true;
			PostFileToS3(manifestPath, generatedManifestPath);
		}

		// LogS3Buckets();
		// LogS3BucketContents();
	}

	private void PostFileToS3(string inputFilePath, string destinationFilePath)
	{
		Stream stream;

		try
		{
			stream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}
		catch (IOException e)
		{
			Debug.LogException(e);
			Debug.LogError(String.Format("Error trying to open filestream for: {0}", inputFilePath));
			return;
		}

		var s3UploadPostRequest = new PostObjectRequest
		{
			Key = destinationFilePath,
			Bucket = bucketName,
			InputStream = stream,
			Region = s3Region.GetRegionEndpoint(),
			CannedACL = S3CannedACL.PublicRead,
		};

		s3Client.PostObjectAsync(s3UploadPostRequest, (result) =>
		{
			PublishUploadedFileResult(result, inputFilePath, destinationFilePath);
		}, null);

	}

	private void InitializeS3Client()
	{
		if (s3Client != null) return;

		CognitoAWSCredentials cognitoCred = new CognitoAWSCredentials(cognitoIdentityPool, cognitoRegion.GetRegionEndpoint());
		s3Client = new AmazonS3Client(cognitoCred, s3Region.GetRegionEndpoint());

		// Instantiate an AWSPrefab so we can access S3
		if (AWSPrefabGO == null)
		{
			AWSPrefabGO = (GameObject)Instantiate(Resources.Load<GameObject>("AWSPrefab"));
		}

		InitializeAmazonInitializer();
	}

	private void InitializeAmazonInitializer()
	{
		// This is a bit of a hack, copy logic in Amazon's scripts to initialize AWS libraries.
		var amazonInitializer = AWSPrefabGO.GetComponent<AmazonInitializer>();

		// prevent the instance from getting destroyed between scenes
		DontDestroyOnLoad(amazonInitializer);

		// load service endpoints from config file
		RegionEndpoint.LoadEndpointDefinitions();

		// add other scripts
		if (amazonInitializer.gameObject.GetComponent<AmazonMainThreadDispatcher>() == null)
			amazonInitializer.gameObject.AddComponent<AmazonMainThreadDispatcher>();

		if (amazonInitializer.gameObject.GetComponent<AmazonNetworkStatusInfo>() == null)
			amazonInitializer.gameObject.AddComponent<AmazonNetworkStatusInfo>();

		// init done
		AmazonInitializer._initialized = true;
	}

	private void PublishUploadedFileResult(AmazonServiceResult result, string inputPath, string destinationPath)
	{
		try
		{
			if (result.Exception != null)
			{
				Debug.LogError(String.Format("Encountered error trying to upload file {0} to {1} bucket",
						Path.GetFileName(inputPath), bucketName));

				LogException(result.Exception);

				return;
			}

			//LogS3PostUploadResponse(result.Response as S3PostUploadResponse);

			string url = String.Format("https://{0}/{1}", cloudfrontBucketEndpoint, destinationPath);

			Debug.Log(String.Format("uploaded: {0}", url));

			//only publish to positron if it's an asset bundle
			if (!inputPath.EndsWith(".manifest"))
			{
				PublishAssetBundleToPositron(url);
			}
		}
		finally
		{
			IsCurrentlyUploading = false;

			//go onto other bundles if it's a manifest file.
			//otherwise, it's an asset bundle and we will wait for it also process the manifest
			if (inputPath.EndsWith(".manifest"))
			{
				HandleRemainingAssetBundles();
			}
		}

	}

	private void PublishAssetBundleToPositron(string fileUrl)
	{
		var publishForm = CreatePositronPublishFormForUrl(fileUrl);

		var positronCredentials = ReadPositronHttpBasicAuthCredentials();
		// NOTE: for some reason you need to pass this through via a copy.
		var headers = publishForm.headers;
		headers["Authorization"] = String.Format("Basic {0}", Convert.ToBase64String(Encoding.ASCII.GetBytes(positronCredentials)));

		//Debug.Log(string.format("Auth header: {0}", headers["Authorization"]));

		WWW www = new WWW(positronPublishApiUrl, publishForm.data, headers);
		while (!www.isDone) { /* Wait */ }

		Debug.Log(www.text);
	}

	private void StopPlayingIfEditor()
	{
#if UNITY_EDITOR
		if (EditorApplication.isPlaying)
			EditorApplication.isPlaying = false;
#endif
	}

	private WWWForm CreatePositronPublishFormForUrl(string assetBundleUrlToPublish)
	{
		WWWForm form = new WWWForm();

		form.AddField("environment_asset_bundle_url", assetBundleUrlToPublish);
		form.AddField("game_engine", "unity");

		if (Application.unityVersion.StartsWith("5.3"))
		{
			form.AddField("game_engine_version", "53");
		}
		else if (Application.unityVersion.StartsWith("5"))
		{
			form.AddField("game_engine_version", "5");
		}
		else
		{
			form.AddField("game_engine_version", "4");
		}

		if (currentPlatformDetail.platform == EnvironmentExportTool.Platform.ANDROID)
		{
			form.AddField("platform", "android");
		}
		else if (currentPlatformDetail.platform == EnvironmentExportTool.Platform.MAC)
		{
			form.AddField("platform", "mac");
		}
		else
		{
			form.AddField("platform", "pc");
		}

		return form;
	}

	private void PrintPolyCount()
	{
        var totalPolyCount = ComputeTotalPolyCount();

        if (totalPolyCount > 50000)
        {
            Debug.LogWarning("Poly count is over 50000!: " + totalPolyCount);
        }
        else if (totalPolyCount > 30000)
        {
            Debug.LogWarning("Poly count is over 30000!: " + totalPolyCount);
        }
	}

	private int ComputeTotalPolyCount()
	{
		int totalPolyCount = 0;
		MeshFilter[] allMeshFilters = FindObjectsOfType<MeshFilter>();

		foreach (MeshFilter mf in allMeshFilters)
		{
			int meshPolyCount = mf.sharedMesh.triangles.Length/3;
			//Debug.Log(mf.gameObject);
			//*mf.gameObject.renderer.sharedMaterials.Length;
			//Debug.Log("before: " + tmpCount.ToString() + ", after: " + (tmpCount/mf.gameObject.renderer.sharedMaterials.Length).ToString());
			totalPolyCount += meshPolyCount;
		}

		Debug.Log("num polys: " + totalPolyCount.ToString());
		return totalPolyCount;
	}

	private static string GenerateS3AssetBundlePath(string assetBundleName)
	{
		// Generate Random SHA
		string randomSha = GenerateRandomSHA();

		var fileExtension = "unity3d";

		if (Application.unityVersion.StartsWith("5"))
		{
			fileExtension = "unity5";
		}

		return String.Format("environments/{0}/{1}/{2}/{0}-{3}.{4}",
			assetBundleName, randomSha.Substring(0, 2), randomSha.Substring(2, 2), randomSha.Substring(0, 8), fileExtension);
	}

	private static string GenerateRandomSHA()
	{
		byte[] bytes = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyyMMddHHmmssfff"));
		StringBuilder sb = new StringBuilder();

		foreach (byte b in (new SHA256Managed()).ComputeHash(bytes))
		{
			sb.Append(b.ToString("x2"));
		}

		return sb.ToString();
	}

	private void LogException(Exception exception)
	{
		Debug.Log(exception.Data);
		Debug.Log(exception.Message);
		Debug.LogException(exception);
	}

	private void LogS3Buckets()
	{
		ListBucketsRequest request = new ListBucketsRequest();
		s3Client.ListBucketsAsync(request, bucketsResponse =>
		{
			if (bucketsResponse.Exception == null)
			{
				ListBucketsResponse response = bucketsResponse.Response as ListBucketsResponse;
				foreach (S3Bucket bucket in response.Buckets)
				{
					Debug.Log(bucket.BucketName);
				}
			}
			else
			{
				Debug.LogException(bucketsResponse.Exception);
				Debug.LogError("ListBucket fail");
			}
		}, null);
	}

	private void LogS3BucketContents() 
	{
		ListObjectsRequest listObjRequest = new ListObjectsRequest();
        listObjRequest.BucketName = bucketName;

        s3Client.ListObjectsAsync(listObjRequest, (listResult) =>
        {
			var unityPackagesLookup = new Dictionary<string, S3Object>();

			if (listResult.Exception == null)
			{
				ListObjectsResponse response = listResult.Response as ListObjectsResponse;

				foreach (S3Object ob in response.S3Objects)
				{
					if (ob.Key.EndsWith(".unity3d") || ob.Key.EndsWith(".unity5"))
					{
						unityPackagesLookup.Add(ob.Key, ob);
					}
				}
			}
			else
			{
				Debug.LogException(listResult.Exception);
				string error = "Could not list unity files on " + bucketName + " bucket.  (ListObjects fail.)";
				Debug.LogError(error);
			}

			foreach (string key in unityPackagesLookup.Keys)
			{
				Debug.Log(key);
			}
        }, null);
	}

	private void LogS3PostUploadResponse(S3PostUploadResponse response)
	{
        Debug.Log(response.ResponseMetadata);
        Debug.Log(response.ContentLength);
        Debug.Log(response.HostId);
        Debug.Log(response.HttpStatusCode);
        Debug.Log(response.RequestId);
        Debug.Log(response.StatusCode);
        Debug.Log(response.ErrorMsg);
	}

	private string ReadPositronHttpBasicAuthCredentials()
	{
		string authFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) +
		                      Path.DirectorySeparatorChar + "altvrEnvirAccount";

		if (!File.Exists(authFilePath))
		{
			Debug.LogError("Authentication key not found. Ask for the altvrEnvirAccount file to put on your desktop!");
			StopPlayingIfEditor();
		}

		try
		{
			using (StreamReader sr = new StreamReader(authFilePath))
			{
				return sr.ReadToEnd().Trim();
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("The file could not be read:");
			Console.WriteLine(e.Message);
			StopPlayingIfEditor();
		}

		return null;
	}
}

