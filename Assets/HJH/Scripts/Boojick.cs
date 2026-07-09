using UnityEngine;

public class Boojick : MonoBehaviour
{
    [SerializeField] private GameObject gasParticle;
    [SerializeField] private GameObject boojickParticle;
    [SerializeField] private GameObject butthole;
    [SerializeField] private Animator animator;

    private static readonly int PoopTrigger = Animator.StringToHash("Poop");

    public void PlayPoopSequence()
    {
        animator.SetTrigger(PoopTrigger);
    }

    public void Gas()
    {
        Instantiate(gasParticle, butthole.transform.position, Quaternion.identity);
    }

    public void Boojicks()
    {
        Instantiate(boojickParticle, butthole.transform.position, Quaternion.identity);
    }
}