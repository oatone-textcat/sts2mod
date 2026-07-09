using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class BozhokastiSaintguardGunnerBossEncounterHook :
	IntegratedStrategyEncounterHook<BozhokastiSaintguardGunnerBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IntegratedStrategyBossMusic.Bozhokasti.Play();
		return Task.CompletedTask;
	}
}
