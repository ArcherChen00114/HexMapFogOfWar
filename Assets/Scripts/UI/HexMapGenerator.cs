using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
	int cellCount;

	public HexGrid grid;

	HexCellPriorityQueue searchFrontier;


	int searchFrontierPhase;
	public bool useFixedSeed;

	public int seed;
	//用于控制提升预期的百分比
	[Range(0f, 0.5f)]
	public float jitterProbability = 0.25f;

	[Range(20, 200)]
	public int chunkSizeMin = 30;

	[Range(20, 200)]
	public int chunkSizeMax = 100;

	[Range(5, 95)]
	public int landPercentage = 50;

	[Range(1, 5)]
	public int waterLevel = 3;

	[Range(0f, 1f)]
	public float highRiseProbability = 0.25f;

	[Range(0f, 0.4f)]
	public float sinkProbability = 0.2f;

	[Range(-4, 0)]
	public int elevationMinimum = -2;

	[Range(6, 10)]
	public int elevationMaximum = 8;

	HexCell GetRandomCell()
	{
		return grid.GetCell(Random.Range(0, cellCount));
	}
	void CreateLand()
	{   //持续生成直到已定数值为0，每次生成减少1
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f); 
		while (landBudget > 0)
		{
			int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
			if (Random.value < sinkProbability)
			{
				landBudget = SinkTerrain(chunkSize, landBudget);
			}
			else
			{
				landBudget = RaiseTerrain(chunkSize, landBudget);
			}
		}
	}
	void SetTerrainType()
	{//按照高度给地形
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i); 
			if (!cell.IsUnderwater)
			{
				cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
			}
		}
	}
	int RaiseTerrain (int chunkSize, int budget) 
	{
		//随机取一个方格，然后以他为中心开始寻找范围内的单元格。持续到寻找到对应数目的单元格
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell();
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		//有一定概率将提升的高度从1变为2，高峰
		int rise = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			//抬升陆地，可能多次抬同一个？
			//只有高度够高才能露出水面，同时地形的不规则性加强，且地形更加崎岖
			int originalElevation = current.Elevation;
			int newElevation = originalElevation + rise;
			if (newElevation > elevationMaximum)
			{
				continue;
			}
			current.Elevation = newElevation;

			//几个判断条件一个个执行，前面的正确才会判断后面的，如果前两个条件符合才会执行最后一个判断（budget才--）
			if (
				originalElevation < waterLevel &&
				newElevation >= waterLevel && --budget == 0)
			{
				break;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					//启用启发式算法，随机给与启发式算法预期值来修改优先级，改变搜索的方向和最终结果形状
					//预期的距离越高优先级越低，其他更远的单元格就会被先访问
					neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();

		return budget;
	}
	int SinkTerrain(int chunkSize, int budget)
	{
		//随机取一个方格，然后以他为中心开始寻找范围内的单元格。持续到寻找到对应数目的单元格
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell();
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		//有一定概率将提升的高度从1变为2，高峰
		int sink = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			//抬升陆地，可能多次抬同一个？
			//只有高度够高才能露出水面，同时地形的不规则性加强，且地形更加崎岖
			int originalElevation = current.Elevation;
			int newElevation = current.Elevation - sink;
			if (newElevation < elevationMinimum)
			{
				continue;
			}

			//几个判断条件一个个执行，前面的正确才会判断后面的，如果前两个条件符合才会执行最后一个判断（budget才--）
			if (originalElevation >= waterLevel &&
				newElevation < waterLevel)
			{
				budget += 1;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					//启用启发式算法，随机给与启发式算法预期值来修改优先级，改变搜索的方向和最终结果形状
					//预期的距离越高优先级越低，其他更远的单元格就会被先访问
					neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();

		return budget;
	}
	public void GenerateMap(int x, int z)
	{

		Random.State originalRandomState = Random.state;
		if (!useFixedSeed)
		{
			seed = Random.Range(0, int.MaxValue);
			seed ^= (int)System.DateTime.Now.Ticks;
			seed ^= (int)Time.time;
			seed &= int.MaxValue;
		}
		Random.InitState(seed);
		cellCount = x * z;
		grid.CreateMap(x, z); 
		//把优先级队列用于寻路
		if (searchFrontier == null)
		{
			searchFrontier = new HexCellPriorityQueue();
		}
		//生成海面，所有水面设置为waterLevel，没有抬升高度的地方就是海了
		for (int i = 0; i < cellCount; i++)
		{
			grid.GetCell(i).WaterLevel = waterLevel;
		}
		CreateLand();
		SetTerrainType();
		//重设队列以免影响生成之后的寻路
		for (int i = 0; i < cellCount; i++)
		{
			grid.GetCell(i).SearchPhase = 0;
		}

		Random.state = originalRandomState;
	}

}