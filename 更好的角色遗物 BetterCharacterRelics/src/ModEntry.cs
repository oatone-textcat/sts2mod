using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace BetterCharacterRelics;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	private const string HarmonyId = "Natsuki.BetterCharacterRelics";

	private static Harmony? _harmony;
	private static bool _hooksInstalled;

	public static void Initialize()
	{
		Harmony harmony = _harmony ??= new Harmony(HarmonyId);
		InstallHooks(harmony);
		Log.Info("[BetterCharacterRelics] Loaded for Slay the Spire 2 0.107.1.");
	}

	private static void InstallHooks(Harmony harmony)
	{
		if (_hooksInstalled)
		{
			return;
		}

		harmony.Patch(
			RequireMethod(typeof(BurningBlood), nameof(BurningBlood.AfterCombatVictory), BindingFlags.Instance | BindingFlags.Public, typeof(CombatRoom)),
			prefix: new HarmonyMethod(typeof(ModEntry), nameof(BurningBloodAfterCombatVictoryPrefix)));
		harmony.Patch(
			RequireMethod(typeof(BlackBlood), nameof(BlackBlood.AfterCombatVictory), BindingFlags.Instance | BindingFlags.Public, typeof(CombatRoom)),
			prefix: new HarmonyMethod(typeof(ModEntry), nameof(BlackBloodAfterCombatVictoryPrefix)));
		harmony.Patch(
			RequireMethod(typeof(AbstractModel), nameof(AbstractModel.AfterEnergyResetLate), BindingFlags.Instance | BindingFlags.Public, typeof(Player)),
			postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterEnergyResetLatePostfix)));
		harmony.Patch(
			RequireMethod(typeof(AbstractModel), nameof(AbstractModel.AfterRoomEntered), BindingFlags.Instance | BindingFlags.Public, typeof(AbstractRoom)),
			postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterRoomEnteredPostfix)));
		harmony.Patch(
			RequireMethod(typeof(AbstractModel), nameof(AbstractModel.AfterPlayerTurnStart), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(Player)),
			postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterPlayerTurnStartPostfix)));
		TryPatchMethod(
			harmony,
			typeof(RingOfTheSnake),
			nameof(RingOfTheSnake.ModifyHandDraw),
			BindingFlags.Instance | BindingFlags.Public,
			[typeof(Player), typeof(decimal)],
			prefixName: nameof(RingOfTheSnakeModifyHandDrawPrefix));
		TryPatchMethod(
			harmony,
			typeof(RingOfTheDrake),
			nameof(RingOfTheDrake.ModifyHandDraw),
			BindingFlags.Instance | BindingFlags.Public,
			[typeof(Player), typeof(decimal)],
			prefixName: nameof(RingOfTheDrakeModifyHandDrawPrefix));
		TryPatchGetter(harmony, typeof(RingOfTheSnake), "CanonicalVars", nameof(RingOfTheSnakeCanonicalVarsPostfix));
		TryPatchGetter(harmony, typeof(RingOfTheDrake), "CanonicalVars", nameof(RingOfTheDrakeCanonicalVarsPostfix));
		TryPatchGetter(harmony, typeof(DivineRight), "CanonicalVars", nameof(DivineRightCanonicalVarsPostfix));
		TryPatchGetter(harmony, typeof(DivineDestiny), "CanonicalVars", nameof(DivineDestinyCanonicalVarsPostfix));
		harmony.Patch(
			RequireMethod(typeof(DivineDestiny), nameof(DivineDestiny.AfterSideTurnStart), BindingFlags.Instance | BindingFlags.Public, typeof(CombatSide), typeof(IReadOnlyList<Creature>), typeof(ICombatState)),
			prefix: new HarmonyMethod(typeof(ModEntry), nameof(DivineDestinyAfterSideTurnStartPrefix)));
		harmony.Patch(
			RequireMethod(typeof(BoundPhylactery), nameof(BoundPhylactery.BeforeCombatStart), BindingFlags.Instance | BindingFlags.Public),
			prefix: new HarmonyMethod(typeof(ModEntry), nameof(BoundPhylacteryBeforeCombatStartPrefix)));
		harmony.Patch(
			RequireMethod(typeof(BoundPhylactery), nameof(BoundPhylactery.AfterEnergyResetLate), BindingFlags.Instance | BindingFlags.Public, typeof(Player)),
			prefix: new HarmonyMethod(typeof(ModEntry), nameof(BoundPhylacteryAfterEnergyResetLatePrefix)));
		harmony.Patch(
			RequireMethod(typeof(PhylacteryUnbound), nameof(PhylacteryUnbound.AfterSideTurnStart), BindingFlags.Instance | BindingFlags.Public, typeof(CombatSide), typeof(IReadOnlyList<Creature>), typeof(ICombatState)),
			prefix: new HarmonyMethod(typeof(ModEntry), nameof(PhylacteryUnboundAfterSideTurnStartPrefix)));
		harmony.Patch(
			RequireMethod(typeof(CrackedCore), nameof(CrackedCore.BeforeSideTurnStart), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(CombatSide), typeof(IReadOnlyList<Creature>), typeof(ICombatState)),
			postfix: new HarmonyMethod(typeof(ModEntry), nameof(CrackedCoreBeforeSideTurnStartPostfix)));
		harmony.Patch(
			RequireMethod(typeof(InfusedCore), nameof(InfusedCore.AfterSideTurnStart), BindingFlags.Instance | BindingFlags.Public, typeof(CombatSide), typeof(IReadOnlyList<Creature>), typeof(ICombatState)),
			postfix: new HarmonyMethod(typeof(ModEntry), nameof(InfusedCoreAfterSideTurnStartPostfix)));
		_hooksInstalled = true;
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		MethodInfo? method = type.GetMethod(name, flags, binder: null, parameters, modifiers: null);
		if (method == null)
		{
			throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
		}

		return method;
	}

	private static void TryPatchMethod(Harmony harmony, Type type, string name, BindingFlags flags, Type[] parameters, string? prefixName = null, string? postfixName = null)
	{
		MethodInfo? method = type.GetMethod(name, flags, binder: null, parameters, modifiers: null);
		if (method == null)
		{
			Log.Warn($"[BetterCharacterRelics] Optional method hook skipped: {type.FullName}.{name} not found.");
			return;
		}

		harmony.Patch(method, GetHarmonyMethod(prefixName), GetHarmonyMethod(postfixName));
	}

	private static void TryPatchGetter(Harmony harmony, Type type, string propertyName, string postfixName)
	{
		MethodInfo? getter = TryFindGetter(type, propertyName);
		if (getter == null)
		{
			Log.Warn($"[BetterCharacterRelics] Optional getter hook skipped: {type.FullName}.{propertyName} not found.");
			return;
		}

		harmony.Patch(getter, postfix: new HarmonyMethod(typeof(ModEntry), postfixName));
	}

	private static MethodInfo? TryFindGetter(Type type, string propertyName)
	{
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		for (Type? current = type; current != null; current = current.BaseType)
		{
			MethodInfo? getter = current.GetProperty(propertyName, flags)?.GetMethod
				?? current.GetMethod("get_" + propertyName, flags);
			if (getter != null)
			{
				return getter;
			}
		}

		return AccessTools.PropertyGetter(type, propertyName)
			?? AccessTools.Method(type, "get_" + propertyName);
	}

	private static HarmonyMethod? GetHarmonyMethod(string? methodName)
	{
		return methodName == null ? null : new HarmonyMethod(typeof(ModEntry), methodName);
	}

	private static bool BurningBloodAfterCombatVictoryPrefix(BurningBlood __instance, ref Task __result)
	{
		__result = BurningBloodAfterCombatVictoryReplacement(__instance);
		return false;
	}

	private static async Task BurningBloodAfterCombatVictoryReplacement(BurningBlood self)
	{
		if (self.Owner == null || self.Owner.Creature.IsDead)
		{
			return;
		}

		Flash(self);
		await CreatureCmd.Heal(self.Owner.Creature, IsAtOrBelowHalfHp(self.Owner.Creature) ? 9m : 6m);
	}

	private static bool BlackBloodAfterCombatVictoryPrefix(BlackBlood __instance, ref Task __result)
	{
		__result = BlackBloodAfterCombatVictoryReplacement(__instance);
		return false;
	}

	private static async Task BlackBloodAfterCombatVictoryReplacement(BlackBlood self)
	{
		if (self.Owner == null || self.Owner.Creature.IsDead)
		{
			return;
		}

		Flash(self);
		await CreatureCmd.Heal(self.Owner.Creature, IsAtOrBelowHalfHp(self.Owner.Creature) ? 18m : 12m);
	}

	private static void RingOfTheSnakeCanonicalVarsPostfix(ref IEnumerable<DynamicVar> __result)
	{
		__result =
		[
			new CardsVar(3)
		];
	}

	private static bool RingOfTheSnakeModifyHandDrawPrefix(RingOfTheSnake __instance, Player player, decimal count, ref decimal __result)
	{
		if (player != __instance.Owner || player.Creature.CombatState == null || player.Creature.CombatState.RoundNumber > 1)
		{
			__result = count;
			return false;
		}

		__result = count + 3m;
		return false;
	}

	private static bool RingOfTheDrakeModifyHandDrawPrefix(RingOfTheDrake __instance, Player player, decimal count, ref decimal __result)
	{
		if (player != __instance.Owner || player.Creature.CombatState == null || player.Creature.CombatState.RoundNumber > 3)
		{
			__result = count;
			return false;
		}

		__result = count + 3m;
		return false;
	}

	private static void RingOfTheDrakeCanonicalVarsPostfix(ref IEnumerable<DynamicVar> __result)
	{
		__result =
		[
			new CardsVar(3),
			new DynamicVar("Turns", 3m)
		];
	}

	private static void AfterEnergyResetLatePostfix(AbstractModel __instance, Player player, ref Task __result)
	{
		__result = AfterEnergyResetLateAfterOriginal(__result, __instance, player);
	}

	private static async Task AfterEnergyResetLateAfterOriginal(Task original, AbstractModel self, Player player)
	{
		await original;

		ICombatState? combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}

		switch (self)
		{
			case DivineRight divineRight when player == divineRight.Owner && player.PlayerCombatState is { Stars: < 3 }:
				Flash(divineRight);
				await PlayerCmd.GainStars(1m, player);
				break;
		}
	}

	private static void AfterRoomEnteredPostfix(AbstractModel __instance, AbstractRoom room, ref Task __result)
	{
		__result = AfterRoomEnteredAfterOriginal(__result, __instance, room);
	}

	private static async Task AfterRoomEnteredAfterOriginal(Task original, AbstractModel self, AbstractRoom room)
	{
		await original;

		if (self is DivineDestiny divineDestiny && room is CombatRoom && divineDestiny.Owner != null)
		{
			Flash(divineDestiny);
			await PlayerCmd.GainStars(6m, divineDestiny.Owner);
		}
	}

	private static void AfterPlayerTurnStartPostfix(AbstractModel __instance, PlayerChoiceContext choiceContext, Player player, ref Task __result)
	{
		__result = AfterPlayerTurnStartAfterOriginal(__result, __instance, choiceContext, player);
	}

	private static async Task AfterPlayerTurnStartAfterOriginal(Task original, AbstractModel self, PlayerChoiceContext choiceContext, Player player)
	{
		await original;

		ICombatState? combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}

		switch (self)
		{
			case RingOfTheSnake ringOfTheSnake when player == ringOfTheSnake.Owner && combatState.RoundNumber == 1:
				await SelectAndDiscardOne(choiceContext, ringOfTheSnake.Owner, ringOfTheSnake);
				break;
			case RingOfTheDrake ringOfTheDrake when player == ringOfTheDrake.Owner && combatState.RoundNumber <= 3:
				await SelectAndDiscardOne(choiceContext, ringOfTheDrake.Owner, ringOfTheDrake);
				break;
		}
	}

	private static void DivineRightCanonicalVarsPostfix(ref IEnumerable<DynamicVar> __result)
	{
		__result =
		[
			new StarsVar(3),
			new StarsVar("MinStars", 3),
			new StarsVar("TurnStartStars", 1)
		];
	}

	private static void DivineDestinyCanonicalVarsPostfix(ref IEnumerable<DynamicVar> __result)
	{
		__result =
		[
			new StarsVar(6),
			new StarsVar("MinStars", 6),
			new StarsVar("TurnStartStars", 2)
		];
	}

	private static bool DivineDestinyAfterSideTurnStartPrefix(DivineDestiny __instance, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, ref Task __result)
	{
		__result = DivineDestinyAfterSideTurnStartReplacement(__instance, side, participants);
		return false;
	}

	private static async Task DivineDestinyAfterSideTurnStartReplacement(DivineDestiny self, CombatSide side, IReadOnlyList<Creature> participants)
	{
		if (self.Owner == null || side != self.Owner.Creature.Side || !participants.Contains(self.Owner.Creature) || self.Owner.PlayerCombatState == null || self.Owner.PlayerCombatState.Stars >= 6)
		{
			return;
		}

		Flash(self);
		await PlayerCmd.GainStars(2m, self.Owner);
	}

	private static bool BoundPhylacteryBeforeCombatStartPrefix(ref Task __result)
	{
		__result = Task.CompletedTask;
		return false;
	}

	private static bool BoundPhylacteryAfterEnergyResetLatePrefix(BoundPhylactery __instance, Player player, ref Task __result)
	{
		__result = BoundPhylacteryAfterEnergyResetLateReplacement(__instance, player);
		return false;
	}

	private static async Task BoundPhylacteryAfterEnergyResetLateReplacement(BoundPhylactery self, Player player)
	{
		if (self.Owner == null || player != self.Owner)
		{
			return;
		}

		Flash(self);
		await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), self.Owner, self.Owner.IsOstyAlive ? 2m : 1m, self);
	}

	private static bool PhylacteryUnboundAfterSideTurnStartPrefix(PhylacteryUnbound __instance, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, ref Task __result)
	{
		__result = PhylacteryUnboundAfterSideTurnStartReplacement(__instance, side, participants);
		return false;
	}

	private static async Task PhylacteryUnboundAfterSideTurnStartReplacement(PhylacteryUnbound self, CombatSide side, IReadOnlyList<Creature> participants)
	{
		if (self.Owner == null || side != self.Owner.Creature.Side || !participants.Contains(self.Owner.Creature))
		{
			return;
		}

		Flash(self);
		await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), self.Owner, self.Owner.IsOstyAlive ? 4m : 2m, self);
	}

	private static void CrackedCoreBeforeSideTurnStartPostfix(CrackedCore __instance, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, ref Task __result)
	{
		__result = CrackedCoreBeforeSideTurnStartAfterOriginal(__result, __instance, side, participants, combatState);
	}

	private static async Task CrackedCoreBeforeSideTurnStartAfterOriginal(Task original, CrackedCore self, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		await original;

		if (self.Owner == null || side != self.Owner.Creature.Side || !participants.Contains(self.Owner.Creature) || combatState.RoundNumber != 3)
		{
			return;
		}

		Flash(self);
		await GainFocus(self.Owner, 1m);
	}

	private static void InfusedCoreAfterSideTurnStartPostfix(InfusedCore __instance, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState, ref Task __result)
	{
		__result = InfusedCoreAfterSideTurnStartAfterOriginal(__result, __instance, side, participants, combatState);
	}

	private static async Task InfusedCoreAfterSideTurnStartAfterOriginal(Task original, InfusedCore self, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		await original;

		if (self.Owner == null || side != self.Owner.Creature.Side || !participants.Contains(self.Owner.Creature) || combatState.RoundNumber > 1)
		{
			return;
		}

		Flash(self);
		await GainFocus(self.Owner, 1m);
	}

	private static bool IsAtOrBelowHalfHp(Creature creature)
	{
		return creature.CurrentHp * 2 <= creature.MaxHp;
	}

	private static async Task GainFocus(Player owner, decimal amount)
	{
		await PowerCmd.Apply<FocusPower>(new ThrowingPlayerChoiceContext(), owner.Creature, amount, owner.Creature, null);
	}

	private static async Task SelectAndDiscardOne(PlayerChoiceContext choiceContext, Player? owner, RelicModel source)
	{
		if (owner == null || owner.Creature.IsDead || owner.PlayerCombatState == null)
		{
			return;
		}

		var selectedCards = (await CardSelectCmd.FromHandForDiscard(
			choiceContext,
			owner,
			new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1),
			filter: null,
			source)).ToList();
		if (selectedCards.Count == 0)
		{
			return;
		}

		Flash(source);
		await CardCmd.Discard(choiceContext, selectedCards);
	}

	private static void Flash(RelicModel relic)
	{
		typeof(RelicModel).GetMethod("Flash", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(relic, null);
	}
}
