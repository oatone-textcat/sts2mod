using Godot;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntegratedStrategyEvents.Encounters;

public sealed class TheaterAssembly : MonsterModel
{
	public const string ChargeMoveId = "CHARGE_MOVE";
	public const string SmashMoveId = "SMASH_MOVE";

	private const int InitialHp = 200;
	private const decimal ChargeStrengthGain = 5m;
	private const int SmashDamage = 25;
	private const float ChargeDelay = 0.7f;
	private const float SmashHitDelay = 0.65f;
	private const string AttackSfxPath = "event:/sfx/enemy/enemy_attacks/spectral_knight/spectral_knight_soul_slash";
	private const string BuffSfxPath = "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_flame";

	public override int MinInitialHp => InitialHp;

	public override int MaxInitialHp => InitialHp;

	public override bool HasDeathSfx => false;

	public override bool HasHurtSfx => false;

	public override bool ShouldFadeAfterDeath => true;

	public override float DeathAnimLengthOverride => 1.1f;

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	protected override string VisualsPath =>
		"res://IntegratedStrategyEvents/scenes/creature_visuals/theater_assembly.tscn";

	public override Vector2 ExtraDeathVfxPadding => Vector2.One * 0.6f;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState charge = new(
			ChargeMoveId,
			ChargeMove,
			new BuffIntent());
		MoveState smash = new(
			SmashMoveId,
			SmashMove,
			new SingleAttackIntent(SmashDamage));

		charge.FollowUpState = smash;
		smash.FollowUpState = charge;

		return new MonsterMoveStateMachine([charge, smash], charge);
	}

	private async Task ChargeMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		SfxCmd.Play(BuffSfxPath);
		await Cmd.Wait(ChargeDelay);
		await PowerCmd.Apply<StrengthPower>(Creature, ChargeStrengthGain, Creature, null);
	}

	private async Task SmashMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(SmashDamage)
			.FromMonster(this)
			.WithAttackerAnim(CreatureAnimator.attackTrigger, 0f)
			.WithWaitBeforeHit(SmashHitDelay, SmashHitDelay)
			.WithHitFx("vfx/vfx_attack_blunt", AttackSfxPath)
			.Execute(null);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idle", isLooping: true);
		AnimState attack = new("Attack")
		{
			NextState = idle
		};
		AnimState die = new("Die");

		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle);
		animator.AddAnyState(CreatureAnimator.attackTrigger, attack);
		animator.AddAnyState(CreatureAnimator.deathTrigger, die);
		return animator;
	}
}
