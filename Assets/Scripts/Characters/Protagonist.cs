using System;
using UnityEngine;
// 主角入口 


/// <summary>
/// <para>This component consumes input on the InputReader and stores its values. The input is then read, and manipulated, by the StateMachines's Actions.</para>
/// </summary>
public class Protagonist : MonoBehaviour
{
	[SerializeField] private InputReader _inputReader = default;
	[SerializeField] private TransformAnchor _gameplayCameraTransform = default;

	private Vector2 _inputVector;
	private float _previousSpeed;

	//These fields are read and manipulated by the StateMachine actions
	[NonSerialized] public bool jumpInput;
	[NonSerialized] public bool extraActionInput;
	[NonSerialized] public bool attackInput;
	/*
	movementInput和movementVector的区别：
	- movementInput：表示玩家的输入方向和强度，通常是一个归一化的向量
		范围在0到1之间。它反映了玩家希望角色移动的方向和速度。
	- movementVector：表示角色实际的移动向量，通常是根据movementInput计算得出的。
		它可能会受到角色的速度、加速度、摩擦
	*/
	[NonSerialized] public Vector3 movementInput; //Initial input coming from the Protagonist script
	[NonSerialized] public Vector3 movementVector; //Final movement vector, manipulated by the StateMachine actions
	[NonSerialized] public ControllerColliderHit lastHit;
	[NonSerialized] public bool isRunning; // Used when using the keyboard to run, brings the normalised speed to 1
	[NonSerialized] public bool dashInput;

	/*
		public bool HasDashInput => dashInput; 
		等价于 
		public bool HasDashInput
		{
			get
			{
				return dashInput;
			}
		}
		HasDashInput 是一个公开的只读属性。
		外部读取它时，返回当前的 dashInput。
		外部不能写 HasDashInput = false，因为它没有 set。
	*/
	public bool HasDashInput => dashInput;
	
	public void ConsumeDashInput()
	{
		dashInput = false;
	}

	public const float GRAVITY_MULTIPLIER = 5f;
	public const float MAX_FALL_SPEED = -50f;
	public const float MAX_RISE_SPEED = 100f;
	public const float GRAVITY_COMEBACK_MULTIPLIER = .03f;
	public const float GRAVITY_DIVIDER = .6f;
	public const float AIR_RESISTANCE = 5f;

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		lastHit = hit;
	}

	//Adds listeners for events being triggered in the InputReader script
	private void OnEnable()
	{
		_inputReader.JumpEvent += OnJumpInitiated;
		_inputReader.JumpCanceledEvent += OnJumpCanceled;
		_inputReader.MoveEvent += OnMove;
		_inputReader.StartedRunning += OnStartedRunning;
		_inputReader.StoppedRunning += OnStoppedRunning;
		_inputReader.AttackEvent += OnStartedAttack;
		_inputReader.DashEvent += OnDash;
		//...
	}

	//Removes all listeners to the events coming from the InputReader script
	private void OnDisable()
	{
		_inputReader.JumpEvent -= OnJumpInitiated;
		_inputReader.JumpCanceledEvent -= OnJumpCanceled;
		_inputReader.MoveEvent -= OnMove;
		_inputReader.StartedRunning -= OnStartedRunning;
		_inputReader.StoppedRunning -= OnStoppedRunning;
		_inputReader.AttackEvent -= OnStartedAttack;
		_inputReader.DashEvent -= OnDash;
		//...
	}

	private void Update()
	{
		RecalculateMovement();
	}

	private void RecalculateMovement()
	{
		float targetSpeed;
		Vector3 adjustedMovement;

		if (_gameplayCameraTransform.isSet)
		{
			//Get the two axes from the camera and flatten them on the XZ plane
			Vector3 cameraForward = _gameplayCameraTransform.Value.forward;
			cameraForward.y = 0f;
			Vector3 cameraRight = _gameplayCameraTransform.Value.right;
			cameraRight.y = 0f;
			// ① 计算"摄像机相对方向"
			//    取摄像机 forward/right，压平到 XZ 平面，
			//    再和 2D 输入(_inputVector) 组合成世界空间方向。
			//    效果：按"上"永远是"远离镜头"的方向，而不是世界坐标的 +Z。
			//Use the two axes, modulated by the corresponding inputs, and construct the final vector
			adjustedMovement = cameraRight.normalized * _inputVector.x +
				cameraForward.normalized * _inputVector.y;
		}
		else
		{
			//No CameraManager exists in the scene, so the input is just used absolute in world-space
			Debug.LogWarning("No gameplay camera in the scene. Movement orientation will not be correct.");
			adjustedMovement = new Vector3(_inputVector.x, 0f, _inputVector.y);
		}

		// ② 无输入时的兜底
		//    如果摇杆没动(sqrMagnitude==0)，保持角色当前朝向，
		//    避免向量变 zero 导致角色瞬间转向 x:0, z:0。
		//Fix to avoid getting a Vector3.zero vector, which would result in the player turning to x:0, z:0
		if (_inputVector.sqrMagnitude == 0f)
			adjustedMovement = transform.forward * (adjustedMovement.magnitude + .01f);

		// ③ 速度渐变（加速/减速）
		//    targetSpeed = Clamp01(输入大小)，范围 0~1
		//    isRunning(Shift)  → 强制拉到 1（键盘玩家"跑"）
		//    attackInput        → 压低到 0.05（攻击时几乎停下）
		//    Lerp 上一帧速度 → 目标速度，Time.deltaTime * 4 控制过渡快慢
		//Accelerate/decelerate
		targetSpeed = Mathf.Clamp01(_inputVector.magnitude);
		if (targetSpeed > 0f)
		{
			// This is used to set the speed to the maximum if holding the Shift key,
			// to allow keyboard players to "run"
			if (isRunning)
				targetSpeed = 1f;

			if (attackInput)
				targetSpeed = .05f;
		}
		targetSpeed = Mathf.Lerp(_previousSpeed, targetSpeed, Time.deltaTime * 4f);

		// ④ 输出
		movementInput = adjustedMovement.normalized * targetSpeed;

		_previousSpeed = targetSpeed;
	}

	//---- EVENT LISTENERS ----

	private void OnMove(Vector2 movement)
	{

		_inputVector = movement;
	}
	// 添加跳跃输入，按住space越久，跳跃高度越高，参考 AscendActionSO.cs
	private void OnJumpInitiated()
	{
		jumpInput = true;
	}

	private void OnJumpCanceled()
	{
		jumpInput = false;
	}

	private void OnDash()
	{
		dashInput = true;
		Debug.Log($"Dash cached: {dashInput}");
	}

	private void OnStoppedRunning() => isRunning = false;

	private void OnStartedRunning() => isRunning = true;


	private void OnStartedAttack() => attackInput = true;

	// Triggered from Animation Event
	public void ConsumeAttackInput() => attackInput = false;
}
