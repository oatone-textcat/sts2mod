using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IzumikEcologicalFountainBossEncounterHook :
	IntegratedStrategyEncounterHook<IzumikEcologicalFountainBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IzumikEcologicalFountainMusicController.Play();
		return Task.CompletedTask;
	}
}

public sealed class IzumikEcologicalFountainTestEncounterHook :
	IntegratedStrategyEncounterHook<IzumikEcologicalFountainTestEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IzumikEcologicalFountainMusicController.Play();
		return Task.CompletedTask;
	}
}
