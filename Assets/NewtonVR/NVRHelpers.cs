using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRHelpers
    {
        private static Shader standardShader;
        private static Shader StandardShader
        {
            get
            {
                if (standardShader == null)
                {
                    standardShader = Shader.Find("Standard");
                }
                return standardShader;
            }
        }

        public static void SetTransparent(Material material, Color? newcolor = null)
        {
            if (material.shader != StandardShader)
                Debug.LogWarning("Trying to set transparent mode on non-standard shader. Please use the Standard Shader instead or modify this method.");
			// AltspaceVR TODO
			//get rid of transparent stuff -- should be "lighter" in color instead of transparent. make it more emissive

			/*material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0f);
            material.renderQueue = 3000;
            material.mainTexture = null;

            if (newcolor != null)
            {
                material.color = newcolor.Value;
            }*/
			material.EnableKeyword("_EMISSION");
			material.SetColor("_EmissionColor", new Color(0.133f, 0.133f, 0.133f));

			// AltspaceVR end TODO
        }
        
        public static void SetOpaque(Material material)
        {
            if (material.shader != StandardShader)
                Debug.LogWarning("Trying to set opaque mode on non-standard shader. Please use the Standard Shader instead or modify this method.");
			// AltspaceVR TODO
			// instead of plain opaque, make less emissive
			/*material.SetOverrideTag("RenderType", "");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;*/
			material.EnableKeyword("_EMISSION");
			material.SetColor("_EmissionColor", new Color(0.0f, 0.0f, 0.0f));
			// end AltspaceVR TODO
		}
	}
}