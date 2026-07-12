using Godot;
using IntegratedStrategyEvents.Encounters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace IntegratedStrategyEvents.Relics;

public sealed class ProphecyProjectionRelic : IntegratedStrategyEventRelic
{
	private const int RandomRelicRewardCount = 2;
	private const float SpawnedChoralePlayerXOffset = 320f;
	private const float SpawnedChoralePlayerYOffset = 0f;
	private const float SpawnedChoraleFallbackX = 260f;
	private const float SpawnedChoraleFallbackY = 150f;

	private uint? _activeChoraleCombatId;
	private int _lastChoraleHpBeforeZero;
	private bool _directlyKilledThisCombat;
	private bool _defeatedThisCombat;
	private bool _rewardAuthorityThisCombat;

	public ProphecyProjectionRelic()
		: base("prophecy_projection.png")
	{
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedProphecyProjectionChoraleHp
	{
		get => _choraleHp;
		set
		{
			_choraleHp = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private bool SavedProphecyProjectionChoraleDefeated
	{
		get => _choraleDefeated;
		set
		{
			_choraleDefeated = value;
			if (value)
			{
				_choraleHp = 0;
			}

			InvokeDisplayAmountChanged();
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private bool SavedProphecyProjectionChoraleRewardsGranted
	{
		get => _choraleRewardsGranted;
		set => _choraleRewardsGranted = value;
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private bool SavedProphecyProjectionUsesScaledChoraleHp
	{
		get => _usesScaledChoraleHp;
		set => _usesScaledChoraleHp = value;
	}

	private int _choraleHp;
	private bool _choraleDefeated;
	private bool _choraleRewardsGranted;
	private bool _usesScaledChoraleHp;

	public override bool ShowCounter => !IsCanonical;

	public override int DisplayAmount => !IsCanonical && !_choraleDefeated
		? Math.Max(0, GetDisplayChoraleHp())
		: 0;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		new HoverTip(
			new LocString("relics", "PROPHECY_PROJECTION_RELIC.final_chorale.title"),
			new LocString("relics", "PROPHECY_PROJECTION_RELIC.final_chorale.description"))
		{
			Id = $"{ModInfo.ModId}.final_chorale"
		}
	];

	public override async Task BeforeCombatStart()
	{
		_activeChoraleCombatId = null;
		_lastChoraleHpBeforeZero = 0;
		_directlyKilledThisCombat = false;
		_defeatedThisCombat = false;
		_rewardAuthorityThisCombat = false;

		if (Owner == null)
		{
			return;
		}

		Creature ownerCreature = Owner.Creature;
		ICombatState? combatState = ownerCreature.CombatState;
		if (combatState == null)
		{
			return;
		}

		SynchronizeSharedChoraleState(combatState);
		if (_choraleDefeated || GetSharedChoraleHp(combatState) <= 0)
		{
			return;
		}

		Creature? existingChorale = FindExistingChorale(combatState);
		ProphecyProjectionRelic? authority = GetChoraleAuthority(combatState);
		if (!ReferenceEquals(this, authority))
		{
			if (existingChorale != null)
			{
				TrackChorale(existingChorale, rewardAuthority: false);
			}

			return;
		}

		_rewardAuthorityThisCombat = true;
		if (existingChorale != null)
		{
			TrackChorale(existingChorale, rewardAuthority: true);
			return;
		}

		FinalChorale chorale = (FinalChorale)ModelDb.Monster<FinalChorale>().ToMutable();
		Creature creature = combatState.CreateCreature(chorale, CombatSide.Enemy, slot: null);
		creature.SetCurrentHpInternal(GetSharedChoraleHp(combatState));

		Flash();
		await CreatureCmd.Add(creature);
		PositionSpawnedChorale(creature);
		await PowerCmd.Apply<MinionPower>(creature, 1m, ownerCreature, null, silent: true);

		TrackChorale(creature, rewardAuthority: true);
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (!IsTrackedChorale(creature))
		{
			return Task.CompletedTask;
		}

		if (creature.CurrentHp <= 0 && delta < 0m)
		{
			_lastChoraleHpBeforeZero = Math.Max(1, creature.CurrentHp - (int)delta);
		}

		if (creature.CombatState != null)
		{
			SetSharedChoraleHp(creature.CombatState, creature.CurrentHp);
		}
		else
		{
			SetChoraleHp(creature.CurrentHp);
		}

		return Task.CompletedTask;
	}

	public override Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		_ = choiceContext;
		_ = dealer;
		_ = props;
		_ = cardSource;

		if (IsTrackedChorale(target) && result.WasTargetKilled)
		{
			_directlyKilledThisCombat = true;
		}

		return Task.CompletedTask;
	}

	public override bool ShouldDie(Creature creature)
	{
		if (!IsTrackedChorale(creature)
			|| _directlyKilledThisCombat
			|| HasLivingPrimaryEnemy(creature.CombatState))
		{
			return true;
		}

		int preservedHp = Math.Max(1, _lastChoraleHpBeforeZero);
		creature.SetCurrentHpInternal(preservedHp);
		if (creature.CombatState != null)
		{
			SetSharedChoraleHp(creature.CombatState, preservedHp);
		}
		else
		{
			SetChoraleHp(preservedHp);
		}

		return false;
	}

	public override Task AfterDeath(
		PlayerChoiceContext choiceContext,
		Creature creature,
		bool wasRemovalPrevented,
		float deathAnimLength)
	{
		_ = choiceContext;
		_ = deathAnimLength;

		if (!IsTrackedChorale(creature) || wasRemovalPrevented)
		{
			return Task.CompletedTask;
		}

		if (creature.CombatState != null)
		{
			SetSharedChoraleDefeated(creature.CombatState);
		}
		else
		{
			SavedProphecyProjectionChoraleHp = 0;
			SavedProphecyProjectionChoraleDefeated = true;
			_defeatedThisCombat = true;
		}

		Flash(Array.Empty<Creature>());
		return Task.CompletedTask;
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (Owner == null ||
			!_defeatedThisCombat ||
			_choraleRewardsGranted ||
			!_rewardAuthorityThisCombat ||
			!ReferenceEquals(this, GetRewardAuthority(room.CombatState)))
		{
			ClearCombatTracking();
			return Task.CompletedTask;
		}

		foreach (Player player in room.CombatState.Players)
		{
			for (int i = 0; i < RandomRelicRewardCount; i++)
			{
				room.AddExtraReward(player, new RelicReward(player));
			}

			room.AddExtraReward(player, new RelicReward(ModelDb.Relic<EndlessKeyRelic>().ToMutable(), player));
		}

		SetSharedChoraleRewardsGranted(room.CombatState);
		ClearCombatTracking();
		return Task.CompletedTask;
	}

	private void SetChoraleHp(int value)
	{
		_usesScaledChoraleHp = true;
		SavedProphecyProjectionChoraleHp = Math.Max(0, value);
	}

	private void TrackChorale(Creature creature, bool rewardAuthority)
	{
		_activeChoraleCombatId = creature.CombatId;
		_rewardAuthorityThisCombat = rewardAuthority;
		if (creature.CombatState != null)
		{
			SetSharedChoraleHp(creature.CombatState, creature.CurrentHp);
		}
		else
		{
			SetChoraleHp(creature.CurrentHp);
		}
	}

	private bool IsTrackedChorale(Creature creature)
	{
		return _activeChoraleCombatId.HasValue
			&& creature.CombatId == _activeChoraleCombatId
			&& creature.Monster is FinalChorale;
	}

	private static ProphecyProjectionRelic? GetChoraleAuthority(ICombatState combatState)
	{
		List<ProphecyProjectionRelic> projections = GetActiveProjectionRelics(combatState).ToList();
		int initialHp = GetInitialChoraleHp(combatState);
		return projections.FirstOrDefault(projection =>
				!projection._choraleDefeated &&
				!projection._choraleRewardsGranted &&
				projection.GetNormalizedChoraleHp(initialHp) > 0) ??
			projections.FirstOrDefault(static projection =>
				!projection._choraleDefeated &&
				!projection._choraleRewardsGranted);
	}

	private static ProphecyProjectionRelic? GetRewardAuthority(ICombatState combatState)
	{
		return GetActiveProjectionRelics(combatState).FirstOrDefault();
	}

	private static IEnumerable<ProphecyProjectionRelic> GetActiveProjectionRelics(ICombatState combatState)
	{
		foreach (Player player in combatState.Players)
		{
			if (!player.IsActiveForHooks)
			{
				continue;
			}

			foreach (RelicModel relic in player.Relics)
			{
				if (relic is ProphecyProjectionRelic projection &&
					!projection.IsMelted &&
					!projection.HasBeenRemovedFromState)
				{
					yield return projection;
				}
			}
		}
	}

	private static Creature? FindExistingChorale(ICombatState combatState)
	{
		return combatState.Enemies.FirstOrDefault(static creature => creature.Monster is FinalChorale);
	}

	private static void SynchronizeSharedChoraleState(ICombatState combatState)
	{
		List<ProphecyProjectionRelic> projections = GetActiveProjectionRelics(combatState).ToList();
		if (projections.Count == 0)
		{
			return;
		}

		bool defeated = projections.Any(static projection =>
			projection._choraleDefeated || projection._choraleRewardsGranted);
		bool rewardsGranted = projections.Any(static projection => projection._choraleRewardsGranted);
		int sharedHp = defeated
			? 0
			: GetSharedChoraleHp(combatState, projections);

		foreach (ProphecyProjectionRelic projection in projections)
		{
			if (defeated)
			{
				projection._choraleDefeated = true;
			}

			if (rewardsGranted)
			{
				projection._choraleRewardsGranted = true;
			}

			projection.SetChoraleHp(sharedHp);
		}
	}

	private static int GetSharedChoraleHp(ICombatState combatState)
	{
		return GetSharedChoraleHp(combatState, GetActiveProjectionRelics(combatState).ToList());
	}

	private static int GetSharedChoraleHp(
		ICombatState combatState,
		IReadOnlyCollection<ProphecyProjectionRelic> projections)
	{
		if (projections.Any(static projection => projection._choraleDefeated))
		{
			return 0;
		}

		int initialHp = GetInitialChoraleHp(combatState);
		int savedHp = projections
			.Select(projection => projection.GetNormalizedChoraleHp(initialHp))
			.Where(static hp => hp > 0)
			.DefaultIfEmpty(0)
			.Max();
		return savedHp > 0 ? savedHp : initialHp;
	}

	private static void SetSharedChoraleHp(ICombatState combatState, int value)
	{
		int hp = Math.Max(0, value);
		foreach (ProphecyProjectionRelic projection in GetActiveProjectionRelics(combatState))
		{
			projection.SetChoraleHp(hp);
		}
	}

	private static void SetSharedChoraleDefeated(ICombatState combatState)
	{
		foreach (ProphecyProjectionRelic projection in GetActiveProjectionRelics(combatState))
		{
			projection.SavedProphecyProjectionChoraleHp = 0;
			projection.SavedProphecyProjectionChoraleDefeated = true;
			projection._defeatedThisCombat = true;
		}
	}

	private static void SetSharedChoraleRewardsGranted(ICombatState combatState)
	{
		foreach (ProphecyProjectionRelic projection in GetActiveProjectionRelics(combatState))
		{
			projection.SavedProphecyProjectionChoraleRewardsGranted = true;
		}
	}

	private int GetInitialChoraleHpForOwner()
	{
		if (Owner?.Creature.CombatState is ICombatState combatState)
		{
			return GetInitialChoraleHp(combatState);
		}

		if (Owner?.RunState != null)
		{
			return GetInitialChoraleHp(Owner.RunState, encounter: null);
		}

		return FinalChorale.InitialHp;
	}

	private int GetDisplayChoraleHp()
	{
		int initialHp = GetInitialChoraleHpForOwner();
		if (_choraleHp <= 0)
		{
			return initialHp;
		}

		return GetNormalizedChoraleHp(initialHp);
	}

	private int GetNormalizedChoraleHp(int initialHp)
	{
		if (!_usesScaledChoraleHp &&
			_choraleHp == FinalChorale.InitialHp &&
			initialHp > FinalChorale.InitialHp)
		{
			return initialHp;
		}

		return _choraleHp;
	}

	private static int GetInitialChoraleHp(ICombatState combatState)
	{
		return GetInitialChoraleHp(combatState.RunState, combatState.Encounter);
	}

	private static int GetInitialChoraleHp(IRunState runState, EncounterModel? encounter)
	{
		int actIndex = Math.Clamp(runState.CurrentActIndex, 0, 2);
		return (int)Creature.ScaleHpForMultiplayer(
			FinalChorale.InitialHp,
			encounter,
			runState.Players.Count,
			actIndex);
	}

	private static bool HasLivingPrimaryEnemy(ICombatState? combatState)
	{
		return combatState?.Enemies.Any(static enemy => enemy.IsAlive && enemy.IsPrimaryEnemy) == true;
	}

	private static void PositionSpawnedChorale(Creature creature)
	{
		NCombatRoom? room = NCombatRoom.Instance;
		NCreature? choraleNode = room?.GetCreatureNode(creature);
		if (room == null || choraleNode == null)
		{
			return;
		}

		NCreature? playerNode = room.CreatureNodes.FirstOrDefault(static node => node.Entity.IsPlayer);
		if (playerNode == null)
		{
			choraleNode.Position = new Vector2(SpawnedChoraleFallbackX, SpawnedChoraleFallbackY);
			return;
		}

		choraleNode.Position = new Vector2(
			playerNode.Position.X + SpawnedChoralePlayerXOffset,
			playerNode.Position.Y + SpawnedChoralePlayerYOffset);
	}

	private void ClearCombatTracking()
	{
		_activeChoraleCombatId = null;
		_lastChoraleHpBeforeZero = 0;
		_directlyKilledThisCombat = false;
		_defeatedThisCombat = false;
		_rewardAuthorityThisCombat = false;
	}
}
