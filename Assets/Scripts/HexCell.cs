using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	public RectTransform uiRect;

	public HexGridChunk chunk;

	int distance;

	[SerializeField]
	bool[] roads;

	int terrainTypeIndex;//原来表示颜色Id

	int elevation = int.MinValue;

	bool hasIncomingRiver, hasOutgoingRiver;

	bool explored;

	HexDirection incomingRiver, outgoingRiver;

	[SerializeField]
	HexCell[] neighbors;

	int waterLevel;

	int urbanLevel, farmLevel, plantLevel;

	int specialIndex;

	int visibility;

	bool walled;
	public int SearchPhase { get; set; }
	public HexCell PathFrom { get; set; }

	public HexCell NextWithSamePriority { get; set; }

	public HexUnit Unit { get; set; }

	public HexCellShaderData ShaderData { get; set; }

	public int Index { get; set; }

	public bool Explorable { get; set; }
	public bool IsExplored {
		get
		{
			return explored && Explorable;
		}
		private set
		{
			explored = value;
		}
	}

	public int Elevation
	{
		get
		{
			return elevation;
		}
		set
		{
			if (elevation == value)
			{
				return;
			}
			int originalViewElevation = ViewElevation;
			elevation = value;
			if (ViewElevation != originalViewElevation)
			{
				ShaderData.ViewElevationChanged();
			}
			RefreshPosition();
			ValidateRivers();
			for (int i = 0; i < roads.Length; i++)
			{
				if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
				{
					SetRoad(i, false);//
				}
			}

			Refresh();
		}
	}
	public int WaterLevel
	{//定义水下高度，低于该高度就是水下部分
		get
		{
			return waterLevel;
		}
		set
		{
			if (waterLevel == value)
			{
				return;
			}
			int originalViewElevation = ViewElevation;
			waterLevel = value;
			if (ViewElevation != originalViewElevation)
			{
				ShaderData.ViewElevationChanged();
			}
			ValidateRivers();
			Refresh();
		}
	}
	public bool IsUnderwater
	{
		get
		{
			return waterLevel > elevation;
		}
	}

	public bool HasRoads//但凡有条路也不会是false
	{
		get
		{
			for (int i = 0; i < roads.Length; i++)
			{
				if (roads[i])
				{
					return true;
				}
			}
			return false;
		}
	}
	public int ViewElevation
	{
		get
		{
			return elevation >= waterLevel ? elevation : waterLevel;
		}
	}
	public bool IsVisible
	{
		get
		{
			return visibility > 0 && Explorable;
		}
	}
	public void IncreaseVisibility()
	{
		visibility += 1; 
		if (visibility == 1)
		{
			IsExplored = true;
			ShaderData.RefreshVisibility(this);
		}
	}
	public void DecreaseVisibility()
	{
		visibility -= 1;
		if (visibility == 0)
		{
			ShaderData.RefreshVisibility(this);
		}
	}
	public void ResetVisibility()
	{
		if (visibility > 0)
		{
			visibility = 0;
			ShaderData.RefreshVisibility(this);
		}
	}

	public int SpecialIndex
	{
		get
		{
			return specialIndex;
		}
		set
		{
			if (specialIndex != value && !HasRiver)
			{
				specialIndex = value;
				RemoveRoads();
				RefreshSelfOnly();
			}
		}
	}

	public bool IsSpecial
	{
		get
		{
			return specialIndex > 0;
		}
	}
	public bool Walled
	{
		get
		{
			return walled;
		}
		set
		{
			if (walled != value)
			{
				walled = value;
				Refresh();
			}
		}
	}

	public int UrbanLevel//城市等级？
	{
		get
		{
			return urbanLevel;
		}
		set
		{

				urbanLevel = value;
				RefreshSelfOnly();
			
		}
	}
	public int FarmLevel
	{
		get
		{
			return farmLevel;
		}
		set
		{
			if (farmLevel != value)
			{
				farmLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int PlantLevel
	{
		get
		{
			return plantLevel;
		}
		set
		{
			if (plantLevel != value)
			{
				plantLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public void Save(BinaryWriter writer)
	{
		writer.Write((byte)terrainTypeIndex);
		//使0处于127位，将正负分到两边，使其能够保存负值
		writer.Write((byte)(elevation + 127));
		writer.Write((byte)waterLevel);
		writer.Write((byte)urbanLevel);
		writer.Write((byte)farmLevel);
		writer.Write((byte)plantLevel);
		writer.Write((byte)specialIndex);
		writer.Write(walled);

		if (hasIncomingRiver)
		{
			writer.Write((byte)(incomingRiver + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		if (hasOutgoingRiver)
		{
			writer.Write((byte)(outgoingRiver + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		int roadFlags = 0;
		for (int i = 0; i < roads.Length; i++)
		{
			//			writer.Write(roads[i]);
			if (roads[i])
			{
				roadFlags |= 1 << i;
			}
		}
		writer.Write((byte)roadFlags);
		writer.Write(IsExplored);
	}

	public void Load(BinaryReader reader, int header)
	{
		terrainTypeIndex = reader.ReadByte();
		ShaderData.RefreshTerrain(this);
		//4版存档高度默认存储为高度+127
		if (header >= 4)
		{
			elevation -= 127;
		}
		elevation = reader.ReadByte();
		RefreshPosition();
		waterLevel = reader.ReadByte();
		urbanLevel = reader.ReadByte();
		farmLevel = reader.ReadByte();
		plantLevel = reader.ReadByte();
		specialIndex = reader.ReadByte();
		walled = reader.ReadBoolean();

		byte riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			hasIncomingRiver = true;
			incomingRiver = (HexDirection)(riverData - 128);
		}
		else
		{
			hasIncomingRiver = false;
		}

		riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			hasOutgoingRiver = true;
			outgoingRiver = (HexDirection)(riverData - 128);
		}
		else
		{
			hasOutgoingRiver = false;
		}

		int roadFlags = reader.ReadByte();
		for (int i = 0; i < roads.Length; i++)
		{
			roads[i] = (roadFlags & (1 << i)) != 0;
		}

		IsExplored = header >= 3 ? reader.ReadBoolean() : false;
		ShaderData.RefreshVisibility(this);
	}
	bool IsValidRiverDestination(HexCell neighbor)
	{
		return neighbor && (
			elevation >= neighbor.elevation || waterLevel == neighbor.elevation
		);
	}
	public HexDirection RiverBeginOrEndDirection
	{
		get
		{
			return hasIncomingRiver ? incomingRiver : outgoingRiver;
		}
	}

	public void DisableHighlight()
	{
		Image highlight = uiRect.GetChild(0).GetComponent<Image>();
		highlight.enabled = false;
	}

	public void EnableHighlight(Color color)
	{
		Image highlight = uiRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}
	public void RemoveRoads()
	{
		for (int i = 0; i < neighbors.Length; i++)
		{
			if (roads[i])
			{
				SetRoad(i, false);
			}
		}
	}
	void SetRoad(int index, bool state)//一条路有来回，添加删除都需要修改来回两个方向的road
	{
		roads[index] = state;
		neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
		neighbors[index].RefreshSelfOnly();
		RefreshSelfOnly();
	}
	public void AddRoad(HexDirection direction)
	{
		if (!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1)
			//添加道路必须道路不存在，并且该方向没有河流，路不能画在河流上。而且路两端的高度差不能太高
		{
			SetRoad((int)direction, true);
		}
	}

	public int SearchHeuristic { get; set; }

	public int SearchPriority
	{
		get
		{
			return distance + SearchHeuristic*5;
		}
	}
	public Color Color
	{
		get
		{
			return HexMetrics.colors[terrainTypeIndex];
		}
	}


	public int TerrainTypeIndex
	{
		get
		{
			return terrainTypeIndex;
		}
		set
		{
			if (terrainTypeIndex != value)
			{
				terrainTypeIndex = value;
				ShaderData.RefreshTerrain(this);
			}
		}
	}

	public float RiverSurfaceY
	{
		get
		{
			return
				(elevation + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}
	public float WaterSurfaceY
	{
		get
		{
			return
				(waterLevel + HexMetrics.waterElevationOffset) *
				HexMetrics.elevationStep;
		}
	}

	void RefreshPosition()
	{
		Vector3 position = transform.localPosition;
		position.y = elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		transform.localPosition = position;
		
		Vector3 uiPosition = uiRect.localPosition;
		uiPosition.z = -position.y;
		uiRect.localPosition = uiPosition;
		
	}
		public int GetElevationDifference(HexDirection direction)
	{
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}
	public bool HasRoadThroughEdge(HexDirection direction)
	{
		return roads[(int)direction];
	}
	public bool HasIncomingRiver {
		get {
			return hasIncomingRiver;
		}
	}

	public bool HasOutgoingRiver {
		get {
			return hasOutgoingRiver;
		}
	}

	public bool HasRiver {
		get {
			return hasIncomingRiver || hasOutgoingRiver;
		}
	}

	public bool HasRiverBeginOrEnd {
		get {
			return hasIncomingRiver != hasOutgoingRiver;
		}
	}

	public HexDirection IncomingRiver {
		get {
			return incomingRiver;
		}
	}

	public HexDirection OutgoingRiver {
		get {
			return outgoingRiver;
		}
	}

	public Vector3 Position {
		get {
			return transform.localPosition;
		}
	}

	public float StreamBedY {
		get {
			return
				(elevation + HexMetrics.streamBedElevationOffset) *
				HexMetrics.elevationStep;
		}
	}


	public HexCell GetNeighbor (HexDirection direction) {
		return neighbors[(int)direction];
	}

	public void SetNeighbor (HexDirection direction, HexCell cell) {
		neighbors[(int)direction] = cell;
		cell.neighbors[(int)direction.Opposite()] = this;
	}

	public HexEdgeType GetEdgeType (HexDirection direction) {
		return HexMetrics.GetEdgeType(
			elevation, neighbors[(int)direction].elevation
		);
	}

	public HexEdgeType GetEdgeType (HexCell otherCell) {
		return HexMetrics.GetEdgeType(
			elevation, otherCell.elevation
		);
	}

	public bool HasRiverThroughEdge (HexDirection direction) {
		return
			hasIncomingRiver && incomingRiver == direction ||
			hasOutgoingRiver && outgoingRiver == direction;
	}

	public void RemoveIncomingRiver () {
		if (!hasIncomingRiver) {
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	public void RemoveOutgoingRiver () {
		if (!hasOutgoingRiver) {
			return;
		}
		hasOutgoingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	public void RemoveRiver () {
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	public void SetOutgoingRiver (HexDirection direction) {
		if (hasOutgoingRiver && outgoingRiver == direction) {
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor))
		{
			return;
		}

		RemoveOutgoingRiver();
		if (hasIncomingRiver && incomingRiver == direction) {
			RemoveIncomingRiver();
		}
		hasOutgoingRiver = true;
		outgoingRiver = direction;
		specialIndex = 0;
		//RefreshSelfOnly();

		neighbor.RemoveIncomingRiver();
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();
		neighbor.specialIndex = 0;
		//neighbor.RefreshSelfOnly();
		SetRoad((int)direction, false);//把刷新放到这里，在设置河流的同时删除道路
	}

	void ValidateRivers()
	{
		if (
			hasOutgoingRiver &&
			!IsValidRiverDestination(GetNeighbor(outgoingRiver))
		)
		{
			RemoveOutgoingRiver();
		}
		if (
			hasIncomingRiver &&
			!GetNeighbor(incomingRiver).IsValidRiverDestination(this)
		)
		{
			RemoveIncomingRiver();
		}
	}
	void Refresh () {
		if (chunk) {
			chunk.Refresh();
			for (int i = 0; i < neighbors.Length; i++) {
				HexCell neighbor = neighbors[i];
				if (neighbor != null && neighbor.chunk != chunk) {
					neighbor.chunk.Refresh();
				}
			}
			if (Unit)
			{
				Unit.ValidateLocation();
			}
		}
	}

	void RefreshSelfOnly () {
		chunk.Refresh(); 
		if (Unit)
		{
			Unit.ValidateLocation();
		}
	}
	public int Distance
	{
		get
		{
			return distance;
		}
		set
		{
			distance = value;
		}
	}
	public void SetLabel(string text)
	{
		UnityEngine.UI.Text label = uiRect.GetComponent<Text>();
		label.text = text;
	}

}