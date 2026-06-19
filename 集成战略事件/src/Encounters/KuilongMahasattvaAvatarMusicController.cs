using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace IntegratedStrategyEvents.Encounters;

internal sealed class KuilongMahasattvaAvatarMusicController : EncounterMusicController
{
	private const string PlayerName = "IntegratedStrategyKuilongMusic";
	internal const string TrackPath = $"res://{ModInfo.ModId}/audio/music/kuilong_mahasattva_avatar.ogg";
	private const float VolumeScale = 0.40f;

	private static readonly KuilongMahasattvaAvatarMusicController Instance = new();

	private KuilongMahasattvaAvatarMusicController()
		: base(
			PlayerName,
			TrackPath,
			VolumeScale,
			"Kuilong",
			IsKuilongMahasattvaAvatarEncounter)
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

	internal static bool IsKuilongMahasattvaAvatarEncounter(EncounterModel encounter)
	{
		return encounter is KuilongMahasattvaAvatarBossEncounter ||
			encounter.CanonicalInstance is KuilongMahasattvaAvatarBossEncounter ||
			encounter is KuilongMahasattvaAvatarTestEncounter ||
			encounter.CanonicalInstance is KuilongMahasattvaAvatarTestEncounter;
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenu))]
internal static class KuilongMahasattvaAvatarMusicReturnToMainMenuPatch
{
	private static void Prefix()
	{
		KuilongMahasattvaAvatarMusicController.Stop(restoreGameMusic: false);
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenuAfterRun))]
internal static class KuilongMahasattvaAvatarMusicReturnToMainMenuAfterRunPatch
{
	private static void Prefix()
	{
		KuilongMahasattvaAvatarMusicController.Stop(restoreGameMusic: false);
	}
}
