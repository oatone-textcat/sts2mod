using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Rewards;

namespace IntegratedStrategyEvents.Events;

public abstract partial class IntegratedStrategyEventModel
{
	protected EventOption Choice(Func<Task> onChosen, string optionKey, string pageKey = InitialPage)
	{
		return new EventOption(this, onChosen, $"{Id.Entry}.pages.{pageKey}.options.{optionKey}");
	}

	protected EventOption LockedChoice(string optionKey, string pageKey = InitialPage)
	{
		// 等价于 BaseLib CustomEventModel.LockedOption：onChosen 为 null 即锁定选项。
		return new EventOption(this, null, $"{Id.Entry}.pages.{pageKey}.options.{optionKey}");
	}

	protected EventOption GoldChoice(
		Player owner,
		int cost,
		Func<Task> onChosen,
		string optionKey,
		string lockedOptionKey,
		string pageKey = InitialPage)
	{
		return owner.Gold >= cost && (!IsShared || AllPlayersHaveGold(cost))
			? Choice(onChosen, optionKey, pageKey)
			: LockedChoice(lockedOptionKey, pageKey);
	}

	protected EventOption HpChoice(
		Player owner,
		int hpLoss,
		Func<Task> onChosen,
		string optionKey,
		string lockedOptionKey,
		string pageKey = InitialPage)
	{
		return CanLoseHp(owner, hpLoss) && (!IsShared || AllPlayersCanLoseHp(hpLoss))
			? Choice(onChosen, optionKey, pageKey).ThatDoesDamage(hpLoss)
			: LockedChoice(lockedOptionKey, pageKey);
	}

	protected bool AllPlayersHaveGold(int cost)
	{
		return OwnerOrThrow.RunState.Players.All(player => player.Gold >= cost);
	}

	protected void ShowPage(string pageKey, IReadOnlyList<EventOption> options)
	{
		SetEventState(PageDescription(pageKey), options);
	}

	protected EventOption FightChoice(Func<Task> startFight, string pageKey)
	{
		return Choice(startFight, "FIGHT", pageKey);
	}

	protected EventOption FightChoice<TEncounter>(string pageKey)
		where TEncounter : EncounterModel
	{
		return FightChoice(EnterEventCombat<TEncounter>, pageKey);
	}

	protected void ShowFightPage(string pageKey, Func<Task> startFight)
	{
		ShowPage(pageKey, [FightChoice(startFight, pageKey)]);
	}

	protected void ShowFightPage<TEncounter>(string pageKey)
		where TEncounter : EncounterModel
	{
		ShowFightPage(pageKey, EnterEventCombat<TEncounter>);
	}

	protected Task EnterEventCombat<TEncounter>()
		where TEncounter : EncounterModel
	{
		return EnterEventCombat<TEncounter>(Array.Empty<Reward>());
	}

	protected Task EnterEventCombat<TEncounter>(IReadOnlyList<Reward> rewards)
		where TEncounter : EncounterModel
	{
		EnterCombatWithoutExitingEvent<TEncounter>(
			rewards,
			shouldResumeAfterCombat: false);
		return Task.CompletedTask;
	}

	protected void Finish(string pageKey)
	{
		SetEventFinished(PageDescription(pageKey));
	}
}
