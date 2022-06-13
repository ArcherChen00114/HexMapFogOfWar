using UnityEngine;
using static UnityEngine.Mathf;

public static class HexMetrics {

	public const float outerRadius = 10f;

	public const float outerToInner = 0.866025404f;

	public const float innerToOuter = 1f / outerToInner;

	public const float innerRadius = outerRadius * outerToInner;

	public const float waterFactor = 0.6f;

	public const float waterBlendFactor = 1f - waterFactor;

	public const float solidFactor = 0.8f;

	public const float blendFactor = 1f - solidFactor;

	public const float elevationStep = 3f;

	public const int terracesPerSlope = 2;

	public const int terraceSteps = terracesPerSlope * 2 + 1;

	public const float horizontalTerraceStepSize = 1f / terraceSteps;

	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

	public const float cellPerturbStrength = 4f;

	public const float elevationPerturbStrength = 1.5f;

	public const float streamBedElevationOffset = -1.75f;


	public const int chunkSizeX = 5, chunkSizeZ = 5;

	public const float waterElevationOffset = -0.5f;

	public const int hashGridSize = 256;

	static HexHash[] hashGrid;

	public const float hashGridScale = 0.25f;

	public const float wallHeight = 4f;

	public const float wallYOffset = -1f;

	public const float wallThickness = 0.75f;

	public const float wallElevationOffset = verticalTerraceStepSize;

	public const float wallTowerThreshold = 0.5f;

	public const float bridgeDesignLength = 7f;

	public static float HeightMapScale = 0.003f;//因为需要把整个图片扩大到整个版面，所以理论上是
											   //1/CellCountX*CellCountZ
	public static Texture2D HeightMap;

	public const float noiseScale=0.003f;

	public static Texture2D noiseSource;

	public static Texture2D ColorMap;

	public static int cellCountX, cellCountZ;

	public static float MapScaleMulitper=8f;

	public static Color[] colors;//保存颜色，以便去除HexCell中具体的颜色数据

	static float[][] featureThresholds = {
		new float[] {0.0f, 0.0f, 0.4f},
		new float[] {0.0f, 0.4f, 0.6f},
		new float[] {0.4f, 0.6f, 0.8f}
	};
	public static Vector3 WallLerp(Vector3 near, Vector3 far)
	{//Wall下移避免在有高度差时Wall悬浮
		near.x += (far.x - near.x) * 0.5f;
		near.z += (far.z - near.z) * 0.5f;
		float v =
			near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
		near.y += (far.y - near.y) * v + wallYOffset;//添加一个下降的量使塔楼和墙在斜坡情况下是立在地上
		return near;
	}
	public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
	{
		Vector3 offset;
		offset.x = far.x - near.x;
		offset.y = 0f;
		offset.z = far.z - near.z;
		return offset.normalized * (wallThickness * 0.5f); ;//减一半，因为两侧都向外移动厚度的一半
	}

	public static float[] GetFeatureThresholds(int level)
	{
		return featureThresholds[level];
	}

