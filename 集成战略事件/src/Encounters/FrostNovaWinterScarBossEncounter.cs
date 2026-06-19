using Godot;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Encounters;

public sealed class FrostNovaWinterScarBossEncounter : IntegratedStrategyBossEncounter
{
	public const string BossNodePathBase = $"res://{ModInfo.ModId}/images/map/frost_nova_winter_scar_boss_icon";

	public override string? CustomScenePath =>
		"res://IntegratedStrategyEvents/scenes/encounters/frost_nova_winter_scar.tscn";

	public override string BossNodePath => BossNodePathBase;

	public override IEnumerable<string> ExtraAssetPaths =>
	[
		BossNodePathBase + ".png",
		BossNodePathBase + "_outline.png",
		FrostNovaWinterScarMusicController.TrackPath
	];

	public override IEnumerable<MonsterModel> AllPossibleMonsters =>
	[
		Monster<FrostNovaWinterScar>()
	];

	public override IReadOnlyList<string> Slots =>
	[
		FrostNovaWinterScar.BossSlot
	];

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return [(MutableMonster<FrostNovaWinterScar>(), FrostNovaWinterScar.BossSlot)];
	}

	public override float GetCameraScaling()
	{
		return 0.9f;
	}

	public override Vector2 GetCameraOffset()
	{
		return Vector2.Down * 35f;
	}
}
