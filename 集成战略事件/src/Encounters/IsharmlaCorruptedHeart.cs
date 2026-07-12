using Godot;
using IntegratedStrategyEvents.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IsharmlaCorruptedHeart : MonsterModel
{
	public const string BossSlot = "isharmla";

	public const string ReviveMoveId = "REVIVE_MOVE";
	public const string LightlessBurialWishMoveId = "LIGHTLESS_BURIAL_WISH_MOVE";
	public const string TideSurgeMoveId = "TIDE_SURGE_MOVE";
	public const string LamentMoveId = "LAMENT_MOVE";
	public const string TideEbbMoveId = "TIDE_EBB_MOVE";
	public const string BiteMoveId = "BITE_MOVE";
	public const string StruggleMoveId = "STRUGGLE_MOVE";
	public const string RoarMoveId = "ROAR_MOVE";

	private const int PhaseOneHp = 300;
	private const int PhaseTwoHp = 700;
	private const decimal LightlessPowerAmount = 1m;
	private const decimal TideSurgeHeal = 30m;
	private const decimal LamentStatLoss = 1m;
	private const decimal TideEbbDisintegrationAmount = 6m;
	private const int BiteDamage = 10;
	private const int BiteHits = 3;
	private const int StruggleDamage = 40;
	private const decimal RoarBlock = 30m;
	private const decimal RoarRitual = 2m;

	private const float LightlessDelay = 0.95f;
	private const float PhaseOneDebuffDelay = 0.75f;
	private const float ReviveDelay = 1.75f;
	private const float BiteHitDelay = 0.55f;
	private const float StruggleHitDelay = 0.7f;
	private const float RoarDelay = 0.8f;

	private const string LightlessTrigger = "LightlessTrigger";
	private const string PhaseOneAttackTrigger = "PhaseOneAttackTrigger";
	private const string ReviveTrigger = "ReviveTrigger";
	private const string PhaseTwoAttackTrigger = "PhaseTwoAttackTrigger";
	private const string LightlessSfxPath = "event:/sfx/enemy/enemy_attacks/spectral_knight/spectral_knight_hex";
	private const string DebuffSfxPath = "event:/sfx/enemy/enemy_attacks/ceremonial_beast/ceremonial_beast_shrill";
	private const string AttackSfxPath = "event:/sfx/enemy/enemy_attacks/soul_fysh/soul_fysh_bite";
	private const string HeavyAttackSfxPath = "event:/sfx/enemy/enemy_attacks/soul_fysh/soul_fysh_wave";

	private MoveState? _reviveState;
	private bool _hasRevived;
	private bool _isReviving;
	private bool _secondPhase;

	public override int MinInitialHp => PhaseOneHp;

	public override int MaxInitialHp => PhaseOneHp;

	public override bool HasDeathSfx => false;

	public override bool HasHurtSfx => false;

	public override bool ShouldFadeAfterDeath => true;

	public override bool ShouldDisappearFromDoom => HasRevived;

	public override float DeathAnimLengthOverride => 1.2f;

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

	protected override string VisualsPath =>
		"res://IntegratedStrategyEvents/scenes/creature_visuals/isharmla_corrupted_heart.tscn";

	public override Vector2 ExtraDeathVfxPadding => Vector2.One * 0.8f;

	public bool HasRevived => _hasRevived;

	private bool IsSecondPhase => _secondPhase;

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await PowerCmd.Apply<SwarmAvatarPower>(Creature, 1m, Creature, null, silent: true);
	}

	public override async Task AfterDeath(
		PlayerChoiceContext choiceContext,
		Creature creature,
		bool wasRemovalPrevented,
		float deathAnimLength)
	{
		_ = choiceContext;
		_ = deathAnimLength;
		if (wasRemovalPrevented || creature != Creature || HasRevived || _isReviving)
		{
			return;
		}

		_isReviving = true;
		await TriggerReviveWaitingState();
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		return creature != Creature || !_isReviving;
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return Creature is { IsDead: true } && (_isReviving || !HasRevived);
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		return creature != Creature || HasRevived;
	}

	private Task TriggerReviveWaitingState()
	{
		if (HasRevived || _reviveState == null)
		{
			return Task.CompletedTask;
		}

		SetMoveImmediate(_reviveState, forceTransition: true);
		return Task.CompletedTask;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState revive = new(
			ReviveMoveId,
			ReviveMove,
			new HealIntent(),
			new BuffIntent())
		{
			MustPerformOnceBeforeTransitioning = true
		};
		MoveState lightlessBurialWish = new(
			LightlessBurialWishMoveId,
			LightlessBurialWishMove,
			new DebuffIntent());
		MoveState tideSurge = new(
			TideSurgeMoveId,
			TideSurgeMove,
			new HealIntent());
		MoveState lament = new(
			LamentMoveId,
			LamentMove,
			new DebuffIntent());
		MoveState tideEbb = new(
			TideEbbMoveId,
			TideEbbMove,
			new DebuffIntent());
		MoveState bite = new(
			BiteMoveId,
			BiteMove,
			new MultiAttackIntent(BiteDamage, BiteHits));
		MoveState struggle = new(
			StruggleMoveId,
			StruggleMove,
			new SingleAttackIntent(StruggleDamage));
		MoveState roar = new(
			RoarMoveId,
			RoarMove,
			new DefendIntent(),
			new BuffIntent());

		_reviveState = revive;

		lightlessBurialWish.FollowUpState = tideSurge;
		tideSurge.FollowUpState = lament;
		lament.FollowUpState = tideEbb;
		tideEbb.FollowUpState = tideSurge;

		revive.FollowUpState = bite;
		bite.FollowUpState = struggle;
		struggle.FollowUpState = roar;
		roar.FollowUpState = bite;

		return new MonsterMoveStateMachine(
			[revive, lightlessBurialWish, tideSurge, lament, tideEbb, bite, struggle, roar],
			lightlessBurialWish);
	}

	private async Task ReviveMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		_secondPhase = true;
		await RemoveLightlessDebuffsFromPlayers();
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, ReviveTrigger, ReviveDelay);
		await CreatureCmd.SetMaxAndCurrentHp(Creature, GetScaledPhaseTwoHp());
		_hasRevived = true;
		_isReviving = false;
	}

	private async Task LightlessBurialWishMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, LightlessTrigger, LightlessDelay);
		SfxCmd.Play(LightlessSfxPath);

		foreach (Creature target in LivingPlayerCreatures())
		{
			await ApplyDampen(target);
			await PowerCmd.Apply<HexPower>(target, LightlessPowerAmount, Creature, null);
		}
	}

	private async Task TideSurgeMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await PlayPhaseOneAttackDebuffAnimation();
		decimal healAmount = GetScaledTideSurgeHeal();
		foreach (Creature target in LivingEnemyCreatures())
		{
			await CreatureCmd.Heal(target, healAmount);
		}
	}

	private async Task LamentMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await PlayPhaseOneAttackDebuffAnimation();
		IReadOnlyList<Creature> players = LivingPlayerCreatures();
		await PowerCmd.Apply<StrengthPower>(players, -LamentStatLoss, Creature, null);
		await PowerCmd.Apply<DexterityPower>(players, -LamentStatLoss, Creature, null);
	}

	private async Task TideEbbMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await PlayPhaseOneAttackDebuffAnimation();
		await PowerCmd.Apply<DisintegrationPower>(LivingPlayerCreatures(), TideEbbDisintegrationAmount, Creature, null);
	}

	private async Task BiteMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(BiteDamage)
			.FromMonster(this)
			.WithHitCount(BiteHits)
			.WithAttackerAnim(PhaseTwoAttackTrigger, 0f)
			.WithWaitBeforeHit(BiteHitDelay, BiteHitDelay)
			.WithHitFx("vfx/vfx_attack_slash", AttackSfxPath)
			.OnlyPlayAnimOnce()
			.Execute(null);
	}

	private async Task StruggleMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(StruggleDamage)
			.FromMonster(this)
			.WithAttackerAnim(PhaseTwoAttackTrigger, 0f)
			.WithWaitBeforeHit(StruggleHitDelay, StruggleHitDelay)
			.WithHitFx("vfx/vfx_attack_blunt", HeavyAttackSfxPath)
			.Execute(null);
	}

	private async Task RoarMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, PhaseTwoAttackTrigger, RoarDelay);
		await CreatureCmd.GainBlock(Creature, RoarBlock, ValueProp.Move, null);
		await PowerCmd.Apply<RitualPower>(Creature, RoarRitual, Creature, null);
	}

	private async Task PlayPhaseOneAttackDebuffAnimation()
	{
		SfxCmd.Play(DebuffSfxPath);
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, PhaseOneAttackTrigger, PhaseOneDebuffDelay);
	}

	private async Task ApplyDampen(Creature target)
	{
		DampenPower? dampenPower = target.GetPower<DampenPower>();
		bool shouldApply = dampenPower == null;
		if (shouldApply)
		{
			dampenPower = (DampenPower)ModelDb.Power<DampenPower>().ToMutable();
		}

		dampenPower!.AddCaster(Creature);
		if (shouldApply)
		{
			await PowerCmd.Apply(dampenPower, target, LightlessPowerAmount, Creature, null);
		}
	}

	private async Task RemoveLightlessDebuffsFromPlayers()
	{
		foreach (Creature player in CombatState.PlayerCreatures)
		{
			PowerModel[] powers = player.Powers
				.Where(static power => power is DampenPower or HexPower)
				.ToArray();
			foreach (PowerModel power in powers)
			{
				await PowerCmd.Remove(power);
			}
		}
	}

	private IReadOnlyList<Creature> LivingPlayerCreatures()
	{
		return CombatState.PlayerCreatures
			.Where(static creature => creature.IsAlive && creature.IsPlayer)
			.ToList();
	}

	private IReadOnlyList<Creature> LivingEnemyCreatures()
	{
		return CombatState.Enemies
			.Where(static creature => creature.IsAlive)
			.ToList();
	}

	private decimal GetScaledTideSurgeHeal()
	{
		if (CombatState.RunState != null)
		{
			return Creature.ScaleHpForMultiplayer(
				TideSurgeHeal,
				CombatState.Encounter,
				CombatState.RunState.Players.Count,
				Math.Clamp(CombatState.RunState.CurrentActIndex, 0, 2));
		}

		return TideSurgeHeal;
	}

	private decimal GetScaledPhaseTwoHp()
	{
		if (CombatState.RunState != null)
		{
			return Creature.ScaleHpForMultiplayer(
				PhaseTwoHp,
				CombatState.Encounter,
				CombatState.RunState.Players.Count,
				Math.Clamp(CombatState.RunState.CurrentActIndex, 0, 2));
		}

		decimal phaseOneScale = PhaseOneHp == 0 ? 1m : Creature.MaxHp / (decimal)PhaseOneHp;
		return Math.Ceiling(PhaseTwoHp * phaseOneScale);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState phaseOneIdle = new("Idle", isLooping: true);
		AnimState phaseTwoIdle = new("Idle_02", isLooping: true)
		{
			BoundsContainer = "PhaseTwoBounds"
		};
		AnimState lightless = new("Skill")
		{
			NextState = phaseOneIdle
		};
		AnimState phaseOneAttack = new("Attack")
		{
			NextState = phaseOneIdle
		};
		AnimState phaseOneDeathHold = new("Idle", isLooping: true);
		AnimState phaseOneDie = new("Die")
		{
			NextState = phaseOneDeathHold
		};
		AnimState reviveStart = new("Start")
		{
			NextState = phaseTwoIdle,
			BoundsContainer = "PhaseTwoBounds"
		};
		AnimState phaseTwoAttack = new("Attack_02")
		{
			NextState = phaseTwoIdle,
			BoundsContainer = "PhaseTwoBounds"
		};
		AnimState phaseTwoDie = new("Die_02")
		{
			BoundsContainer = "PhaseTwoBounds"
		};

		CreatureAnimator animator = new(phaseOneIdle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, phaseOneIdle, () => !IsSecondPhase);
		animator.AddAnyState(CreatureAnimator.idleTrigger, phaseTwoIdle, () => IsSecondPhase);
		animator.AddAnyState(LightlessTrigger, lightless);
		animator.AddAnyState(PhaseOneAttackTrigger, phaseOneAttack);
		animator.AddAnyState(ReviveTrigger, reviveStart);
		animator.AddAnyState(PhaseTwoAttackTrigger, phaseTwoAttack);
		animator.AddAnyState(CreatureAnimator.deathTrigger, phaseOneDie, () => !HasRevived);
		animator.AddAnyState(CreatureAnimator.deathTrigger, phaseTwoDie, () => HasRevived);
		return animator;
	}
}
