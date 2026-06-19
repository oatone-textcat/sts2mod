using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace IntegratedStrategyEvents.Encounters;

public sealed class IsharmlaCorruptedHeartBossEncounter : IntegratedStrategyBossEncounter
{
	public const string BossNodePathBase = $"res://{ModInfo.ModId}/images/map/skadi_seaborn_boss_icon";
	private const string SuckPiercerSlot = "isharmla_piercer_suck";
	private const string PiercePiercerSlot = "isharmla_piercer_pierce";

	public override string? CustomScenePath =>
		"res://IntegratedStrategyEvents/scenes/encounters/isharmla_corrupted_heart.tscn";

	public override string BossNodePath => BossNodePathBase;

	public override IEnumerable<string> ExtraAssetPaths =>
	[
		BossNodePathBase + ".png",
		BossNodePathBase + "_outline.png"
	];

	public override IEnumerable<MonsterModel> AllPossibleMonsters =>
	[
		Monster<EutrophicPiercer>(),
		Monster<IsharmlaCorruptedHeart>()
	];

	public override IReadOnlyList<string> Slots =>
	[
		SuckPiercerSlot,
		PiercePiercerSlot,
		IsharmlaCorruptedHeart.BossSlot
	];

	public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
	{
		_ = parentAct;
		return CreateWaterfallGiantBackground(rng);
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return
		[
			(CreatePiercer(EutrophicPiercer.OpeningMove.Suck), SuckPiercerSlot),
			(CreatePiercer(EutrophicPiercer.OpeningMove.Pierce), PiercePiercerSlot),
			(MutableMonster<IsharmlaCorruptedHeart>(), IsharmlaCorruptedHeart.BossSlot)
		];
	}

	private static EutrophicPiercer CreatePiercer(EutrophicPiercer.OpeningMove openingMove)
	{
		EutrophicPiercer piercer = (EutrophicPiercer)Monster<EutrophicPiercer>().ToMutable();
		piercer.InitialMove = openingMove;
		return piercer;
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
