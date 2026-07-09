using MegaCrit.Sts2.Core.Combat;
using STS2RitsuLib.Models;

namespace IntegratedStrategyEvents.Encounters;

public abstract class IntegratedStrategyEncounterHook<TEncounter> : HookedSingletonModel
	where TEncounter : class
{
	protected IntegratedStrategyEncounterHook()
		: base(HookType.Combat)
	{
	}

	public sealed override Task BeforeCombatStart()
	{
		if (!IntegratedStrategyEncounterSetup.TryGetCombatState<TEncounter>(out CombatState combatState))
		{
			return Task.CompletedTask;
		}

		return BeforeIntegratedStrategyCombatStart(combatState);
	}

	protected abstract Task BeforeIntegratedStrategyCombatStart(CombatState combatState);
}
