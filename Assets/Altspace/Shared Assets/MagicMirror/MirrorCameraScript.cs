using UnityEngine;
using System.Collections.Generic;

public class MirrorCameraScript : MonoBehaviour
{
	/// <summary>
	/// The list of all active mirrors currently in the Unity scene.
	/// </summary>
	public readonly static List<MirrorCameraScript> ActiveMirrors = new List<MirrorCameraScript>();

	/// <summary>
	/// The camera responsible for rendering the view from the mirror.
	/// </summary>
	public Camera CameraObject { get; private set; }

	/// <summary>
	/// The renderer that the mirror renders onto.
	/// </summary>
	public Renderer MirrorRenderer { get; private set; }

	public GameObject MirrorObject;

	private Material mirrorMaterial;
	private MirrorScript mirrorScript;
	private RenderTexture reflectionTexture;
	private Matrix4x4 reflectionMatrix;
	private int oldReflectionTextureSize;
	private static bool renderingMirror;

	private void OnEnable()
	{
		ActiveMirrors.Add(this);
	}

	// Cleanup all the objects we possibly have created
	private void OnDisable()
	{
		ActiveMirrors.Remove(this);

		if (reflectionTexture)
		{
			DestroyImmediate(reflectionTexture);
			reflectionTexture = null;
		}
	}

	private void Start()
	{
		mirrorScript = GetComponentInParent<MirrorScript>();
		CameraObject = GetComponent<Camera>();

		if (mirrorScript.AddFlareLayer)
		{
			CameraObject.gameObject.AddComponent<FlareLayer>();
		}

		MirrorRenderer = MirrorObject.GetComponent<Renderer>();
		if (Application.isPlaying)
		{
			MirrorRenderer.sharedMaterial = MirrorRenderer.material;
		}
		mirrorMaterial = MirrorRenderer.sharedMaterial;

		CreateRenderTexture();
	}

	private void CreateRenderTexture()
	{
		if (reflectionTexture == null || oldReflectionTextureSize != mirrorScript.TextureSize)
		{
			if (reflectionTexture)
			{
				DestroyImmediate(reflectionTexture);
			}
			reflectionTexture = new RenderTexture(mirrorScript.TextureSize, mirrorScript.TextureSize, 16);
			reflectionTexture.filterMode = FilterMode.Bilinear;
			reflectionTexture.antiAliasing = 1;
			reflectionTexture.name = "MirrorRenderTexture_" + GetInstanceID();
			reflectionTexture.hideFlags = HideFlags.HideAndDontSave;
			reflectionTexture.generateMips = false;
			reflectionTexture.wrapMode = TextureWrapMode.Clamp;
			mirrorMaterial.SetTexture("_MainTex", reflectionTexture);
			oldReflectionTextureSize = mirrorScript.TextureSize;
		}

		if (CameraObject.targetTexture != reflectionTexture)
		{
			CameraObject.targetTexture = reflectionTexture;
		}
	}

	private void Update()
	{
		CreateRenderTexture();
	}

	private void UpdateCameraProperties(Camera src, Camera dest)
	{
		dest.clearFlags = src.clearFlags;
		dest.backgroundColor = src.backgroundColor;
		if (src.clearFlags == CameraClearFlags.Skybox)
		{
			Skybox sky = src.GetComponent<Skybox>();
			Skybox mysky = dest.GetComponent<Skybox>();
			if (!sky || !sky.material)
			{
				mysky.enabled = false;
			}
			else
			{
				mysky.enabled = true;
				mysky.material = sky.material;
			}
		}

		dest.orthographic = src.orthographic;
		dest.orthographicSize = src.orthographicSize;
		/*if (mirrorScript.AspectRatio > 0.0f)
		{
			dest.aspect = mirrorScript.AspectRatio;
		}
		else
		{
			dest.aspect = src.aspect;
		}*/
		// force the destination to use source's aspect ratio, otherwise strange things happen yo
		dest.aspect = src.aspect;
		dest.cullingMask = ~(1 << 4) & mirrorScript.ReflectLayers.value;
	}

