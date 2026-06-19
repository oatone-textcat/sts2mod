using System.Reflection;
using IntegratedStrategyEvents.Cards;
using IntegratedStrategyEvents.Encounters;
using IntegratedStrategyEvents.Events;
using IntegratedStrategyEvents.Map;
using IntegratedStrategyEvents.Relics;
using IntegratedStrategyEvents.TreeHoles;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace IntegratedStrategyEvents;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	private static Harmony? HarmonyInstance;
	private static bool CardPoolsRegistered;

	public static void Initialize()
	{
		InjectSavedPropertyCaches();
		RegisterRelics();
		RegisterCards();
		EnsureModelsRegisteredIfModelDbAlreadyInitialized();
		HarmonyInstance ??= new Harmony(ModInfo.HarmonyId);
		IntegratedStrategyMapReflectionCache.Validate();
		IntegratedStrategyModelIdSerializationWarningHooks.Install(HarmonyInstance);
		HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
		IntegratedStrategyEncounterLocalization.Install();
		IntegratedStrategyEventRuntimeCompatibility.Install();
		IntegratedStrategyPotionTracker.Install();
		IntegratedStrategyTreeHoleController.Install();
		Log.Info($"{ModInfo.LogPrefix} Loaded for Slay the Spire 2 {ModInfo.TargetGameVersion}.");
	}

	private static void InjectSavedPropertyCaches()
	{
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(ProphecyProjectionRelic));
	}

	private static void RegisterRelics()
	{
		foreach (Type relicType in IntegratedStrategyContentCatalog.EventRelicTypes)
		{
			ModHelper.AddModelToPool(typeof(EventRelicPool), relicType);
		}
	}

	private static void RegisterCards()
	{
		if (CardPoolsRegistered)
		{
			return;
		}

		CardPoolsRegistered = true;
		try
		{
			ModHelper.AddModelToPool(typeof(TokenCardPool), typeof(Frozen));
		}
		catch (InvalidOperationException ex)
		{
			Log.Warn($"{ModInfo.LogPrefix} Could not add Frozen to TokenCardPool: {ex.Message}");
		}
	}

	private static void EnsureModelsRegisteredIfModelDbAlreadyInitialized()
	{
		if (!ModelDb.Contains(typeof(Ironclad)))
		{
			return;
		}

		foreach (Type type in IntegratedStrategyContent.ModelTypes)
		{
			if (ModelDb.Contains(type))
			{
				continue;
			}

			ModelDb.Inject(type);
			ModelId id = ModelDb.GetId(type);
			ModelDb.GetById<AbstractModel>(id).InitId(id);
		}
	}
}
