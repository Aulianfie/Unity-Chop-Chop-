using UOP1.StateMachine.ScriptableObjects;

namespace UOP1.StateMachine
{
	public class State
	{
		// internal 的意思是只能在同一个程序集（assembly）中访问，不能被外部程序集访问。
		// public是所有程序集都能访问，private是只能在类内部访问，protected是只能在类及其子类访问。
		/*
		"程序集"指的就是 Unity 里的 asmdef 划分。本项目里：
		- UOP1.StateMachine.asmdef  →  程序集 A（状态机框架，含 State.cs / StateMachine.cs / TransitionTableSO.cs）
		- UOP1.StateMachine.Editor.asmdef → 程序集 B（编辑器工具，调试器）
		- Assembly-CSharp（默认）   →  程序集 C（游戏逻辑，Protagonist.cs / NPC.cs 等）
		*/
		internal StateSO _originSO;
		internal StateMachine _stateMachine; // 反向引用挂在自己身上的那个 StateMachine 组件
		internal StateTransition[] _transitions;
		internal StateAction[] _actions;

		internal State() { }

		public State(
			StateSO originSO,
			StateMachine stateMachine,
			StateTransition[] transitions,
			StateAction[] actions)
		{
			_originSO = originSO;
			_stateMachine = stateMachine;
			_transitions = transitions;
			_actions = actions;
		}

		public void OnStateEnter()
		{
			// 这是"局部函数"：只能在 OnStateEnter() 内部用的小工具函数
			void OnStateEnter(IStateComponent[] comps)
			{
				for (int i = 0; i < comps.Length; i++)
					comps[i].OnStateEnter(); // 遍历组件，挨个调用它们的 OnStateEnter
			}
			OnStateEnter(_transitions); // 先通知所有"转换条件"
			OnStateEnter(_actions); // 再通知所有"行为"
		}

		public void OnUpdate()
		{
			for (int i = 0; i < _actions.Length; i++)
				_actions[i].OnUpdate();
		}

		public void OnStateExit()
		{
			void OnStateExit(IStateComponent[] comps)
			{
				for (int i = 0; i < comps.Length; i++)
					comps[i].OnStateExit();
			}
			OnStateExit(_transitions);
			OnStateExit(_actions);
		}

		/*
			注意：这里的out是用来返回一个额外的值的。它允许方法返回多个值。
			在这个方法中，out参数state用于返回满足条件的目标状态。
			如果方法返回true，表示存在一个满足条件的转换，并且state参数将被赋值为目标状态。
			如果方法返回false，表示没有满足条件的转换，state参数将被赋值为null。
			所以，
			- return的bool 说"是否transition"
			- out 说"如果要transition到哪"
		*/
		public bool TryGetTransition(out State state)
		{
			state = null;  // ① 默认"没有目标状态"

			for (int i = 0; i < _transitions.Length; i++)
				if (_transitions[i].TryGetTransiton(out state))  // ② 挨个查询每条"转换线"
					break; // 如果存在条件满足的，后面的不再看

			for (int i = 0; i < _transitions.Length; i++)
				_transitions[i].ClearConditionsCache(); // ③ 清掉条件缓存

			return state != null; // ④ 有目标状态 → true
		}
	}
}
