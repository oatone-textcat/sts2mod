using System;
using Godot;
using IntegratedStrategyEvents.Cards;
using IntegratedStrategyEvents.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntegratedStrategyEvents.Encounters;

public sealed class FrostNovaWinterScar : MonsterModel
{
	public const string BossSlot = "frostnova";

	public const string ColdWaveMoveId = "COLD_WAVE_MOVE";
	public const string PhaseOneIceSpikeMoveId = "PHASE_ONE_ICE_SPIKE_MOVE";
	public const string PhaseOneIceRingMoveId = "PHASE_ONE_ICE_RING_MOVE";
	public const string PhaseOneFreezeMoveId = "PHASE_ONE_FREEZE_MOVE";
	public const string ReviveMoveId = "REVIVE_MOVE";
	public const string PhaseTwoIcicleMoveId = "PHASE_TWO_ICICLE_MOVE";
	public const string PhaseTwoIceRingMoveId = "PHASE_TWO_ICE_RING_MOVE";
	public const string PhaseTwoFreezeMoveId = "PHASE_TWO_FREEZE_MOVE";

	private const int InitialHp = 200;
	private const decimal ColdWaveSlow = 1m;
	private const int PhaseOneIceSpikeDamage = 3;
	private const int PhaseOneIceSpikeHits = 4;
	private const int PhaseOneIceRingDamage = 20;
	private const int PhaseOneFrozenCards = 2;
	private const int PhaseTwoIcicleDamage = 8;
	private const int PhaseTwoIcicleHits = 3;
	private const int PhaseTwoIceRingDamage = 30;
	private const int PhaseTwoFrozenCards = 3;
	private const float ReviveAnimationSplitRatio = 0.3f;
	private const float ReviveAnimationFallbackLength = 1.8f;
	private const float ColdWaveDelay = 0.65f;
	private const float IceSpikeFirstHitDelay = 0.65f;
	private const float IceSpikeFollowUpHitDelay = 0.08f;
	private const float IceRingHitDelay = 0.9f;
	private const float FreezeDelay = 0.75f;
	private const string IceRingTrigger = "IceRing";
	private const string ReviveBeginTrigger = "ReviveBegin";
	private const string ReviveEndTrigger = "ReviveEnd";
	private const string FreezeTrigger = "Freeze";
	private const string ColdWaveSfxPath = "event:/sfx/enemy/enemy_attacks/spectral_knight/spectral_knight_hex";
	private const string IceSpikeSfxPath =
		"event:/sfx/enemy/enemy_attacks/spectral_knight/spectral_knight_soul_slash";
	private const string IceRingSfxPath = "event:/sfx/enemy/enemy_attacks/soul_fysh/soul_fysh_wave";
	private const string FreezeSfxPath = "event:/sfx/enemy/enemy_attacks/spectral_knight/spectral_knight_hex";

	[NonSerialized]
	private MegaSprite? _spineController;

	private MoveState? _reviveState;
	private bool _hasRevived;

	public override int MinInitialHp => InitialHp;

	public override int MaxInitialHp => InitialHp;

	public override bool HasDeathSfx => false;

	public override bool HasHurtSfx => false;

	public override bool ShouldFadeAfterDeath => true;

	public override bool ShouldDisappearFromDoom => HasRevived;

	public override float DeathAnimLengthOverride => 1.25f;

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

	protected override string VisualsPath =>
		"res://IntegratedStrategyEvents/scenes/creature_visuals/frost_nova_winter_scar.tscn";

	public override Vector2 ExtraDeathVfxPadding => Vector2.One * 0.8f;

