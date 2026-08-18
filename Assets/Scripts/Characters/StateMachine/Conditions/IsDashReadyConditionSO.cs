using UnityEngine;
using UOP1.StateMachine;
using UOP1.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Dash Ready")]
public class IsDashReadyConditionSO : StateConditionSO<IsDashReadyCondition>
{
}

public class IsDashReadyCondition : Condition
{
    private Protagonist _protagonist;

    public override void Awake(StateMachine stateMachine)
    {
        _protagonist = stateMachine.GetComponent<Protagonist>();
    }

    protected override bool Statement()
    {
        return _protagonist.IsDashReady;
    }
}
