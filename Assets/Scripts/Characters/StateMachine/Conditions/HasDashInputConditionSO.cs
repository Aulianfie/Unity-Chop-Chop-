using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UOP1.StateMachine;
using UOP1.StateMachine.ScriptableObjects;
/*
给角色状态机提供一个“玩家是否按下了 Dash”的判断条件。
玩家按下 Dash
→ Protagonist.OnDash()
→ dashInput = true
→ HasDashInputCondition 检查
→ 条件为 true
→ 状态机从 Idle/Walking 切换到 Dash
→ Dash 状态消费输入并执行移动
*/
[CreateAssetMenu(menuName = "State Machines/Conditions/Has Dash Input")]
public class HasDashInputConditionSO : StateConditionSO<HasDashInputCondition>
{
}

public class HasDashInputCondition : Condition
{
	private Protagonist _protagonist;
    // Awake() 的作用是找到同一个角色身上的 Protagonist
	public override void Awake(StateMachine stateMachine)
	{
		_protagonist = stateMachine.GetComponent<Protagonist>();
	}
    // 把主角当前“是否存在 Dash 输入”的布尔值返回给状态机
    protected override bool Statement()
	{
		return _protagonist.HasDashInput;
	}
}
