using IntegratedStrategyEvents.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using System.Runtime.CompilerServices;

namespace IntegratedStrategyEvents.Encounters;

public sealed class ReincarnationLotusDuoEncounterHook :
	IntegratedStrategyEncounterHook<ReincarnationLotusDuoEncounter>
{
	private const int RequiredLotusCount = 2;
	private static readonly ConditionalWeakTable<CombatState, Tracker> Trackers = new();

	protected override Task BeforeIntegratedStrategyCombatStart(CombatState combatState)
	{
		Creature[] lotuses = IntegratedStrategyEncounterSetup.FindEnemies<ReincarnationLotus>(
			combatState,
			RequiredLotusCount);
		Trackers.Remove(combatState);
		if (lotuses.Length >= RequiredLotusCount)
		{
			Trackers.Add(combatState, new Tracker(lotuses.Take(RequiredLotusCount).ToArray()));
		}

		return Task.CompletedTask;
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		CombatState combatState = room.CombatState;
		EncounterModel? encounter = combatState.Encounter;
		if (encounter?.CanonicalInstance is not ReincarnationLotusDuoEncounter ||
			!Trackers.TryGetValue(combatState, out Tracker? tracker))
		{
			return Task.CompletedTask;
		}

		Trackers.Remove(combatState);
		if (!tracker.WereAllLotusesDefeated(combatState))
		{
			return Task.CompletedTask;
		}

		foreach (Player player in combatState.Players)
		{
			if (player.Relics.Any(static relic => relic is AnasaKarmaRelic && !relic.HasBeenRemovedFromState))
			{
				continue;
			}

			room.AddExtraReward(player, new RelicReward(ModelDb.Relic<AnasaKarmaRelic>().ToMutable(), player));
		}

		return Task.CompletedTask;
	}

	private sealed class Tracker(Creature[] lotuses)
	{
		public bool WereAllLotusesDefeated(CombatState combatState)
		{
			return lotuses.All(static lotus => lotus.IsDead) &&
				!lotuses.Any(combatState.EscapedCreatures.Contains);
		}
	}
}
