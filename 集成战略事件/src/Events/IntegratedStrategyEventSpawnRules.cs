using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.Events;

internal static partial class IntegratedStrategyEventSpawnRules
{
	public static bool IsAllowed(Type eventType, IRunState runState)
	{
		return !AllowRules.TryGetValue(eventType, out Func<IRunState, bool>? isAllowed)
			|| isAllowed(runState);
	}
}
