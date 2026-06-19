using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IntegratedStrategyEvents;

internal static class PowerCmd
{
	public static Task<T?> Apply<T>(
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			new ThrowingPlayerChoiceContext(),
			target,
			amount,
			applier,
			cardSource,
			silent);
	}

	public static Task<IReadOnlyList<T>> Apply<T>(
		IEnumerable<Creature> targets,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
		where T : PowerModel
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(
			new ThrowingPlayerChoiceContext(),
			targets,
			amount,
			applier,
			cardSource,
			silent);
	}

	public static Task Apply(
		PowerModel power,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply(
			new ThrowingPlayerChoiceContext(),
			power,
			target,
			amount,
			applier,
			cardSource,
			silent);
	}

	public static Task<int> ModifyAmount(
		PowerModel power,
		decimal offset,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.ModifyAmount(
			new ThrowingPlayerChoiceContext(),
			power,
			offset,
			applier,
			cardSource,
			silent);
	}

	public static Task Decrement(PowerModel power)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Decrement(power);
	}

	public static Task Remove(PowerModel power)
	{
		return MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(power);
	}

	public static Task Remove<T>(Creature creature)
		where T : PowerModel
	{
		T? power = creature.GetPower<T>();
		return power == null
			? Task.CompletedTask
			: MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(power);
	}
}
