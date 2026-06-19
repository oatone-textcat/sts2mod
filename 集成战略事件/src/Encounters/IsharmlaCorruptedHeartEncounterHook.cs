using MegaCrit.Sts2.Core.Combat;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IsharmlaCorruptedHeartBossEncounterHook :
	IntegratedStrategyEncounterHook<IsharmlaCorruptedHeartBossEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IsharmlaCorruptedHeartMusicController.Play();
		return Task.CompletedTask;
	}
}

public sealed class IsharmlaCorruptedHeartTestEncounterHook :
	IntegratedStrategyEncounterHook<IsharmlaCorruptedHeartTestEncounter>
{
	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		_ = combatState;
		IsharmlaCorruptedHeartMusicController.Play();
		return Task.CompletedTask;
	}
}
