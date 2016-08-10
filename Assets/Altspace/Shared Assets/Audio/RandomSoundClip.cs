using UnityEngine;
using System.Collections;

public class RandomSoundClip : MonoBehaviour {
	
	public float delayMin = 2;
	public float delayMax = 10;
	public AudioClip[] ac = new AudioClip[6];

	void Start () {
		StartCoroutine(PlaySound());
	}
	
	// Update is called once per frame
	IEnumerator PlaySound()
	{
		AudioSource src = GetComponent<AudioSource>();
		src.loop = false;
		src.clip = null;
		src.playOnAwake = false;
		int prevIndex = 1;
		while (src != null)
		{
			if (src.isPlaying && clipLength() == 0)
			{
				src.Stop();
				src.clip = null;
				yield return null;
			}
			else if (clipLength() > 0)
			{
				int length = ac.Length;
				int index = 0;
				if (clipLength() > 1)
				{
					do
					{
						index = Random.Range(0, length);
					} while (!ac[index] || index == prevIndex);
				}
				prevIndex = index;
				src.clip = ac[index];
				src.Play();
				float waitTime = Random.Range(delayMin, delayMax);
				yield return new WaitForSeconds(waitTime);
			}
		}
	}

	private int cachedClipLength = -1;
	public int clipLength(){
		if (cachedClipLength == -1)
		{
			cachedClipLength = 0;
			for (int i = 0; i < ac.Length; i++)
			{
				if (ac[i])
					cachedClipLength++;
			}
		}
		return cachedClipLength;
	}
}
