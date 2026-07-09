using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class FrostNovaWinterScarBossEncounterHook :
	IntegratedStrategyEncounterHook<FrostNovaWinterScarBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IntegratedStrategyBossMusic.FrostNova.Play();
		return Task.CompletedTask;
	}
}
