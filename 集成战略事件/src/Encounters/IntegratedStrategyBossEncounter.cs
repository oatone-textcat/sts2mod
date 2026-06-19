using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace IntegratedStrategyEvents.Encounters;

public abstract class IntegratedStrategyBossEncounter : CustomEncounterModel
{
	private const string WaterfallGiantBackgroundKey = "waterfall_giant_boss";

	protected IntegratedStrategyBossEncounter()
		: base(RoomType.Boss, autoAdd: false)
	{
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
