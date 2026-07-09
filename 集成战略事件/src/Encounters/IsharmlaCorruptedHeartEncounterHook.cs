using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IsharmlaCorruptedHeartBossEncounterHook :
	IntegratedStrategyEncounterHook<IsharmlaCorruptedHeartBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IntegratedStrategyBossMusic.Isharmla.Play();
		return Task.CompletedTask;
	}
}
