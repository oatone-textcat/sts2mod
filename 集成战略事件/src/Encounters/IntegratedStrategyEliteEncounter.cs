using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content;

namespace IntegratedStrategyEvents.Encounters;

public abstract class IntegratedStrategyEliteEncounter : ModEncounterTemplate
{
	public override RoomType RoomType => RoomType.Elite;

	// 与 BaseLib 时代同名的自定义战斗场景入口，转接 RitsuLib 的 CustomEncounterScenePath。
	public virtual string? CustomScenePath => null;

	public override string? CustomEncounterScenePath => CustomScenePath;

	public override bool IsValidForAct(ActModel act)
	{
		return false;
	}

	protected static MonsterModel Monster<TMonster>()
		where TMonster : MonsterModel
	{
		return ModelDb.Monster<TMonster>();
	}

	protected static TMonster MutableMonster<TMonster>()
		where TMonster : MonsterModel
	{
		return (TMonster)ModelDb.Monster<TMonster>().ToMutable();
	}
}
