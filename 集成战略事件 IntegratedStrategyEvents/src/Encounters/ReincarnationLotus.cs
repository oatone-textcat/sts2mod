using System;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace IntegratedStrategyEvents.Encounters;

public sealed class ReincarnationLotus : MonsterModel
{
	private static readonly Func<CardModel, bool>[] StealPriorities =
	[
		static card => card.Enchantment is not Imbued && card.Rarity == CardRarity.Uncommon,
		static card => card.Enchantment is not Imbued &&
			(card.Rarity == CardRarity.Common ||
				card.Rarity == CardRarity.Rare ||
				card.Rarity == CardRarity.Event),
		static card => card.Enchantment is not Imbued &&
			(card.Rarity == CardRarity.Basic || card.Rarity == CardRarity.Quest),
		static card => card.Rarity == CardRarity.Ancient || card.Enchantment is Imbued
	];

	public const string PuduMoveId = "PUDU_MOVE";
	public const string RebirthMoveId = "REBIRTH_MOVE";
	public const string PilgrimageMoveId = "PILGRIMAGE_MOVE";
	public const string DepartureMoveId = "DEPARTURE_MOVE";

	private const int InitialHp = 80;
	private const int PuduDamage = 10;
	private const decimal InitialArtifactAmount = 2m;
	private const decimal InitialSoarAmount = 1m;
	private const decimal RebirthHeal = 15m;
	private const int PilgrimageBlock = 15;
	private const float IdleActionDelay = 0.45f;
	private const float PuduHitDelay = 0.35f;
	private const float DepartureAnimDelay = 0.2f;
	private const float DepartureWait = 1.2f;
	private const string DepartureTrigger = "DepartureTrigger";

	public override int MinInitialHp => InitialHp;

	public override int MaxInitialHp => InitialHp;

	public override bool HasDeathSfx => false;

	public override bool HasHurtSfx => false;

	public override bool ShouldFadeAfterDeath => true;

	public override float DeathAnimLengthOverride => 1.2f;

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	protected override string VisualsPath => "res://IntegratedStrategyEvents/scenes/creature_visuals/reincarnation_lotus.tscn";

	public override Vector2 ExtraDeathVfxPadding => Vector2.One * 0.5f;

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await PowerCmd.Apply<ArtifactPower>(Creature, InitialArtifactAmount, Creature, null, silent: true);
		await PowerCmd.Apply<SoarPower>(Creature, InitialSoarAmount, Creature, null, silent: true);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState pudu = new(
			PuduMoveId,
			PuduMove,
			new SingleAttackIntent(PuduDamage),
			new CardDebuffIntent());
		MoveState rebirth = new(
			RebirthMoveId,
			RebirthMove,
			new HealIntent());
		MoveState pilgrimage = new(
			PilgrimageMoveId,
			PilgrimageMove,
			new DefendIntent());
		MoveState departure = new(
			DepartureMoveId,
			DepartureMove,
			new EscapeIntent());

		pudu.FollowUpState = rebirth;
		rebirth.FollowUpState = pilgrimage;
		pilgrimage.FollowUpState = departure;
		departure.FollowUpState = departure;
		return new MonsterMoveStateMachine([pudu, rebirth, pilgrimage, departure], pudu);
	}

	private async Task PuduMove(IReadOnlyList<Creature> targets)
	{
		await DamageCmd.Attack(PuduDamage)
			.FromMonster(this)
			.WithNoAttackerAnim()
			.WithWaitBeforeHit(PuduHitDelay, PuduHitDelay)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(null);

		List<CardModel> stolenCards = await StealCards(targets);
		await ApplySwipePowers(stolenCards);
	}

	private async Task RebirthMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await Cmd.Wait(IdleActionDelay);
		await CreatureCmd.Heal(Creature, RebirthHeal);
	}

	private async Task PilgrimageMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await Cmd.Wait(IdleActionDelay);
		await CreatureCmd.GainBlock(Creature, PilgrimageBlock, ValueProp.Move, null);
	}

	private async Task DepartureMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await CreatureCmd.TriggerAnim(Creature, DepartureTrigger, DepartureAnimDelay);
		await Cmd.Wait(DepartureWait);
		if (Creature.IsAlive)
		{
			await CreatureCmd.Escape(Creature);
		}
	}

	private async Task<List<CardModel>> StealCards(IReadOnlyList<Creature> targets)
	{
		List<CardModel> stolenCards = new();
		foreach (Creature target in targets.Where(static creature => creature.IsAlive))
		{
			Player? player = target.Player ?? target.PetOwner;
			if (player == null)
			{
				continue;
			}

			List<CardModel> candidates = CardPile.GetCards(player, PileType.Draw, PileType.Discard)
				.Where(static card => card.DeckVersion != null)
				.ToList();
			CardModel? cardToSteal = ChooseCardToSteal(candidates);
			if (cardToSteal == null)
			{
				continue;
			}

			await CardPileCmd.RemoveFromCombat(cardToSteal);
			stolenCards.Add(cardToSteal);
		}

		return stolenCards;
	}

	private CardModel? ChooseCardToSteal(List<CardModel> cards)
	{
		if (cards.Count == 0)
		{
			return null;
		}

		IEnumerable<CardModel> candidates = cards;
		foreach (Func<CardModel, bool> predicate in StealPriorities)
		{
			List<CardModel> prioritized = cards.Where(predicate).ToList();
			if (prioritized.Count > 0)
			{
				candidates = prioritized;
				break;
			}
		}

		return RunRng.CombatCardGeneration.NextItem(candidates);
	}

	private async Task ApplySwipePowers(IEnumerable<CardModel> stolenCards)
	{
		foreach (CardModel stolenCard in stolenCards)
		{
			SwipePower swipe = (SwipePower)ModelDb.Power<SwipePower>().ToMutable();
			await swipe.Steal(stolenCard);
			await PowerCmd.Apply(swipe, Creature, 1m, Creature, null);
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idle", isLooping: true);
		AnimState start = new("Start")
		{
			NextState = idle
		};
		AnimState die = new("Die");

		CreatureAnimator animator = new(start, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(DepartureTrigger, die);
		animator.AddAnyState(CreatureAnimator.deathTrigger, die);
		return animator;
	}
}
