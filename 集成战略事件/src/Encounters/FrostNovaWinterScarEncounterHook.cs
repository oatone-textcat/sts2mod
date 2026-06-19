using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class FrostNovaWinterScarBossEncounterHook :
	IntegratedStrategyEncounterHook<FrostNovaWinterScarBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		FrostNovaWinterScarMusicController.Play();
		return Task.CompletedTask;
	}
}

public sealed class FrostNovaWinterScarTestEncounterHook :
	IntegratedStrategyEncounterHook<FrostNovaWinterScarTestEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		FrostNovaWinterScarMusicController.Play();
		return Task.CompletedTask;
	}
}
