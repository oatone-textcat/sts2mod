using Godot;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Encounters;

public sealed class KuilongMahasattvaAvatarBossEncounter : IntegratedStrategyBossEncounter
{
	public const string BossNodePathBase = $"res://{ModInfo.ModId}/images/map/seated_mahasattva_boss_icon";

	public override string? CustomScenePath =>
		"res://IntegratedStrategyEvents/scenes/encounters/kuilong_mahasattva_avatar.tscn";

	public override string BossNodePath => BossNodePathBase;

	public override IEnumerable<string> ExtraAssetPaths =>
	[
		BossNodePathBase + ".png",
		BossNodePathBase + "_outline.png",
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
