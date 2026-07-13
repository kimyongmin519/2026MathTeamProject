using UnityEngine;
using UnityEngine.Playables;

public class Boojick : MonoBehaviour
{
    [SerializeField] private GameObject gasParticle;
    [SerializeField] private GameObject boojickParticle;
    [SerializeField] private GameObject butthole;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayableDirector director;
    [SerializeField] private PlayableDirector Cleardirector;

    private static readonly int PoopTrigger = Animator.StringToHash("Poop");
    private static readonly int DanceTrigger = Animator.StringToHash("Dance");

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
    public void PlayCutscene()
    {
        director.Play();
    }
    public void PlayClaerCutscene()
    {
        Cleardirector.Play();
    }
}