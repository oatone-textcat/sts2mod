using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class SorrowfulLockBossEncounterHook :
	IntegratedStrategyEncounterHook<SorrowfulLockBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IntegratedStrategyBossMusic.SorrowfulLock.Play();
		return Task.CompletedTask;
	}
}