	internal void RenderMirror()
	{
		Camera cameraLookingAtThisMirror;

		// bail if we don't have a camera or renderer
		if (renderingMirror || !enabled || (cameraLookingAtThisMirror = Camera.current) == null ||
			MirrorRenderer == null || mirrorMaterial == null || !MirrorRenderer.enabled)
		{
			return;
		}

		renderingMirror = true;

		int oldPixelLightCount = QualitySettings.pixelLightCount;
		if (QualitySettings.pixelLightCount != mirrorScript.MaximumPerPixelLights)
		{
			QualitySettings.pixelLightCount = mirrorScript.MaximumPerPixelLights;
		}

		try
		{
			UpdateCameraProperties(cameraLookingAtThisMirror, CameraObject);

			if (mirrorScript.MirrorRecursion)
			{
				mirrorMaterial.EnableKeyword("MIRROR_RECURSION");
				CameraObject.ResetWorldToCameraMatrix();
				CameraObject.ResetProjectionMatrix();
				CameraObject.projectionMatrix = CameraObject.projectionMatrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));
				GL.invertCulling = true;
				CameraObject.Render();
				GL.invertCulling = false;
			}
			else
			{
				mirrorMaterial.DisableKeyword("MIRROR_RECURSION");
				Vector3 pos = transform.position;
				Vector3 normal = (mirrorScript.NormalIsForward ? transform.forward : transform.up);

				// Reflect camera around reflection plane
				float d = -Vector3.Dot(normal, pos) - mirrorScript.ClipPlaneOffset;
				Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
				CalculateReflectionMatrix(ref reflectionPlane);
				Vector3 oldpos = CameraObject.transform.position;
				Vector3 newpos = reflectionMatrix.MultiplyPoint(oldpos);
				Matrix4x4 worldToCameraMatrix = cameraLookingAtThisMirror.worldToCameraMatrix * reflectionMatrix;
				CameraObject.worldToCameraMatrix = worldToCameraMatrix;

				// Clip out background
				Vector4 clipPlane = CameraSpacePlane(ref worldToCameraMatrix, ref pos, ref normal, 1.0f);
				CameraObject.projectionMatrix = cameraLookingAtThisMirror.CalculateObliqueMatrix(clipPlane);
				GL.invertCulling = true;
				CameraObject.useOcclusionCulling = false;
				CameraObject.transform.position = newpos;
				CameraObject.nearClipPlane = cameraLookingAtThisMirror.nearClipPlane;
				CameraObject.farClipPlane = cameraLookingAtThisMirror.farClipPlane;
				CameraObject.fieldOfView = cameraLookingAtThisMirror.fieldOfView;
				CameraObject.Render();
				CameraObject.transform.position = oldpos;
				GL.invertCulling = false;
			}
		}
		finally
		{
			renderingMirror = false;
			if (QualitySettings.pixelLightCount != oldPixelLightCount)
			{
				QualitySettings.pixelLightCount = oldPixelLightCount;
			}
		}
	}

	private Vector4 CameraSpacePlane(ref Matrix4x4 worldToCameraMatrix, ref Vector3 pos, ref Vector3 normal, float sideSign)
	{
		Vector3 offsetPos = pos + normal * mirrorScript.ClipPlaneOffset;
		Vector3 cpos = worldToCameraMatrix.MultiplyPoint(offsetPos);
		Vector3 cnormal = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
		return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
	}

	private void CalculateReflectionMatrix(ref Vector4 plane)
	{
		// Calculates reflection matrix around the given plane

		reflectionMatrix.m00 = (1F - 2F * plane[0] * plane[0]);
		reflectionMatrix.m01 = (-2F * plane[0] * plane[1]);
		reflectionMatrix.m02 = (-2F * plane[0] * plane[2]);
		reflectionMatrix.m03 = (-2F * plane[3] * plane[0]);

		reflectionMatrix.m10 = (-2F * plane[1] * plane[0]);
		reflectionMatrix.m11 = (1F - 2F * plane[1] * plane[1]);
		reflectionMatrix.m12 = (-2F * plane[1] * plane[2]);
		reflectionMatrix.m13 = (-2F * plane[3] * plane[1]);

		reflectionMatrix.m20 = (-2F * plane[2] * plane[0]);
		reflectionMatrix.m21 = (-2F * plane[2] * plane[1]);
		reflectionMatrix.m22 = (1F - 2F * plane[2] * plane[2]);
		reflectionMatrix.m23 = (-2F * plane[3] * plane[2]);

		reflectionMatrix.m30 = 0F;
		reflectionMatrix.m31 = 0F;
		reflectionMatrix.m32 = 0F;
		reflectionMatrix.m33 = 1F;
	}

	private static void CalculateObliqueMatrix(ref Matrix4x4 projection, ref Vector4 clipPlane)
	{
		Vector4 q = projection.inverse * new Vector4(
			Sign(clipPlane.x),
			Sign(clipPlane.y),
			1.0f,
			1.0f
		);
		Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
		// third row = clip plane - fourth row
		projection[2] = c.x - projection[3];
		projection[6] = c.y - projection[7];
		projection[10] = c.z - projection[11];
		projection[14] = c.w - projection[15];
	}

	private static float Sign(float a)
	{
		if (a > 0.0f) return 1.0f;
		if (a < 0.0f) return -1.0f;
		return 0.0f;
	}
}
