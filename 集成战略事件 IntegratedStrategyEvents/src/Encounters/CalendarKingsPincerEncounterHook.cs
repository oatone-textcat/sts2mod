using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace IntegratedStrategyEvents.Encounters;

public sealed class CalendarKingsPincerBossEncounterHook :
	IntegratedStrategyEncounterHook<CalendarKingsPincerBossEncounter>
{
	protected override async Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		await CalendarKingsPincerEncounterSetup.ApplyToCombat(combatState);
	}
}

internal static class CalendarKingsPincerEncounterSetup
{
	public static async Task ApplyToCombat(CombatState combatState)
	{
		IntegratedStrategyBossMusic.CalendarKings.Play();
		if (!IntegratedStrategyEncounterSetup.TryFindEnemyPairBySlots(
			combatState,
			CalendarKingsPincerBossEncounter.LeftSlot,
			CalendarKingsPincerBossEncounter.RightSlot,
			out Creature leftBoss,
			out Creature rightBoss))
		{
			return;
		}

		await IntegratedStrategyEncounterSetup.ApplyTwoSidedBackAttackPowers(combatState, leftBoss, rightBoss);
		IntegratedStrategyEncounterSetup.FaceCreatureBodyRight(leftBoss);
	}
}
