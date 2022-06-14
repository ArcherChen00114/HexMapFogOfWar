using UnityEngine;

public class NewMapMenu : MonoBehaviour
{

	public HexMapGenerator mapGenerator;

	public HexGrid hexGrid;

	bool generateMaps = true;

	public void ToggleMapGeneration(bool toggle)
	{
		generateMaps = toggle;
	}
	void CreateMap(int x, int z)
	{
		if (generateMaps)
		{
			mapGenerator.GenerateMap(x, z);
		}
		else
		{
			hexGrid.CreateMap(x, z);
		}
		HexMapCamera.ValidatePosition();
		Close();
	}
	public void CreateSmallMap()
	{
		HexMetrics.MapScaleMulitper = 8f;
		CreateMap(20, 15);
	}

	public void CreateMediumMap()
	{
		HexMetrics.MapScaleMulitper = 32f;
		CreateMap(40, 30);
	}

	public void CreateLargeMap()
	{
		HexMetrics.MapScaleMulitper = 64f;
		CreateMap(80, 60);
	}
	public void Open()
	{
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

}