using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace IntegratedStrategyEvents.Encounters;

internal sealed class IsharmlaCorruptedHeartMusicController : EncounterMusicController
{
	private const string PlayerName = "IntegratedStrategyIsharmlaMusic";
	internal const string TrackPath = $"res://{ModInfo.ModId}/audio/music/isharmla_boss.ogg";
	private const float VolumeScale = 0.52f;

	private static readonly IsharmlaCorruptedHeartMusicController Instance = new();

	private IsharmlaCorruptedHeartMusicController()
		: base(
			PlayerName,
			TrackPath,
			VolumeScale,
			"Isharmla",
			IsIsharmlaCorruptedHeartEncounter)
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

	internal static bool IsIsharmlaCorruptedHeartEncounter(EncounterModel encounter)
	{
		return encounter is IsharmlaCorruptedHeartBossEncounter ||
			encounter.CanonicalInstance is IsharmlaCorruptedHeartBossEncounter ||
			encounter is IsharmlaCorruptedHeartTestEncounter ||
			encounter.CanonicalInstance is IsharmlaCorruptedHeartTestEncounter;
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenu))]
internal static class IsharmlaCorruptedHeartMusicReturnToMainMenuPatch
{
	private static void Prefix()
	{
		IsharmlaCorruptedHeartMusicController.Stop(restoreGameMusic: false);
	}
}

[HarmonyPatch(typeof(NGame), nameof(NGame.ReturnToMainMenuAfterRun))]
internal static class IsharmlaCorruptedHeartMusicReturnToMainMenuAfterRunPatch
{
	private static void Prefix()
	{
		IsharmlaCorruptedHeartMusicController.Stop(restoreGameMusic: false);
	}
}