	public bool HasRevived => _hasRevived;

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await PowerCmd.Apply<SnowMonsterPower>(Creature, 1m, Creature, null);
	}

	public async Task TriggerReviveWaitingState(float deathAnimLength)
	{
		if (HasRevived || _reviveState == null)
		{
			return;
		}

		SetMoveImmediate(_reviveState, forceTransition: true);
		await PauseReviveAnimationAtMidpoint(deathAnimLength);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState coldWave = new(
			ColdWaveMoveId,
			ColdWaveMove,
			new DebuffIntent())
		{
			MustPerformOnceBeforeTransitioning = true
		};
		MoveState phaseOneIceSpike = new(
			PhaseOneIceSpikeMoveId,
			PhaseOneIceSpikeMove,
			new MultiAttackIntent(PhaseOneIceSpikeDamage, PhaseOneIceSpikeHits));
		MoveState phaseOneIceRing = new(
			PhaseOneIceRingMoveId,
			PhaseOneIceRingMove,
			new SingleAttackIntent(PhaseOneIceRingDamage));
		MoveState phaseOneFreeze = new(
			PhaseOneFreezeMoveId,
			PhaseOneFreezeMove,
			new StatusIntent(PhaseOneFrozenCards));
		MoveState revive = new(
			ReviveMoveId,
			ReviveMove,
			new HealIntent())
		{
			MustPerformOnceBeforeTransitioning = true
		};
		MoveState phaseTwoIcicle = new(
			PhaseTwoIcicleMoveId,
			PhaseTwoIcicleMove,
			new MultiAttackIntent(PhaseTwoIcicleDamage, PhaseTwoIcicleHits));
		MoveState phaseTwoIceRing = new(
			PhaseTwoIceRingMoveId,
			PhaseTwoIceRingMove,
			new SingleAttackIntent(PhaseTwoIceRingDamage));
		MoveState phaseTwoFreeze = new(
			PhaseTwoFreezeMoveId,
			PhaseTwoFreezeMove,
			new StatusIntent(PhaseTwoFrozenCards));

		_reviveState = revive;

		coldWave.FollowUpState = phaseOneIceSpike;
		phaseOneIceSpike.FollowUpState = phaseOneIceRing;
		phaseOneIceRing.FollowUpState = phaseOneFreeze;
		phaseOneFreeze.FollowUpState = phaseOneIceSpike;
		revive.FollowUpState = phaseTwoIcicle;
		phaseTwoIcicle.FollowUpState = phaseTwoIceRing;
		phaseTwoIceRing.FollowUpState = phaseTwoFreeze;
		phaseTwoFreeze.FollowUpState = phaseTwoIcicle;

		return new MonsterMoveStateMachine(
			[
				coldWave,
				phaseOneIceSpike,
				phaseOneIceRing,
				phaseOneFreeze,
				revive,
				phaseTwoIcicle,
				phaseTwoIceRing,
				phaseTwoFreeze
			],
			coldWave);
	}

	private async Task ColdWaveMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		SfxCmd.Play(ColdWaveSfxPath);
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, CreatureAnimator.attackTrigger, ColdWaveDelay);
		await PowerCmd.Apply<SlowPower>(LivingPlayerCreatures(), ColdWaveSlow, Creature, null);
	}

	private async Task PhaseOneIceSpikeMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await IceSpikeAttack(PhaseOneIceSpikeDamage, PhaseOneIceSpikeHits);
	}

	private async Task PhaseOneIceRingMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await IceRingAttack(PhaseOneIceRingDamage);
	}

	private async Task PhaseOneFreezeMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await AddFrozenCardsWithAnimation(PhaseOneFrozenCards);
	}

	private async Task ReviveMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await CreatureCmd.Heal(Creature, Creature.MaxHp);
		await PlayReviveAnimationSecondHalf();
		_hasRevived = true;
		Creature.GetPower<SnowMonsterPower>()?.CompleteRevive();
	}

	private async Task PhaseTwoIcicleMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await IceSpikeAttack(PhaseTwoIcicleDamage, PhaseTwoIcicleHits);
	}

	private async Task PhaseTwoIceRingMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await IceRingAttack(PhaseTwoIceRingDamage);
	}

	private async Task PhaseTwoFreezeMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await AddFrozenCardsWithAnimation(PhaseTwoFrozenCards);
	}

	private async Task IceRingAttack(int damage)
	{
		await DamageCmd.Attack(damage)
			.FromMonster(this)
			.WithAttackerAnim(IceRingTrigger, 0f)
			.WithWaitBeforeHit(IceRingHitDelay, IceRingHitDelay)
			.WithHitFx("vfx/vfx_attack_blunt", IceRingSfxPath)
			.Execute(null);
	}

	private async Task IceSpikeAttack(int damage, int hits)
	{
		await DamageCmd.Attack(damage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0f)
			.WithWaitBeforeHit(IceSpikeFirstHitDelay, IceSpikeFirstHitDelay)
			.WithHitFx("vfx/vfx_attack_slash", IceSpikeSfxPath)
			.Execute(null);

		for (int i = 1; i < hits; i++)
		{
			await Cmd.CustomScaledWait(IceSpikeFollowUpHitDelay, IceSpikeFollowUpHitDelay);
			await DamageCmd.Attack(damage)
				.FromMonster(this)
				.WithNoAttackerAnim()
				.WithHitFx("vfx/vfx_attack_slash", IceSpikeSfxPath)
				.Execute(null);
		}
	}

	private async Task AddFrozenCardsWithAnimation(int count)
	{
		SfxCmd.Play(FreezeSfxPath);
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, FreezeTrigger, FreezeDelay);

		foreach (Creature target in LivingPlayerCreatures())
		{
			await AddFrozenCardsToHand(target, count);
		}
	}

	private async Task AddFrozenCardsToHand(Creature target, int count)
	{
		Player? player = target.Player ?? target.PetOwner;
		if (player == null || player.Creature.IsDead || target.CombatState == null)
		{
			return;
		}

		for (int i = 0; i < count; i++)
		{
			await CardPileCmd.AddGeneratedCardToCombat(
				CombatState.CreateCard<Frozen>(player),
				PileType.Hand,
				player,
				CardPilePosition.Bottom);
		}
	}

	private async Task PauseReviveAnimationAtMidpoint(float deathAnimLength)
	{
		float duration = GetReviveAnimationDuration(deathAnimLength);
		float midpoint = GetReviveAnimationMidpoint(duration);
		await Cmd.Wait(midpoint);
		HoldReviveAnimationAt(midpoint);
	}

	private async Task PlayReviveAnimationSecondHalf()
	{
		float duration = GetReviveAnimationDuration(ReviveAnimationFallbackLength);
		float midpoint = GetReviveAnimationMidpoint(duration);
		await CreatureCmd.TriggerAnim(Creature, ReviveEndTrigger, 0f);

		MegaAnimationState? animationState = _spineController?.GetAnimationState();
		MegaTrackEntry? currentTrack = animationState?.GetCurrent(0);
		if (animationState == null || currentTrack == null)
		{
			await Cmd.Wait(duration - midpoint);
			return;
		}

		duration = Math.Max(duration, currentTrack.GetAnimationEnd());
		midpoint = GetReviveAnimationMidpoint(duration);
		currentTrack.SetTimeScale(1f);
		currentTrack.SetTrackTime(ClampAnimationTime(midpoint, duration));
		ApplyAnimationState(animationState);
		await Cmd.Wait(Math.Max(0.05f, duration - midpoint));
	}

	private float GetReviveAnimationDuration(float fallbackLength)
	{
		MegaTrackEntry? currentTrack = _spineController?.GetAnimationState().GetCurrent(0);
		float duration = currentTrack?.GetAnimationEnd() ?? 0f;
		return duration > 0f ? duration : Math.Max(fallbackLength, ReviveAnimationFallbackLength);
	}

	private static float GetReviveAnimationMidpoint(float duration)
	{
		return Math.Max(0.05f, duration * ReviveAnimationSplitRatio);
	}

	private void HoldReviveAnimationAt(float trackTime)
	{
		MegaAnimationState? animationState = _spineController?.GetAnimationState();
		MegaTrackEntry? currentTrack = animationState?.GetCurrent(0);
		if (animationState == null || currentTrack == null)
		{
			return;
		}

		float duration = GetReviveAnimationDuration(ReviveAnimationFallbackLength);
		currentTrack.SetTrackTime(ClampAnimationTime(trackTime, duration));
		currentTrack.SetTimeScale(0f);
		ApplyAnimationState(animationState);
	}

	private static float ClampAnimationTime(float trackTime, float duration)
	{
		return Mathf.Clamp(trackTime, 0f, Math.Max(0f, duration - 0.01f));
	}

	private void ApplyAnimationState(MegaAnimationState animationState)
	{
		MegaSkeleton? skeleton = _spineController?.GetSkeleton();
		if (skeleton != null)
		{
			animationState.Update(0f);
			animationState.Apply(skeleton);
		}
	}

	private IReadOnlyList<Creature> LivingPlayerCreatures()
	{
		return CombatState.PlayerCreatures
			.Where(static creature => creature.IsAlive && creature.IsPlayer)
			.ToList();
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		_spineController = controller;
		AnimState idle = new("Idle", isLooping: true);
		AnimState attack = new("Attack")
		{
			NextState = idle
		};
		AnimState iceRing = new("Skill_3")
		{
			NextState = idle
		};
		AnimState reviveBegin = new("Skill_2");
		AnimState reviveEnd = new("Skill_2")
		{
			NextState = idle
		};
		AnimState freeze = new("Skill_1")
		{
			NextState = idle
		};
		AnimState die = new("Die");

		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(CreatureAnimator.attackTrigger, attack);
		animator.AddAnyState(IceRingTrigger, iceRing);
		animator.AddAnyState(ReviveBeginTrigger, reviveBegin);
		animator.AddAnyState(ReviveEndTrigger, reviveEnd);
		animator.AddAnyState(FreezeTrigger, freeze);
		animator.AddAnyState(CreatureAnimator.deathTrigger, reviveBegin, () => !HasRevived);
		animator.AddAnyState(CreatureAnimator.deathTrigger, die, () => HasRevived);
		return animator;
	}
}
