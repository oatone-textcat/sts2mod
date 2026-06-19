using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntegratedStrategyEvents.Encounters;

public sealed class EutrophicPiercer : MonsterModel
{
	public enum OpeningMove
	{
		Pierce,
		Suck
	}

	public const string PierceMoveId = "PIERCE_MOVE";
	public const string SuckMoveId = "SUCK_MOVE";

	private const int InitialHp = 100;
	private const int PierceDamage = 18;
	private const decimal InitialSuckAmount = 2m;
	private const decimal SuckGain = 1m;
	private const float PierceHitDelay = 0.55f;
	private const float SuckAnimDelay = 0.75f;
	private const string SuckTrigger = "SuckTrigger";
	private const string PierceSfxPath = "event:/sfx/enemy/enemy_attacks/living_fog/living_fog_attack_blow";

	private OpeningMove _initialMove = OpeningMove.Pierce;

	public override int MinInitialHp => InitialHp;

	public override int MaxInitialHp => InitialHp;

	public override bool HasDeathSfx => false;

	public override bool HasHurtSfx => false;

	public override bool ShouldFadeAfterDeath => true;

	public override float DeathAnimLengthOverride => 1.2f;

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

	protected override string VisualsPath => "res://IntegratedStrategyEvents/scenes/creature_visuals/eutrophic_piercer.tscn";

	public override Vector2 ExtraDeathVfxPadding => Vector2.One * 0.55f;

	public OpeningMove InitialMove
	{
		get => _initialMove;
		set
		{
			AssertMutable();
			_initialMove = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await PowerCmd.Apply<SuckPower>(Creature, InitialSuckAmount, Creature, null, silent: true);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState pierce = new(
			PierceMoveId,
			PierceMove,
			new SingleAttackIntent(PierceDamage));
		MoveState suck = new(
			SuckMoveId,
			SuckMove,
			new BuffIntent());

		pierce.FollowUpState = suck;
		suck.FollowUpState = pierce;
		MonsterState initialState = InitialMove switch
		{
			OpeningMove.Suck => suck,
			_ => pierce
		};
		return new MonsterMoveStateMachine([pierce, suck], initialState);
	}

	private async Task PierceMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(PierceDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0f)
			.WithWaitBeforeHit(PierceHitDelay, PierceHitDelay)
			.WithHitFx("vfx/vfx_attack_slash", PierceSfxPath)
			.Execute(null);
	}

	private async Task SuckMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await MonsterAnimationHelper.TriggerAnimWithFixedWait(Creature, SuckTrigger, SuckAnimDelay);
		await PowerCmd.Apply<SuckPower>(Creature, SuckGain, Creature, null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idle", isLooping: true);
		AnimState pierce = new("Attack")
		{
			NextState = idle
		};
		AnimState suck = new("Attack_Down")
		{
			NextState = idle
		};
		AnimState die = new("Die");

		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(CreatureAnimator.attackTrigger, pierce);
		animator.AddAnyState(SuckTrigger, suck);
		animator.AddAnyState(CreatureAnimator.deathTrigger, die);
		return animator;
	}
}
