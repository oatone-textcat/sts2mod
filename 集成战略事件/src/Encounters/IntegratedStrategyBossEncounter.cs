using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content;

namespace IntegratedStrategyEvents.Encounters;

public abstract class IntegratedStrategyBossEncounter : ModEncounterTemplate
{
	private const string WaterfallGiantBackgroundKey = "waterfall_giant_boss";

	public override RoomType RoomType => RoomType.Boss;

	// 与 BaseLib 时代同名的自定义战斗场景入口，转接 RitsuLib 的 CustomEncounterScenePath。
	public virtual string? CustomScenePath => null;

	public override string? CustomEncounterScenePath => CustomScenePath;

	// 与 BaseLib CustomEncounterModel.CustomEncounterBackground 同语义的程序化背景入口。
	// 需要程序化背景的遭遇战覆写 HasProgrammaticBackground => true。
	protected virtual bool HasProgrammaticBackground => false;

	protected override bool UseProgrammaticCombatBackground => HasProgrammaticBackground;

	protected override bool UseActCombatBackground => !HasProgrammaticBackground;

	public virtual BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
	{
		return null;
	}

	protected sealed override BackgroundAssets? BuildProgrammaticCombatBackground(ActModel parentAct, Rng rng)
	{
		return CustomEncounterBackground(parentAct, rng);
	}

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

	protected static BackgroundAssets CreateWaterfallGiantBackground(Rng rng)
	{
		return new BackgroundAssets(WaterfallGiantBackgroundKey, rng);
	}
}
