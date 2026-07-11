using UnityEngine;

public class Boojick : MonoBehaviour
{

    [SerializeField] private GameObject gasParticle;
    [SerializeField] private GameObject boojickParticle;

    [SerializeField] private GameObject butthole;
    
    public void Gas()
    {
        Instantiate(gasParticle, butthole.transform);
    }

    public void Boojicks()
    {
        Instantiate (boojickParticle, butthole.transform);
    }
}
