using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace IntegratedStrategyEvents.TreeHoles;

internal static class TreeHoleSessionManager
{
	internal static TreeHoleSessionStore SessionStore { get; } = new();

	public static bool IsActive(IRunState? runState)
	{
		return runState is RunState state && SessionStore.IsActive(state);
	}

	public static bool IsActiveCurrentRun()
	{
		return IsActive(RunManager.Instance.DebugOnlyGetState());
	}

	public static bool TryRestoreCompletedCurrentRun()
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		if (state == null || !SessionStore.TryGetTreeHoleSession(state, out TreeHoleSession session))
		{
			return false;
		}

		if (SessionStore.IsCompletionSuppressedUntilTerminalProceed(state))
		{
			return false;
		}

		if (state.CurrentRoom is not MapRoom)
		{
			return false;
		}

		if (!HasVisitedCoord(state.VisitedMapCoords, session.TerminalCoord))
		{
			return false;
		}

		RestoreOriginalMap(state, session);
		return true;
	}

	public static bool TryRestoreCompletedCurrentRunAfterTerminalProceed()
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		if (state == null || !SessionStore.TryGetTreeHoleSession(state, out TreeHoleSession session))
		{
			return false;
		}

		if (!state.CurrentMapCoord.HasValue || !state.CurrentMapCoord.Value.Equals(session.TerminalCoord))
		{
			return false;
		}

		if (!HasVisitedCoord(state.VisitedMapCoords, session.TerminalCoord))
		{
			return false;
		}

		if (state.CurrentRoom is not MapRoom &&
			state.CurrentRoom is not TreasureRoom &&
			state.CurrentRoom is not CombatRoom)
		{
			return false;
		}

		RestoreOriginalMap(state, session);
		return true;
	}

	public static bool TryGetCurrentDestination(out string actName)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		if (state != null && SessionStore.TryGetTreeHoleSession(state, out TreeHoleSession session))
		{
			actName = session.DestinationActName;
			return true;
		}

		if (state != null && SessionStore.TryGetFinaleSession(state, out EndlessFinaleSession finaleSession))
		{
			actName = finaleSession.DestinationActName;
			return true;
		}

		actName = string.Empty;
		return false;
	}

	public static bool TryGetCurrentDestination(out string stageLabel, out string actName)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		if (state != null && SessionStore.TryGetTreeHoleSession(state, out TreeHoleSession session))
		{
			stageLabel = session.StageLabel;
			actName = session.DestinationActName;
			return true;
		}

		if (state != null && SessionStore.TryGetFinaleSession(state, out EndlessFinaleSession finaleSession))
		{
			stageLabel = finaleSession.StageLabel;
			actName = finaleSession.DestinationActName;
			return true;
		}

		stageLabel = string.Empty;
		actName = string.Empty;
		return false;
	}

	public static TreeHoleSaveSnapshot? GetSaveSnapshot(RunState? state)
	{
		return IntegratedStrategyTreeHoleSaveStateStore.CreateSnapshot(state, SessionStore);
	}

	public static void QueueRestoreFromSave(SerializableRun save, RunState state)
	{
		TreeHoleRestoreSnapshot? snapshot = IntegratedStrategyTreeHoleSaveStateStore.Load(save);
		if (snapshot == null)
		{
			return;
		}

		SessionStore.QueueRestore(state, snapshot);
		if (ShouldWaitForTerminalRewardsProceed(save, snapshot))
		{
			SessionStore.SuppressCompletionUntilTerminalProceed(state);
			Log.Info($"{ModInfo.LogPrefix} Delaying tree-hole completion restore until terminal rewards proceed.");
		}

		Log.Info($"{ModInfo.LogPrefix} Queued {snapshot.Kind} tree-hole restore from save.");
	}

	public static bool TryRestoreSavedSessionForCurrentRun(ActMap map)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		if (state == null || !SessionStore.TryGetPendingRestore(state, out TreeHoleRestoreSnapshot snapshot))
		{
			return false;
		}

		if (state.CurrentActIndex != snapshot.CurrentActIndex || !MapContainsCoord(map, snapshot.TerminalCoord))
		{
			Log.Warn($"{ModInfo.LogPrefix} Ignored stale tree-hole restore snapshot.");
			SessionStore.RemovePendingRestore(state);
			return false;
		}

		ActMap originalMap = new SavedActMap(snapshot.OriginalMap);
		List<IReadOnlyList<MapPointHistoryEntry>> originalHistory =
			CopyHistoryByCounts(state.MapPointHistory, snapshot.OriginalMapPointHistoryCounts);

		if (snapshot.Kind == TreeHoleSaveKind.TreeHole)
		{
			SessionStore.SetTreeHoleSession(state, new TreeHoleSession(
				originalMap,
				snapshot.OriginalVisitedMapCoords,
				originalHistory,
				snapshot.OriginalActFloor,
				snapshot.StageLabel,
				snapshot.DestinationActName,
				map,
				snapshot.TerminalCoord));
		}
		else
		{
			SpecialFinaleKind finaleKind = snapshot.Kind switch
			{
				TreeHoleSaveKind.EternalDustFinale => SpecialFinaleKind.EternalDust,
				TreeHoleSaveKind.RadiantApexFinale => SpecialFinaleKind.RadiantApex,
				TreeHoleSaveKind.CarefreeViharaFinale => SpecialFinaleKind.CarefreeVihara,
				TreeHoleSaveKind.AbyssalJungleFinale => SpecialFinaleKind.AbyssalJungle,
				TreeHoleSaveKind.AbyssalJungleIsharmlaFinale => SpecialFinaleKind.AbyssalJungleIsharmla,
				TreeHoleSaveKind.ProphetHornFragment => SpecialFinaleKind.ProphetHornFragment,
				_ => SpecialFinaleKind.EndlessFinale
			};
			SessionStore.SetFinaleSession(state, new EndlessFinaleSession(
				originalMap,
				snapshot.OriginalVisitedMapCoords,
				originalHistory,
				snapshot.OriginalActFloor,
				state.Act.ToSave(),
				snapshot.StageLabel,
				snapshot.DestinationActName,
				map,
				finaleKind));
		}

		state.ActFloor = snapshot.CurrentActFloor;
		SessionStore.RemovePendingRestore(state);
		RefreshLocationSynchronizers(state);
		Log.Info($"{ModInfo.LogPrefix} Restored {snapshot.Kind} tree-hole session from save.");
		return true;
	}

	public static bool IsCurrentTreeHoleMap(ActMap map)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		return state != null &&
			SessionStore.TryGetTreeHoleSession(state, out TreeHoleSession session) &&
			ReferenceEquals(session.TreeHoleMap, map);
	}

	public static bool IsCurrentEndlessFinaleMap(ActMap map)
	{
		return IsCurrentFinaleMap(map, SpecialFinaleKind.EndlessFinale);
	}

	public static bool IsCurrentEternalDustFinaleMap(ActMap map)
	{
		return IsCurrentFinaleMap(map, SpecialFinaleKind.EternalDust);
	}

	public static bool IsCurrentRadiantApexFinaleMap(ActMap map)
	{
		return IsCurrentFinaleMap(map, SpecialFinaleKind.RadiantApex);
	}

	public static bool IsCurrentCarefreeViharaFinaleMap(ActMap map)
	{
		return IsCurrentFinaleMap(map, SpecialFinaleKind.CarefreeVihara);
	}

	public static bool IsCurrentAbyssalJungleFinaleMap(ActMap map)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		return state != null &&
			SessionStore.TryGetFinaleSession(state, out EndlessFinaleSession session) &&
			(session.Kind == SpecialFinaleKind.AbyssalJungle ||
			 session.Kind == SpecialFinaleKind.AbyssalJungleIsharmla) &&
			ReferenceEquals(session.FinaleMap, map);
	}

	public static bool IsCurrentProphetHornFragmentMap(ActMap map)
	{
		return IsCurrentFinaleMap(map, SpecialFinaleKind.ProphetHornFragment);
	}

	public static bool TryGetTreeHoleSession(RunState state, out TreeHoleSession session)
	{
		return SessionStore.TryGetTreeHoleSession(state, out session);
	}

	public static void SetTreeHoleSession(RunState state, TreeHoleSession session)
	{
		SessionStore.SetTreeHoleSession(state, session);
	}

	public static bool TryGetFinaleSession(RunState state, out EndlessFinaleSession session)
	{
		return SessionStore.TryGetFinaleSession(state, out session);
	}

	public static void SetFinaleSession(RunState state, EndlessFinaleSession session)
	{
		SessionStore.SetFinaleSession(state, session);
	}

	public static bool AddPendingFinaleEntry(RunState state)
	{
		return SessionStore.AddPendingFinaleEntry(state);
	}

	public static bool RemovePendingFinaleEntry(RunState state)
	{
		return SessionStore.RemovePendingFinaleEntry(state);
	}

	public static void AddPendingArchitectCompletion(RunState state)
	{
		SessionStore.AddPendingArchitectCompletion(state);
	}

	public static bool HasPendingArchitectCompletion(RunState state)
	{
		return SessionStore.HasPendingArchitectCompletion(state);
	}

	public static bool RemovePendingArchitectCompletion(RunState state)
	{
		return SessionStore.RemovePendingArchitectCompletion(state);
	}

	public static void OnRunStarted(RunState state)
	{
		SessionStore.ClearForRunStarted(state);
	}

	public static void OnRoomEntered()
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		if (state?.CurrentRoom is not MapRoom || !SessionStore.TryGetTreeHoleSession(state, out TreeHoleSession session))
		{
			return;
		}

		if (!HasVisitedCoord(state.VisitedMapCoords, session.TerminalCoord))
		{
			return;
		}

		if (SessionStore.IsCompletionSuppressedUntilTerminalProceed(state))
		{
			return;
		}

		RestoreOriginalMap(state, session);
	}

	public static void RestoreOriginalMapForArchitect(RunState state, EndlessFinaleSession session)
	{
		TreeHoleFinaleMusicCoordinator.StopBeforeArchitectHandoff(session);

		SessionStore.RemoveFinaleSession(state);
		SessionStore.RemovePendingFinaleEntry(state);
		state.Map = session.OriginalMap;
		state.ClearVisitedMapCoordsDebug();
		foreach (MapCoord coord in session.OriginalVisitedMapCoords)
		{
			state.AddVisitedMapCoord(coord);
		}

		RestoreMapPointHistory(state, session.OriginalMapPointHistory);
		state.ActFloor = session.OriginalActFloor;
		TreeHoleRunAccessor.RestoreActRooms(state, session.OriginalActSave);
		RefreshLocationSynchronizers(state);
		SessionStore.AddPendingArchitectCompletion(state);
		Log.Info($"{ModInfo.LogPrefix} Returned from {session.DestinationActName} finale before entering The Architect.");
	}

	public static void RestoreOriginalMapFromFinale(RunState state, EndlessFinaleSession session)
	{
		SessionStore.RemoveFinaleSession(state);
		SessionStore.RemovePendingFinaleEntry(state);
		state.Map = session.OriginalMap;
		state.ClearVisitedMapCoordsDebug();
		foreach (MapCoord coord in session.OriginalVisitedMapCoords)
		{
			state.AddVisitedMapCoord(coord);
		}

		RestoreMapPointHistory(state, session.OriginalMapPointHistory);
		state.ActFloor = session.OriginalActFloor;
		TreeHoleRunAccessor.RestoreActRooms(state, session.OriginalActSave);
		RefreshLocationSynchronizers(state);
		SetMapScreen(session.OriginalMap, state, initMarker: state.CurrentMapCoord.HasValue);
		Log.Info($"{ModInfo.LogPrefix} Returned from {session.DestinationActName} finale.");
	}

	public static void RestoreMapPointHistory(
		RunState state,
		IReadOnlyList<IReadOnlyList<MapPointHistoryEntry>> originalHistory)
	{
		if (!TreeHoleRunAccessor.TryGetMapPointHistory(state, out List<List<MapPointHistoryEntry>> mapPointHistory))
		{
			Log.Warn($"{ModInfo.LogPrefix} Could not restore tree-hole map history.");
			return;
		}

		mapPointHistory.Clear();
		foreach (IReadOnlyList<MapPointHistoryEntry> actHistory in originalHistory)
		{
			mapPointHistory.Add(actHistory.ToList());
		}
	}

	public static bool HasVisitedCoord(IEnumerable<MapCoord> visitedCoords, MapCoord coord)
	{
		return visitedCoords.Any(visited => visited.Equals(coord));
	}

	private static bool ShouldWaitForTerminalRewardsProceed(
		SerializableRun save,
		TreeHoleRestoreSnapshot snapshot)
	{
		return snapshot.Kind == TreeHoleSaveKind.TreeHole &&
			save.PreFinishedRoom is { IsPreFinished: true } &&
			HasVisitedCoord(save.VisitedMapCoords, snapshot.TerminalCoord);
	}

	public static bool MapContainsCoord(ActMap map, MapCoord coord)
	{
		return map.HasPoint(coord) ||
			map.StartingMapPoint.coord.Equals(coord) ||
			map.BossMapPoint.coord.Equals(coord) ||
			map.SecondBossMapPoint?.coord.Equals(coord) == true;
	}

	public static void RefreshLocationSynchronizers(RunState state)
	{
		RunManager.Instance.MapSelectionSynchronizer.OnLocationChanged(state.MapLocation);
		RunManager.Instance.RunLocationTargetedBuffer.OnLocationChanged(state.RunLocation);
	}

	public static void SetMapScreen(ActMap map, RunState state, bool initMarker)
	{
		NMapScreen? mapScreen = NMapScreen.Instance;
		if (mapScreen == null)
		{
			return;
		}

		mapScreen.SetMap(map, state.Rng.Seed, clearDrawings: true);
		if (initMarker && state.CurrentMapCoord is { } currentCoord && map.HasPoint(currentCoord))
		{
			mapScreen.InitMarker(currentCoord);
		}

		mapScreen.SetTravelEnabled(true);
		mapScreen.RefreshAllMapPointVotes();
	}

	public static async Task AwaitNextProcessFrame()
	{
		if (NGame.Instance != null)
		{
			await NGame.Instance.AwaitProcessFrame();
			return;
		}

		await Task.Yield();
	}

	private static bool IsCurrentFinaleMap(ActMap map, SpecialFinaleKind kind)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		return state != null &&
			SessionStore.TryGetFinaleSession(state, out EndlessFinaleSession session) &&
			session.Kind == kind &&
			ReferenceEquals(session.FinaleMap, map);
	}

	private static void RestoreOriginalMap(RunState state, TreeHoleSession session)
	{
		SessionStore.RemoveCompletionSuppression(state);
		SessionStore.RemoveTreeHoleSession(state);
		state.Map = session.OriginalMap;
		state.ClearVisitedMapCoordsDebug();
		foreach (MapCoord coord in session.OriginalVisitedMapCoords)
		{
			state.AddVisitedMapCoord(coord);
		}

		RestoreMapPointHistory(state, session.OriginalMapPointHistory);
		state.ActFloor = session.OriginalActFloor;
		RefreshLocationSynchronizers(state);
		SetMapScreen(session.OriginalMap, state, initMarker: state.CurrentMapCoord.HasValue);
		Log.Info($"{ModInfo.LogPrefix} Returned from {session.DestinationActName} tree-hole.");
	}

	private static List<IReadOnlyList<MapPointHistoryEntry>> CopyHistoryByCounts(
		IReadOnlyList<IReadOnlyList<MapPointHistoryEntry>> source,
		IReadOnlyList<int> counts)
	{
		List<IReadOnlyList<MapPointHistoryEntry>> result = [];
		for (int actIndex = 0; actIndex < counts.Count; actIndex++)
		{
			IReadOnlyList<MapPointHistoryEntry> sourceHistory =
				actIndex < source.Count ? source[actIndex] : [];
			int takeCount = Math.Min(Math.Max(counts[actIndex], 0), sourceHistory.Count);
			result.Add(sourceHistory.Take(takeCount).ToList());
		}

		return result;
	}
}
