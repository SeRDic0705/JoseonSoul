using System;
using UnityEngine;

[Serializable]
public class PlayerAnimationData
{
    [SerializeField] private string groundParameterName = "@Ground";
    [SerializeField] private string idleParameterName = "Idle";
    [SerializeField] private string walkParameterName = "Walk";
    [SerializeField] private string runParameterName = "Run";
    [SerializeField] private string avoidParameterName = "Avoid";

    [SerializeField] private string airParameterName = "@Air";
    [SerializeField] private string jumpParameterName = "Jump";
    [SerializeField] private string fallParameterName = "Fall";

    [SerializeField] private string attackParameterName = "@Attack";
    [SerializeField] private string heavyAttackParameterName = "HeavyAttack";


    public int groundParameterHash { get; private set; }
    public int idleParameterHash { get; private set; }
    public int walkParameterHash { get; private set; }
    public int runParameterHash { get; private set; }
    public int avoidParameterHash { get; private set; }

    public int airParameterHash { get; private set; }
    public int jumpParameterHash { get; private set; }
    public int fallParameterHash { get; private set; }

    public int attackParameterHash { get; private set; }
    public int heavyAttackParameterHash { get; private set; }


    public void Initialize()
    {
        groundParameterHash = Animator.StringToHash(groundParameterName);
        idleParameterHash = Animator.StringToHash(idleParameterName);
        walkParameterHash = Animator.StringToHash(walkParameterName);
        runParameterHash = Animator.StringToHash(runParameterName);
        avoidParameterHash = Animator.StringToHash(avoidParameterName);

        airParameterHash = Animator.StringToHash(airParameterName);
        jumpParameterHash = Animator.StringToHash(jumpParameterName);
        fallParameterHash = Animator.StringToHash(fallParameterName);

        attackParameterHash = Animator.StringToHash(attackParameterName);
        heavyAttackParameterHash = Animator.StringToHash(heavyAttackParameterName);
        
    }

}
