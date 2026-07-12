using Godot;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents.Encounters;

public sealed class ReincarnationLotusDuoEncounter : IntegratedStrategyEliteEncounter
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters => [Monster<ReincarnationLotus>()];

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return
		[
			(MutableMonster<ReincarnationLotus>(), null),
			(MutableMonster<ReincarnationLotus>(), null)
		];
	}

	public override float GetCameraScaling()
	{
		return 0.92f;
	}

	public override Vector2 GetCameraOffset()
	{
		return Vector2.Down * 20f;
	}
}
