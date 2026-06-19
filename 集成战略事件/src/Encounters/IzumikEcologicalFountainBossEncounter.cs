using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IzumikEcologicalFountainBossEncounter : IntegratedStrategyBossEncounter
{
	public const string BossNodePathBase = $"res://{ModInfo.ModId}/images/map/izumik_ecological_spring_boss_icon";

	public override string? CustomScenePath =>
		"res://IntegratedStrategyEvents/scenes/encounters/izumik_ecological_fountain.tscn";

	public override string BossNodePath => BossNodePathBase;

	public override IEnumerable<string> ExtraAssetPaths =>
	[
		BossNodePathBase + ".png",
		BossNodePathBase + "_outline.png",
		IzumikEcologicalFountainMusicController.TrackPath
	];

	public override IEnumerable<MonsterModel> AllPossibleMonsters =>
	[
		Monster<IzumikEcologicalFountain>(),
		Monster<IzumikOffspring>()
	];

	public override IReadOnlyList<string> Slots =>
	[
		IzumikEcologicalFountain.SummonLeftSlot,
		IzumikEcologicalFountain.SummonCenterSlot,
		IzumikEcologicalFountain.SummonRightSlot,
		IzumikEcologicalFountain.BossSlot
	];

	public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
	{
		_ = parentAct;
		return CreateWaterfallGiantBackground(rng);
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return [(MutableMonster<IzumikEcologicalFountain>(), IzumikEcologicalFountain.BossSlot)];
	}

	public override float GetCameraScaling()
	{
		return 0.86f;
	}

	public override Vector2 GetCameraOffset()
	{
		return Vector2.Down * 45f;
	}
}
