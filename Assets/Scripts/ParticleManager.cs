using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ParticleType
{
    BUBBLE_DESTROYED = 0
}

public class ParticleManager : MonoBehaviour {

    [SerializeField]
    private ParticleSystem particleBubbleDestroyed;

    [SerializeField]
    private int particlePoolSize = 10;

    private ParticleSystem[] bubbleDestroyedParticles;

    // This will tell us if any particle is currently being used. A particle with index i
    // corresponds to a isUsed value of index i 
    private bool[] particleInUse;

    // Singleton
    private static ParticleManager sharedInstace;
    public static ParticleManager SharedInstance { get { return sharedInstace; } }

    void Awake()
    {
        if (sharedInstace != null && sharedInstace != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            sharedInstace = this;
        }
    }

	// Use this for initialization
	void Start () {
        bubbleDestroyedParticles = new ParticleSystem[particlePoolSize];
        particleInUse = new bool[particlePoolSize];

        for (int i = 0; i < particlePoolSize; i++)
        {
            ParticleSystem particle = Instantiate(particleBubbleDestroyed);
            particle.transform.parent = this.transform;
            particle.GetComponent<Renderer>().sortingLayerName = "Foreground";
            bubbleDestroyedParticles[i] = particle;
            particleInUse[i] = false;
        }
    }

    private void Update()
    {
        for (int i = 0; i < particlePoolSize; i++)
        {
            if(!bubbleDestroyedParticles[i].isPlaying)
            {
                particleInUse[i] = false;
                bubbleDestroyedParticles[i].gameObject.SetActive(false);
            }
                
        }
    }

    public bool PlayParticle(ParticleType type, Bubble bubble)
    {
        ParticleSystem particle = GetFreeParticle();
        if(particle != null)
        {
            Vector3 pos = bubble.transform.position;
            particle.gameObject.SetActive(true);
            particle.transform.position = new Vector3(pos.x, pos.y, -2);
            particle.Play();
        }

        return false;
    }

    private ParticleSystem GetFreeParticle()
    {
        for (int i = 0; i < particlePoolSize; i++)
        {
            if (!particleInUse[i])
            {
                particleInUse[i] = true;
                return bubbleDestroyedParticles[i];
            }
                
;       }

        return null;
    }

}
