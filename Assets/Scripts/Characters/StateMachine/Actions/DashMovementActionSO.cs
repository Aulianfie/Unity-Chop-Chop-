using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UOP1.StateMachine;
using UOP1.StateMachine.ScriptableObjects;

// ActionSO 是一个 ScriptableObject，用于定义 DashMovementAction 的参数
// DashMovementAction 是一个 StateAction，用于处理角色的 Dash 移动逻辑
// 这个文件定义了一个 DashMovementActionSO 类，它继承自 StateActionSO<DashMovementAction>
// 用于创建 DashMovementAction 的实例。
[CreateAssetMenu(
	fileName = "DashMovement",
	menuName = "State Machines/Actions/Dash Movement")]
public class DashMovementActionSO : StateActionSO<DashMovementAction>
{
    public float Speed => _speed;
    public float InputThreshold  => _inputThreshold;

    [SerializeField, Min(0f)] private float _speed = 10f;
    [SerializeField, Min(0f)] private float _inputThreshold = 0.05f;
}

public class DashMovementAction : StateAction
{
    private new DashMovementActionSO OriginSO => (DashMovementActionSO)base.OriginSO;
    private Protagonist _protagonist;
	private Vector3 _dashDirection;

    // Awake() 的作用是找到挂载到的 Protagonist
	public override void Awake(StateMachine stateMachine)
	{
		_protagonist = stateMachine.GetComponent<Protagonist>();
	}
    // OnStateEnter() 只在刚进入 Dash 状态时调用一次
	public override void OnStateEnter()
	{
		Vector3 input = _protagonist.movementInput;
        input.y = 0f; // Ignore vertical input for dashing

        // InputThreshold 防止摇杆轻微偏移导致角色在没有明显输入的情况下触发 Dash
        float thresholdSqr = OriginSO.InputThreshold * OriginSO.InputThreshold;
        // 如果玩家输入的移动向量长度大于阈值平方，则更新 dash 方向为当前输入方向
        if(input.sqrMagnitude > thresholdSqr) 
        {
            _dashDirection = input.normalized;
        }
        else // 否则，如果玩家没有输入移动向量，则让dash朝向玩家当前朝向
        {
            _dashDirection = _protagonist.transform.forward;
            _dashDirection.y = 0f;  // 之所以y要设置为0，是因为我们只想在水平面上移动，而不希望角色在垂直方向上移动。
            _dashDirection.Normalize();
        }
        _protagonist.ConsumeDashInput(); // 消费掉 dash 输入，防止重复触发
	}

	public override void OnUpdate()
	{
		Vector3 velocity = _protagonist.movementVector; // 获取当前角色的移动向量
        // 设置角色的移动向量为 dash 方向乘以速度
        velocity.x = _dashDirection.x * OriginSO.Speed; 
        velocity.z = _dashDirection.z * OriginSO.Speed;
        
        _protagonist.movementVector = velocity;
	}
}