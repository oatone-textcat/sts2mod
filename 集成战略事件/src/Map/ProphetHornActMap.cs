using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.Map;

internal sealed class ProphetHornActMap : ActMap
{
	private const int MapWidth = 7;
	private const int PathCount = 7;
	private const int CenterColumn = 3;
	private const int FirstRow = 1;
	private const int FixedNodeDistanceFromTop = 7;
	private readonly int _mapLength;
	private readonly Rng _rng;
	private readonly int _secretRow;
	private readonly MapPointTypeCounts _pointTypeCounts;

	private static readonly HashSet<MapPointType> LowerMapPointRestrictions =
	[
		MapPointType.RestSite,
		MapPointType.Elite
	];

	private static readonly HashSet<MapPointType> UpperMapPointRestrictions =
	[
		MapPointType.RestSite
	];

	private static readonly HashSet<MapPointType> ParentMapPointRestrictions =
	[
		MapPointType.Elite,
		MapPointType.RestSite,
		MapPointType.Treasure,
		MapPointType.Shop
	];

	private static readonly HashSet<MapPointType> ChildMapPointRestrictions =
	[
		MapPointType.Elite,
		MapPointType.RestSite,
		MapPointType.Treasure,
		MapPointType.Shop
	];

	private static readonly HashSet<MapPointType> SiblingPointTypeRestrictions =
	[
		MapPointType.RestSite,
		MapPointType.Monster,
		MapPointType.Unknown,
		MapPointType.Elite,
		MapPointType.Shop
	];

	protected override MapPoint?[,] Grid { get; }

	public override MapPoint BossMapPoint { get; }

	public override MapPoint StartingMapPoint { get; }

	public MapCoord SecretCoord { get; private set; }

	public ProphetHornActMap(IRunState runState)
	{
		ActModel act = runState.Act;
		bool isMultiplayer = runState.Players.Count > 1;
		_mapLength = act.GetNumberOfRooms(isMultiplayer) + 1;
		_rng = new Rng(runState.Rng.Seed, "integrated_strategy_prophet_horn_map");
		_pointTypeCounts = act.GetMapPointTypes(_rng);
		Grid = new MapPoint[MapWidth, _mapLength];
		_secretRow = GetRowCount() - FixedNodeDistanceFromTop;
		BossMapPoint = CreateSpecialPoint(CenterColumn, GetRowCount(), MapPointType.Boss);
		StartingMapPoint = CreateSpecialPoint(CenterColumn, 0, MapPointType.Ancient);
		GenerateHourglassMap();
		AssignPointTypes();
		MapPathPruning.PruneAndRepair(Grid, startMapPoints, this, _pointTypeCounts, _rng, IsValidPointType);
	}

	public static bool TryGetSecretCoord(ActMap map, out MapCoord coord)
	{
		coord = default;
		int row = map.GetRowCount() - FixedNodeDistanceFromTop;
		int col = map.GetColumnCount() / 2;
		if (row <= 0 || row >= map.GetRowCount())
		{
			return false;
		}

		MapCoord secretCoord = new()
		{
			col = col,
			row = row
		};
		MapPoint? point = map.HasPoint(secretCoord) ? map.GetPoint(secretCoord) : null;
		if (point is not { PointType: MapPointType.Unknown })
		{
			return false;
		}

		coord = point.coord;
		return true;
	}

	private MapPoint GetOrCreatePoint(int col, int row)
	{
		if (col >= 0 && col < GetColumnCount() && row >= 0 && row < GetRowCount())
		{
			MapPoint? existing = Grid[col, row];
			if (existing != null)
			{
				return existing;
			}
		}

		MapPoint point = new(col, row);
		Grid[col, row] = point;
		return point;
	}

