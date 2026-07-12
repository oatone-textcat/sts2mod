using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IzumikEcologicalFountainBossEncounterHook :
	IntegratedStrategyEncounterHook<IzumikEcologicalFountainBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IntegratedStrategyBossMusic.Izumik.Play();
		return Task.CompletedTask;
	}
}
