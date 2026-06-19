using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IntegratedStrategyEvents.Powers;

public sealed class IsharmlaRingingHexRestorePower : PowerModel, ICustomPower
{
	private sealed class Data
	{
		public bool HasRestored { get; set; }
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override bool IsVisibleInternal => false;

	public override bool ShouldPlayVfx => false;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public async Task ReplaceHexedCardsWithRinging()
	{
		var player = Owner?.Player;
		if (player == null || !ShouldUseRingingInsteadOfHex())
		{
			return;
		}

		var playerCombatState = player.PlayerCombatState;
		if (playerCombatState == null)
		{
			return;
		}

		foreach (CardModel card in playerCombatState.AllCards)
		{
			await ReplaceHexedCardWithRinging(card);
		}
	}

	public override async Task AfterCardEnteredCombat(CardModel card)
	{
		if (ShouldUseRingingInsteadOfHex() && card.Owner == Owner.Player)
		{
			await ReplaceHexedCardWithRinging(card);
		}
	}

	public override async Task AfterSideTurnEndLate(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		_ = choiceContext;
		if (Owner == null || side != Owner.Side || !participants.Contains(Owner))
		{
			return;
		}

		await RestoreHexedCards();
		await PowerCmd.Remove(this);
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		await RestoreHexedCards(oldOwner);
	}

	private bool ShouldUseRingingInsteadOfHex()
	{
		return Owner is { IsPlayer: true }
			&& Owner.HasPower<HexPower>()
			&& Owner.HasPower<RingingPower>();
	}

	private async Task ReplaceHexedCardWithRinging(CardModel card)
	{
		if (card.Affliction is Ringing)
		{
			return;
		}

		if (card.Affliction is Hexed hexed)
		{
			_ = hexed;
			CardCmd.ClearAffliction(card);
			await CardCmd.Afflict<Ringing>(card, Amount);
		}
		else if (card.Affliction == null)
		{
			await CardCmd.Afflict<Ringing>(card, Amount);
		}
	}

	private async Task RestoreHexedCards(Creature? owner = null)
	{
		Creature? target = owner ?? Owner;
		if (target is not { IsPlayer: true } || !target.HasPower<HexPower>())
		{
			return;
		}

		Data data = GetInternalData<Data>();
		if (data.HasRestored)
		{
			return;
		}

		data.HasRestored = true;
		var player = target.Player;
		if (player == null)
		{
			return;
		}

		var playerCombatState = player.PlayerCombatState;
		if (playerCombatState == null)
		{
			return;
		}

		foreach (CardModel card in playerCombatState.AllCards)
		{
			if (card.Affliction is Ringing)
			{
				CardCmd.ClearAffliction(card);
			}

			if (card.Affliction != null)
			{
				continue;
			}

			await CardCmd.Afflict<Hexed>(card, 1m);
		}
	}
}