	private void GenerateHourglassMap()
	{
		if (_secretRow <= 0 || _secretRow >= GetRowCount())
		{
			throw new InvalidOperationException("Secret row is out of bounds for ProphetHornActMap.");
		}

		for (int path = 0; path < PathCount; path++)
		{
			MapPoint first = GetOrCreatePoint(_rng.NextInt(0, MapWidth), FirstRow);
			if (path == 1)
			{
				while (startMapPoints.Contains(first))
				{
					first = GetOrCreatePoint(_rng.NextInt(0, MapWidth), FirstRow);
				}
			}

			startMapPoints.Add(first);
			PathGenerate(first);
		}

		MapPoint secret = GetOrCreatePoint(CenterColumn, _secretRow);
		secret.PointType = MapPointType.Unknown;
		secret.CanBeModified = false;
		SecretCoord = secret.coord;
		foreach (MapPoint point in GetPointsInRow(_secretRow).ToList())
		{
			if (point != secret)
			{
				RedirectToSecret(point, secret);
			}
		}

		ConnectRowToBoss();
		ConnectRowToStart();
	}

	private void PathGenerate(MapPoint startingPoint)
	{
		MapPoint point = startingPoint;
		while (point.coord.row < _mapLength - 1)
		{
			MapCoord nextCoord = GenerateNextCoord(point);
			MapPoint next = GetOrCreatePoint(nextCoord.col, nextCoord.row);
			point.AddChildPoint(next);
			point = next;
		}
	}

	private MapCoord GenerateNextCoord(MapPoint current)
	{
		int row = current.coord.row + 1;
		(int minCol, int maxCol) = GetAllowedColumnsForRow(row);
		int center = GetColumnCount() / 2;
		List<int> directions = [-1, 0, 1];
		int distanceToSecret = _secretRow - current.coord.row;
		if (distanceToSecret > 3)
		{
			directions.StableShuffle(_rng);
		}
		else if (distanceToSecret > 0)
		{
			directions = BuildCenteredPriorityList(current.coord.col, center);
		}
		else
		{
			directions.StableShuffle(_rng);
		}

		foreach (int direction in directions)
		{
			int nextCol = GetNextColumn(current.coord.col, direction);
			if (nextCol < minCol || nextCol > maxCol || HasInvalidCrossover(current, nextCol))
			{
				continue;
			}

			MapPoint? target = Grid[nextCol, row];
			if ((target == null || target.parents.Contains(current) || target.parents.Count < 3) &&
				(current == StartingMapPoint ||
				 current.Children.Count < 3 ||
				 (target != null && current.Children.Contains(target))))
			{
				if (Math.Abs(nextCol - current.coord.col) > 1)
				{
					throw new InvalidOperationException(
						$"Invalid step from ({current.coord.col}, {current.coord.row}) to column {nextCol}.");
				}

				return new MapCoord
				{
					col = nextCol,
					row = row
				};
			}
		}

		int fallback = Math.Clamp(center, minCol, maxCol);
		if (Math.Abs(fallback - current.coord.col) > 1)
		{
			fallback = Math.Clamp(current.coord.col + Math.Sign(fallback - current.coord.col), minCol, maxCol);
		}

		if (HasInvalidCrossover(current, fallback))
		{
			fallback = Math.Clamp(current.coord.col, minCol, maxCol);
		}

		if (Math.Abs(fallback - current.coord.col) > 1)
		{
			throw new InvalidOperationException(
				$"Fallback step from ({current.coord.col}, {current.coord.row}) to column {fallback} exceeds adjacency.");
		}

		return new MapCoord
		{
			col = fallback,
			row = row
		};
	}

	private static List<int> BuildCenteredPriorityList(int currentCol, int centerCol)
	{
		List<int> result = [];
		int towardCenter = Math.Sign(centerCol - currentCol);
		if (towardCenter != 0)
		{
			result.Add(towardCenter);
		}

		result.Add(0);
		int awayFromCenter = -towardCenter;
		if (towardCenter != 0)
		{
			result.Add(awayFromCenter);
		}

		if (!result.Contains(-1))
		{
			result.Add(-1);
		}

		if (!result.Contains(1))
		{
			result.Add(1);
		}

		return result;
	}

