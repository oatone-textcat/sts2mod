using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class KuilongMahasattvaAvatarTestEncounterHook :
	IntegratedStrategyEncounterHook<KuilongMahasattvaAvatarTestEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		KuilongMahasattvaAvatarMusicController.Play();
		return Task.CompletedTask;
	}
}

public sealed class KuilongMahasattvaAvatarBossEncounterHook :
	IntegratedStrategyEncounterHook<KuilongMahasattvaAvatarBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		KuilongMahasattvaAvatarMusicController.Play();
		return Task.CompletedTask;
	}
}
