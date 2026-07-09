using Godot;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Encounters;

public sealed class SorrowfulLockBossEncounter : IntegratedStrategyBossEncounter
{
	public const string BossNodePathBase = $"res://{ModInfo.ModId}/images/map/mechanical_bear_boss_icon";

	public override string BossNodePath => BossNodePathBase;

	public override IEnumerable<string> ExtraAssetPaths =>
	[
		BossNodePathBase + ".png",
		BossNodePathBase + "_outline.png"
	];

	public override string? CustomScenePath =>
		"res://IntegratedStrategyEvents/scenes/encounters/sorrowful_lock.tscn";

	public override IEnumerable<MonsterModel> AllPossibleMonsters =>
	[
		Monster<SorrowfulLock>(),
		Monster<TheaterAssembly>()
	];

	public override IReadOnlyList<string> Slots =>
	[
		SorrowfulLock.AssemblyLeftSlot,
		SorrowfulLock.AssemblyRightSlot,
		SorrowfulLock.BossSlot
	];

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return [(MutableMonster<SorrowfulLock>(), SorrowfulLock.BossSlot)];
	}

	public override float GetCameraScaling()
	{
		return 0.78f;
	}

	public override Vector2 GetCameraOffset()
	{
		return Vector2.Down * 35f;
	}
}