	private static int GetNextColumn(int currentCol, int direction)
	{
		return direction switch
		{
			-1 => Math.Max(0, currentCol - 1),
			0 => currentCol,
			1 => Math.Min(MapWidth - 1, currentCol + 1),
			_ => currentCol
		};
	}

	private (int minCol, int maxCol) GetAllowedColumnsForRow(int row)
	{
		int center = GetColumnCount() / 2;
		int distanceToSecret = Math.Abs(row - _secretRow);
		int distanceToTop = _mapLength - 1 - row;
		int upperSpread = Math.Min(center, Math.Max(0, distanceToTop) + 1);
		int spread = Math.Min(center, Math.Min(distanceToSecret, upperSpread));
		return (Math.Max(0, center - spread), Math.Min(MapWidth - 1, center + spread));
	}

	private bool HasInvalidCrossover(MapPoint current, int targetCol)
	{
		int direction = targetCol - current.coord.col;
		if (direction == 0)
		{
			return false;
		}

		MapPoint? sibling = Grid[targetCol, current.coord.row];
		if (sibling == null)
		{
			return false;
		}

		foreach (MapPoint child in sibling.Children)
		{
			int childDirection = child.coord.col - sibling.coord.col;
			if (childDirection == -direction)
			{
				return true;
			}
		}

		return false;
	}

	private void RedirectToSecret(MapPoint strayNode, MapPoint secret)
	{
		foreach (MapPoint parent in strayNode.parents.ToList())
		{
			parent.RemoveChildPoint(strayNode);
			parent.AddChildPoint(secret);
		}

		foreach (MapPoint child in strayNode.Children.ToList())
		{
			strayNode.RemoveChildPoint(child);
			secret.AddChildPoint(child);
		}

		Grid[strayNode.coord.col, strayNode.coord.row] = null;
	}

	private void ConnectRowToBoss()
	{
		int finalRow = GetRowCount() - 1;
		for (int col = 0; col < GetColumnCount(); col++)
		{
			MapPoint? point = Grid[col, finalRow];
			if (point != null && !point.Children.Contains(BossMapPoint))
			{
				point.AddChildPoint(BossMapPoint);
			}
		}
	}

	private void ConnectRowToStart()
	{
		for (int col = 0; col < GetColumnCount(); col++)
		{
			MapPoint? point = Grid[col, FirstRow];
			if (point != null && !StartingMapPoint.Children.Contains(point))
			{
				StartingMapPoint.AddChildPoint(point);
			}
		}
	}

	private void AssignPointTypes()
	{
		ForEachInRow(GetRowCount() - 1, static point =>
		{
			point.PointType = MapPointType.RestSite;
			point.CanBeModified = false;
		});
		ForEachInRow(FirstRow, static point =>
		{
			if (point.PointType == MapPointType.Unassigned)
			{
				point.PointType = MapPointType.Monster;
			}
		});

		List<MapPointType> pointTypes =
		[
			.. Enumerable.Repeat(MapPointType.RestSite, _pointTypeCounts.NumOfRests),
			.. Enumerable.Repeat(MapPointType.Shop, _pointTypeCounts.NumOfShops),
			.. Enumerable.Repeat(MapPointType.Elite, _pointTypeCounts.NumOfElites),
			.. Enumerable.Repeat(MapPointType.Unknown, _pointTypeCounts.NumOfUnknowns)
		];
		AssignRemainingTypesToRandomPoints(new Queue<MapPointType>(pointTypes));

		foreach (MapPoint point in GetAllMapPoints().Where(static point => point.PointType == MapPointType.Unassigned))
		{
			point.PointType = MapPointType.Monster;
		}

		BossMapPoint.PointType = MapPointType.Boss;
		StartingMapPoint.PointType = MapPointType.Ancient;
	}

