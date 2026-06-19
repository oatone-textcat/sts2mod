using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace IntegratedStrategyEvents.Encounters;

internal sealed class FrostNovaWinterScarMusicController : EncounterMusicController
{
	private const string PlayerName = "IntegratedStrategyFrostNovaMusic";
	internal const string TrackPath = $"res://{ModInfo.ModId}/audio/music/frost_nova_winter_scar.ogg";
	private const float VolumeScale = 0.34f;

	private static readonly FrostNovaWinterScarMusicController Instance = new();

	private FrostNovaWinterScarMusicController()
		: base(
			PlayerName,
			TrackPath,
			VolumeScale,
			"FrostNova",
			IsFrostNovaWinterScarEncounter)
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

	internal static bool IsFrostNovaWinterScarEncounter(EncounterModel encounter)
	{
		return encounter is FrostNovaWinterScarBossEncounter ||
			encounter.CanonicalInstance is FrostNovaWinterScarBossEncounter ||
			encounter is FrostNovaWinterScarTestEncounter ||
			encounter.CanonicalInstance is FrostNovaWinterScarTestEncounter;
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenu))]
internal static class FrostNovaWinterScarMusicReturnToMainMenuPatch
{
	private static void Prefix()
	{
		FrostNovaWinterScarMusicController.Stop(restoreGameMusic: false);
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenuAfterRun))]
internal static class FrostNovaWinterScarMusicReturnToMainMenuAfterRunPatch
{
	private static void Prefix()
	{
		FrostNovaWinterScarMusicController.Stop(restoreGameMusic: false);
	}
}
