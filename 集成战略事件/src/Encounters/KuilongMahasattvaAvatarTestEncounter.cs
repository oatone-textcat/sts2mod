using Godot;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Encounters;

public sealed class KuilongMahasattvaAvatarTestEncounter : IntegratedStrategyBossEncounter
{
	public override bool IsDebugEncounter => true;

	public override string? CustomScenePath =>
		"res://IntegratedStrategyEvents/scenes/encounters/kuilong_mahasattva_avatar.tscn";

	public override IEnumerable<string> ExtraAssetPaths =>
	[
		KuilongMahasattvaAvatarMusicController.TrackPath
	];

	public override IEnumerable<MonsterModel> AllPossibleMonsters =>
	[
		Monster<KuilongMahasattvaAvatar>(),
		Monster<ReincarnationLotus>()
	];

	public override IReadOnlyList<string> Slots =>
	[
		KuilongMahasattvaAvatar.LotusLeftSlot,
		KuilongMahasattvaAvatar.LotusRightSlot,
		KuilongMahasattvaAvatar.BossSlot
	];

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return [(MutableMonster<KuilongMahasattvaAvatar>(), KuilongMahasattvaAvatar.BossSlot)];
	}

	public override float GetCameraScaling()
	{
		return 0.86f;
	}

	public override Vector2 GetCameraOffset()
	{
		return Vector2.Down * 55f;
	}
}