	private void ForEachInRow(int rowIndex, Action<MapPoint> processor)
	{
		for (int col = 0; col < GetColumnCount(); col++)
		{
			MapPoint? point = Grid[col, rowIndex];
			if (point != null)
			{
				processor(point);
			}
		}
	}

	private void AssignRemainingTypesToRandomPoints(Queue<MapPointType> pointTypesToBeAssigned)
	{
		for (int attempt = 0; attempt < 3 && pointTypesToBeAssigned.Count > 0; attempt++)
		{
			List<MapPoint> points = GetAllMapPoints()
				.Where(point => point != BossMapPoint && point != StartingMapPoint)
				.Where(static point => point.PointType == MapPointType.Unassigned)
				.ToList();
			points.StableShuffle(_rng);

			foreach (MapPoint point in points)
			{
				if (pointTypesToBeAssigned.Count == 0)
				{
					break;
				}

				MapPointType nextType = GetNextValidPointType(pointTypesToBeAssigned, point);
				if (nextType != MapPointType.Unassigned)
				{
					point.PointType = nextType;
				}
			}
		}
	}

	private MapPointType GetNextValidPointType(Queue<MapPointType> pointTypesQueue, MapPoint mapPoint)
	{
		if (pointTypesQueue.Count == 0)
		{
			return MapPointType.Unassigned;
		}

		int count = pointTypesQueue.Count;
		for (int i = 0; i < count; i++)
		{
			MapPointType pointType = pointTypesQueue.Dequeue();
			if (_pointTypeCounts.ShouldIgnoreMapPointRulesForMapPointType(pointType) ||
				IsValidPointType(pointType, mapPoint))
			{
				return pointType;
			}

			pointTypesQueue.Enqueue(pointType);
		}

		return MapPointType.Unassigned;
	}

	private bool IsValidPointType(MapPointType pointType, MapPoint mapPoint)
	{
		return IsValidForUpper(pointType, mapPoint) &&
			IsValidForLower(pointType, mapPoint) &&
			IsValidWithParents(pointType, mapPoint) &&
			IsValidWithChildren(pointType, mapPoint) &&
			IsValidWithSiblings(pointType, mapPoint);
	}

	private static bool IsValidForLower(MapPointType pointType, MapPoint mapPoint)
	{
		return mapPoint.coord.row >= 6 || !LowerMapPointRestrictions.Contains(pointType);
	}

	private bool IsValidForUpper(MapPointType pointType, MapPoint mapPoint)
	{
		return mapPoint.coord.row < _mapLength - 3 || !UpperMapPointRestrictions.Contains(pointType);
	}

	private static bool IsValidWithParents(MapPointType pointType, MapPoint mapPoint)
	{
		return !ParentMapPointRestrictions.Contains(pointType) ||
			!mapPoint.parents.Concat(mapPoint.Children).Any(point => point.PointType == pointType);
	}

	private static bool IsValidWithChildren(MapPointType pointType, MapPoint mapPoint)
	{
		return !ChildMapPointRestrictions.Contains(pointType) ||
			!mapPoint.Children.Any(point => point.PointType == pointType);
	}

	private static bool IsValidWithSiblings(MapPointType pointType, MapPoint mapPoint)
	{
		return !SiblingPointTypeRestrictions.Contains(pointType) ||
			!GetSiblings(mapPoint).Any(point => point.PointType == pointType);
	}

	private static IEnumerable<MapPoint> GetSiblings(MapPoint mapPoint)
	{
		return mapPoint.parents.SelectMany(parent => parent.Children)
			.Where(sibling => !Equals(sibling, mapPoint));
	}

	private static MapPoint CreateSpecialPoint(int col, int row, MapPointType pointType)
	{
		return new MapPoint(col, row)
		{
			PointType = pointType,
			CanBeModified = false
		};
	}
}
