using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace IntegratedStrategyEvents.Encounters;

internal sealed class IzumikEcologicalFountainMusicController : EncounterMusicController
{
	private const string PlayerName = "IntegratedStrategyIzumikMusic";
	internal const string TrackPath = $"res://{ModInfo.ModId}/audio/music/izumik_boss.ogg";
	private const float VolumeScale = 0.40f;

	private static readonly IzumikEcologicalFountainMusicController Instance = new();

	private IzumikEcologicalFountainMusicController()
		: base(
			PlayerName,
			TrackPath,
			VolumeScale,
			"Izumik",
			IsIzumikEcologicalFountainEncounter)
	{
	}

	public static void Play()
	{
		Instance.PlayMusic();
	}

	public static void Stop(bool restoreGameMusic)
	{
		Instance.StopMusic(restoreGameMusic);
	}

	internal static bool IsIzumikEcologicalFountainEncounter(EncounterModel encounter)
	{
		return encounter is IzumikEcologicalFountainBossEncounter ||
			encounter.CanonicalInstance is IzumikEcologicalFountainBossEncounter ||
			encounter is IzumikEcologicalFountainTestEncounter ||
			encounter.CanonicalInstance is IzumikEcologicalFountainTestEncounter;
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenu))]
internal static class IzumikEcologicalFountainMusicReturnToMainMenuPatch
{
	private static void Prefix()
	{
		IzumikEcologicalFountainMusicController.Stop(restoreGameMusic: false);
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenuAfterRun))]
internal static class IzumikEcologicalFountainMusicReturnToMainMenuAfterRunPatch
{
	private static void Prefix()
	{
		IzumikEcologicalFountainMusicController.Stop(restoreGameMusic: false);
	}
}