	static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};
	public static void InitializeHashGrid(int seed)
	{
		hashGrid = new HexHash[hashGridSize * hashGridSize];
		Random.State currentState = Random.state;
		Random.InitState(seed);
		for (int i = 0; i < hashGrid.Length; i++)
		{
			hashGrid[i] = HexHash.Create();
		}
		Random.state = currentState;
	}

	public static HexHash SampleHashGrid(Vector3 position)
	{
		int x = (int)(position.x * hashGridScale) % hashGridSize;
		if (x < 0)
		{
			x += hashGridSize;
		}
		int z = (int)(position.z * hashGridScale) % hashGridSize;
		if (z < 0)
		{
			z += hashGridSize;
		}
		return hashGrid[x + z * hashGridSize];
		//通过*Scale来缩小特征物的随机值取值范围以此放大散列表，Scale^2的方格取一个唯一随机值
		//使用坐标来取随机数数组中的值，并将负值映射到正值的坐标上

	}
	public static Vector3 GetWaterBridge(HexDirection direction)
	{
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			waterBlendFactor;
	}

	public static Vector3 GetFirstWaterCorner(HexDirection direction)
	{
		return corners[(int)direction] * waterFactor;
	}

	public static Vector3 GetSecondWaterCorner(HexDirection direction)
	{
		return corners[(int)direction + 1] * waterFactor;
	}

	public static void InitializeHeightMapScale() {
		if (cellCountZ > 0 && cellCountX > 0)
		{
			HeightMapScale = 1f / (cellCountZ * cellCountX);
		}
	}
	public static Vector4 SampleNoise (Vector3 position) {
		return noiseSource.GetPixelBilinear(
			position.x * noiseScale,
			position.z * noiseScale
		);
	}

	public static Vector4 SampleHeight(Vector2 position)
	{
		return HeightMap.GetPixelBilinear(
			position.x * HeightMapScale,
			position.y * HeightMapScale
		);
	}
	/*public static Vector4 SampleColorMap(Vector2 position)
	{
		if (ColorMap)
					{
						return ColorMap.GetPixelBilinear(
			 position.x * HeightMapScale,
			 position.y * HeightMapScale
			);
		}
	}*/

	public static Vector3 GetFirstCorner (HexDirection direction) {
		return corners[(int)direction];
	}

	public static Vector3 GetSecondCorner (HexDirection direction) {
		return corners[(int)direction + 1];
	}

	public static Vector3 GetFirstSolidCorner (HexDirection direction) {
		return corners[(int)direction] * solidFactor;
	}

	public static Vector3 GetSecondSolidCorner (HexDirection direction) {
		return corners[(int)direction + 1] * solidFactor;
	}

	public static Vector3 GetBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			blendFactor;
	}

	public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
		a.y += (b.y - a.y) * v;
		return a;
	}

	public static Color TerraceLerp (Color a, Color b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

	public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
		if (elevation1 == elevation2) {
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1) {
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

	public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
	{
		return
			(corners[(int)direction] + corners[(int)direction + 1]) *
			(0.5f * solidFactor);
	}
	public static Vector3 Perturb(Vector3 position)
	{
		Vector4 sample = SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
		position.y += 0;
		position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
		return position;
	}
	public static float Height(Vector2 position)
	{
		Vector4 sampleHeight = SampleHeight(position);
		float PositionY = (sampleHeight.x * MapScaleMulitper);
        return PositionY;
	}
	/*public static int TerrainIndex(Vector2 position)
	{
		/* 默认五个颜色
		 * 1.黄色, 沙漠贴图 R&G>0.9,0.7<B<0.9
		 * 
		 * 2.绿色，森林贴图 RGB（160,180,100），也就是 0.7<R<0.9,0.7<G<0.9,B<0.5
		 * 
		 * 3.蓝色，泥沼贴图 RGB都非常接近，但是都不接近1 0.9>R&G>0.7,0.7>B>0.5
		 * 
		 * 4.白色，泥土贴图 （1,1,1），0.7<R,0<R-G<0.2,B<0.7
		 * 
		 * 5.橙色, 雪地贴图 ，RGB值都大于0.9
			R-G>0.2,R-B>0,G-b>0
		五种状态的分布	R				G				B
			沙漠		R>0.9			G>0.9		0.7<B<0.9
			森林		0.7<R<0.9	0.7<R<G<0.9			B<0.5
			泥沼		0.7<G<R<0.9	0.7<G<0.9		0.5<B<0.7
			泥土		0.7<R		0<R-G<0.2		0.1<G-B<0.2
			雪地		RGB>0.9

		 
		Vector4 MapColor = SampleColorMap(position);
		int Index = 3;
		float R = MapColor.x;
		float G = MapColor.y;
		float B = MapColor.z;
		float RminusG = MapColor.x - MapColor.y;
		float GMinusB = MapColor.y - MapColor.z;
		if (R > 0.9)
		{
			if (G > 0.9)
			{
				if (B > 0.9)
				{
					return Index = 4;
				}
				else if (B > 0.7)
				{
					return Index = 0;
				}
				else {
					return Index = 2;
				}
			}
		}
		else if(R > 0.7){
			if (Abs(RminusG) < 0.05) {
				if (Abs(GMinusB)< 0.15) {
					return Index = 2;
				}
			}
			if (R > G)
			{
				return Index = 3;
			}
			else {
				return Index = 1;
			}

        }
        else{
			if(G>R)
			{
				return Index = 1;
			}
		}
		
		return Index;
	}*/

}