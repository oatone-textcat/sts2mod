using Godot;
using IntegratedStrategyEvents.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IzumikEcologicalFountain : MonsterModel, IIzumikEcologicalFountain
{
	public const string SummonLeftSlot = "izumik_offspring_left";
	public const string SummonCenterSlot = "izumik_offspring_center";
	public const string SummonRightSlot = "izumik_offspring_right";
	public const string BossSlot = "izumik";

	private static readonly string[] SummonSlots =
	[
		SummonLeftSlot,
		SummonCenterSlot,
		SummonRightSlot
	];

	public const string RejuvenationMoveId = "REJUVENATION_MOVE";
	public const string InterpretationMoveId = "INTERPRETATION_MOVE";
	public const string LearningMoveId = "LEARNING_MOVE";
	public const string ShockMoveId = "SHOCK_MOVE";

	private const int InitialMaxHp = 650;
	// 开局 40% 最大生命，随后三次回春每次回复 20% 最大生命（合计回满）。
	private const decimal InitialCurrentHpRatio = 0.4m;
	private const decimal RejuvenationHealRatio = 0.2m;
	private const decimal InitialIntangible = 3m;
	private const int InterpretationDamage = 8;
	private const int InterpretationHits = 2;
	private const int LearningHeal = 20;
	private const int LearningBlock = 20;
	private const int ShockDamage = 38;
	private const int ShockDazedCount = 3;
	private const float IdleMoveDelay = 0.45f;
	private const float ReviveDelay = 0.9f;
	private const float AttackDelay = 0.75f;
	private const float ShockDelay = 0.95f;
	private const string ReviveTrigger = "ReviveTrigger";
	private const string ShockTrigger = "ShockTrigger";
	private const string AttackSfxPath = "event:/sfx/enemy/enemy_attacks/soul_fysh/soul_fysh_wave";
	private const string ShockSfxPath = "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_flame";

	public override int MinInitialHp => InitialMaxHp;

	public override int MaxInitialHp => InitialMaxHp;

	public override bool HasDeathSfx => false;

	public override bool HasHurtSfx => false;

	public override bool ShouldFadeAfterDeath => true;

	public override float DeathAnimLengthOverride => 1.3f;

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

	protected override string VisualsPath =>
		"res://IntegratedStrategyEvents/scenes/creature_visuals/izumik_ecological_fountain.tscn";

	public override Vector2 ExtraDeathVfxPadding => Vector2.One * 0.8f;

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		Creature.PowerApplied += AfterPowerApplied;
		Creature.PowerRemoved += AfterPowerRemoved;
		await PowerCmd.Apply<IzumikSurvivalPower>(Creature, 1m, Creature, null, silent: true);
		await PowerCmd.Apply<IntangiblePower>(Creature, InitialIntangible, Creature, null, silent: true);
		UpdateIntangibleVisuals();
		await SetScaledInitialCurrentHp();
	}

	public override void BeforeRemovedFromRoom()
	{
		Creature.PowerApplied -= AfterPowerApplied;
		Creature.PowerRemoved -= AfterPowerRemoved;
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		_ = choiceContext;
		_ = wasRemovalPrevented;
		_ = deathAnimLength;
		if (creature == Creature)
		{
			SetColor(Colors.White);
		}

		return Task.CompletedTask;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState rejuvenation1 = CreateRejuvenationState("REJUVENATION_MOVE_1", playReviveAfterHeal: false);
		MoveState rejuvenation2 = CreateRejuvenationState("REJUVENATION_MOVE_2", playReviveAfterHeal: false);
		MoveState rejuvenation3 = CreateRejuvenationState(RejuvenationMoveId, playReviveAfterHeal: true);
		MoveState interpretation = new(
			InterpretationMoveId,
			InterpretationMove,
			new MultiAttackIntent(InterpretationDamage, InterpretationHits));
		MoveState learning = new(
			LearningMoveId,
			LearningMove,
			new HealIntent(),
			new DefendIntent());
		MoveState shock = new(
			ShockMoveId,
			ShockMove,
			new SingleAttackIntent(ShockDamage),
			new StatusIntent(ShockDazedCount));

		rejuvenation1.FollowUpState = rejuvenation2;
		rejuvenation2.FollowUpState = rejuvenation3;
		rejuvenation3.FollowUpState = interpretation;
		interpretation.FollowUpState = learning;
		learning.FollowUpState = shock;
		shock.FollowUpState = interpretation;
		return new MonsterMoveStateMachine(
			[rejuvenation1, rejuvenation2, rejuvenation3, interpretation, learning, shock],
			rejuvenation1);
	}

	private MoveState CreateRejuvenationState(string id, bool playReviveAfterHeal)
	{
		return new MoveState(
			id,
			targets => RejuvenationMove(targets, playReviveAfterHeal),
			new HealIntent());
	}

	private async Task SetScaledInitialCurrentHp()
	{
		await CreatureCmd.SetCurrentHp(Creature, Math.Ceiling(Creature.MaxHp * InitialCurrentHpRatio));
	}

	private void AfterPowerApplied(PowerModel power)
	{
		if (power is IntangiblePower)
		{
			SetIntangibleVisuals(isIntangible: true);
		}
	}

	private void AfterPowerRemoved(PowerModel power)
	{
		if (power is IntangiblePower)
		{
			SetIntangibleVisuals(isIntangible: false);
		}
	}

	private void UpdateIntangibleVisuals()
	{
		SetIntangibleVisuals(Creature.HasPower<IntangiblePower>());
	}

	private void SetIntangibleVisuals(bool isIntangible)
	{
		SetColor(isIntangible ? StsColors.halfTransparentWhite : Colors.White);
	}

	private void SetColor(Color color)
	{
		NCombatRoom.Instance?.GetCreatureNode(Creature)?.GetSpecialNode<CanvasGroup>("%CanvasGroup")?.SetSelfModulate(color);
	}

	public async Task SummonOffspringToEmptySlots()
	{
		foreach (string slot in SummonSlots)
		{
			await SummonOffspringIfSlotEmpty(slot);
		}
	}

	private async Task SummonOffspringIfSlotEmpty(string slotName)
	{
		if (CombatState == null ||
			Creature.IsDead ||
			CombatState.Encounter?.Slots.Contains(slotName) != true ||
			IsSummonSlotOccupied(slotName))
		{
			return;
		}

		Creature offspring = await CreatureCmd.Add<IzumikOffspring>(CombatState, slotName);
		if (!offspring.HasPower<MinionPower>())
		{
			await PowerCmd.Apply<MinionPower>(offspring, 1m, Creature, null, silent: true);
		}
	}

	private bool IsSummonSlotOccupied(string slotName)
	{
		return CombatState?.Enemies.Any(creature => creature.IsAlive && creature.SlotName == slotName) == true;
	}

	private async Task RejuvenationMove(IReadOnlyList<Creature> targets, bool playReviveAfterHeal)
	{
		_ = targets;
		await Cmd.Wait(IdleMoveDelay);
		await CreatureCmd.Heal(Creature, Math.Ceiling(Creature.MaxHp * RejuvenationHealRatio));
		if (playReviveAfterHeal)
		{
			await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, ReviveTrigger, ReviveDelay);
		}
	}

	private async Task InterpretationMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(InterpretationDamage)
			.FromMonster(this)
			.WithHitCount(InterpretationHits)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0f)
			.WithWaitBeforeHit(AttackDelay, AttackDelay)
			.WithHitFx("vfx/vfx_attack_slash", AttackSfxPath)
			.OnlyPlayAnimOnce()
			.Execute(null);
	}

	private async Task LearningMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, CreatureAnimator.attackTrigger, AttackDelay);
		await CreatureCmd.Heal(Creature, LearningHeal);
		await CreatureCmd.GainBlock(Creature, LearningBlock, ValueProp.Move, null);
	}

	private async Task ShockMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(ShockDamage)
			.FromMonster(this)
			.WithAttackerAnim(ShockTrigger, 0f)
			.WithWaitBeforeHit(ShockDelay, ShockDelay)
			.WithHitFx("vfx/vfx_attack_blunt", ShockSfxPath)
			.Execute(null);

		foreach (Creature target in CombatState.PlayerCreatures.Where(static creature => creature.IsAlive))
		{
			await AddDazedCardsToDiscard(target);
		}
	}

	private async Task AddDazedCardsToDiscard(Creature target)
	{
		Player? player = target.Player ?? target.PetOwner;
		if (player == null || player.Creature.IsDead || target.CombatState == null)
		{
			return;
		}

		CardPileAddResult[] addedCards = new CardPileAddResult[ShockDazedCount];
		for (int i = 0; i < ShockDazedCount; i++)
		{
			addedCards[i] = await CardPileCmd.AddGeneratedCardToCombat(
				CombatState.CreateCard<Dazed>(player),
				PileType.Discard,
				player,
				CardPilePosition.Bottom);
		}

		if (LocalContext.IsMe(player))
		{
			CardCmd.PreviewCardPileAdd(addedCards, 1.2f, CardPreviewStyle.HorizontalLayout);
			await Cmd.Wait(0.8f);
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idle", isLooping: true);
		AnimState attack = new("Attack")
		{
			NextState = idle
		};
		AnimState revive = new("Revive")
		{
			NextState = idle
		};
		AnimState shock = new("Skill_2")
		{
			NextState = idle
		};
		AnimState die = new("Die");

		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(CreatureAnimator.attackTrigger, attack);
		animator.AddAnyState(ReviveTrigger, revive);
		animator.AddAnyState(ShockTrigger, shock);
		animator.AddAnyState(CreatureAnimator.deathTrigger, die);
		return animator;
	}
}
