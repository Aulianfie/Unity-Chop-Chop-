using UnityEngine;
using UOP1.StateMachine;
using UOP1.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Is Dash Finished")]
public class DashFinishedConditionSO : StateConditionSO<DashFinishedCondition> { }

public class DashFinishedCondition : Condition
{
    private Protagonist _protagonist;

    public override void Awake(StateMachine stateMachine)
    {
        _protagonist = stateMachine.GetComponent<Protagonist>();
    }

    protected override bool Statement() => _protagonist.dashFinished; 
}