using UOP1.StateMachine.ScriptableObjects;

namespace UOP1.StateMachine
{
	/// <summary>
	/// Class that represents a conditional statement.
	/// </summary>
	public abstract class Condition : IStateComponent
	{
		private bool _isCached = false;
		private bool _cachedStatement = default;
		internal StateConditionSO _originSO;

		/// <summary>
		/// Use this property to access shared data from the <see cref="StateConditionSO"/> that corresponds to this <see cref="Condition"/>
		/// </summary>
		protected StateConditionSO OriginSO => _originSO;

		/// <summary>
		/// Specify the statement to evaluate.
		/// </summary>
		/// <returns></returns>
		protected abstract bool Statement();

		/// <summary>
		/// Wrap the <see cref="Statement"/> so it can be cached.
		/// </summary>
		internal bool GetStatement()
		{
			if (!_isCached)
			{
				_isCached = true;
				_cachedStatement = Statement();
			}

			return _cachedStatement;
		}

		internal void ClearStatementCache()
		{
			_isCached = false;
		}

		/// <summary>
		/// Awake is called when creating a new instance. Use this method to cache the components needed for the condition.
		/// </summary>
		/// <param name="stateMachine">The <see cref="StateMachine"/> this instance belongs to.</param>
		public virtual void Awake(StateMachine stateMachine) { }
		public virtual void OnStateEnter() { }
		public virtual void OnStateExit() { }
	}

	/// <summary>
	/// Struct containing a Condition and its expected result.
	/// </summary>
	
	/*
	 struct 声明上的 readonly：整个结构体不可变
	 字段上的 readonly：只能赋值一次（在调用构造函数的时候）

	 声明"整个类型不可变"，并**强制**下面的实例字段都 readonly；
	 同时让编译器把 `this` 当 readonly 处理、跳过防御性拷贝优化 
	*/
	public readonly struct StateCondition
	{
		internal readonly StateMachine _stateMachine; // 它属于哪个状态机（调试用）
		internal readonly Condition _condition; // 指向某个 Condition 类
		internal readonly bool _expectedResult; // 期望的结果

		public StateCondition(StateMachine stateMachine, Condition condition, bool expectedResult)
		{
			_stateMachine = stateMachine;
			_condition = condition;
			_expectedResult = expectedResult;
		}

		public bool IsMet()
		{
			bool statement = _condition.GetStatement();
			bool isMet = statement == _expectedResult;

/*
Unity 编辑器里运行，编译时 Unity 自动定义：UNITY_EDITOR
除此之外还有这种宏：UNITY_STANDALONE_WIN、UNITY_ANDROID、UNITY_64、UNITY_2021_1_OR_NEWER

Player Settings → Other Settings → Scripting Define Symbols 可以配置自定义宏 
*/

#if UNITY_EDITOR 
			_stateMachine._debugger.TransitionConditionResult(_condition._originSO.name, statement, isMet);
#endif
			return isMet;
		}
	}
}
