using UnityEngine;
using System.Collections;

class AnimateTiledTexture : MonoBehaviour
{
	[SerializeField] private int Columns = 8;
	[SerializeField] private int Rows = 8;
	[SerializeField] private float FramesPerSecond = 10f;
	[SerializeField] private bool AnimateFromTopOfTexture = true;
	
	//the current frame to display
	private int index = 0;
	

	void Start()
	{
		StartCoroutine(updateTiling());
		
		//set the tile size of the texture (in UV units), based on the rows and columns
		// so if there are two rows and two columns, the size would be 0.5 -- which means that 0 - 1.0
		// in U-V space would map to 0.0-0.5, which is the size of one element in the matrix of sub-textures
		Vector2 size = new Vector2(1f / Columns, 1f / Rows);
		GetComponent<Renderer>().sharedMaterial.SetTextureScale("_MainTex", size);
	}

	private IEnumerator updateTiling()
	{
		while (true)
		{
			//move to the next index; check for max index
			index++;
			if (index >= Rows * Columns)
				index = 0;
			
			//split into x and y indexes
			float xIndex = (index%Columns)/(float)Columns; // normalize x index
			float yOffset = (index/Columns)/(float)Rows;
			float yIndex = (AnimateFromTopOfTexture? (1.0f - 1.0f/Rows - yOffset) : yOffset);
			Vector2 offset = new Vector2(xIndex, yIndex);
			GetComponent<Renderer>().sharedMaterial.SetTextureOffset("_MainTex", offset);

			yield return new WaitForSeconds(1f/FramesPerSecond);
		}
		
	}
}