using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class KuilongMahasattvaAvatarBossEncounterHook :
	IntegratedStrategyEncounterHook<KuilongMahasattvaAvatarBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IntegratedStrategyBossMusic.Kuilong.Play();
		return Task.CompletedTask;
	}
}
