using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
	public HexFeatureCollection[]
		urbanCollections, farmCollections, plantCollections;

	Transform container;

	public HexMesh walls;

	public Transform wallTower,bridge;//添加到连接处的塔楼
	public Transform[] special;
	public void Clear() {
		if (container)
		{
			Destroy(container.gameObject);
		}
		container = new GameObject("Features Container").transform;
		container.SetParent(transform, false);
		walls.Clear();
	}
	Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
	{
		if (level > 0)
		{
			float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
			for (int i = 0; i < thresholds.Length; i++)
			{

				//检测该装饰是否小于随机数来决定是否返回它，不返回就不渲染
				if (hash < thresholds[i])
				{
					return collection[i].Pick(choice);
				}
			}
		}
		return null;
	}
	public void Apply()
	{
		walls.Apply();
	}

	public void AddFeature(HexCell cell, Vector3 position)
	{
		if (cell.IsSpecial)
		{
			return;
		}
		HexHash hash = HexMetrics.SampleHashGrid(position);
		//按照不同的等级给与不同等级的装饰品不同的出现概率，空则跳过
		Transform prefab = PickPrefab(urbanCollections,cell.UrbanLevel, hash.a, hash.d);
		Transform otherPrefab = PickPrefab(farmCollections, cell.FarmLevel, hash.b, hash.d);
		float usedHash = hash.a;
		if (prefab)
		{
			if (otherPrefab && hash.b < hash.a)
			{
				prefab = otherPrefab;
				usedHash = hash.b;
			}
		}
		else if (otherPrefab)
		{
			prefab = otherPrefab;
			usedHash = hash.b;
		}
		otherPrefab = PickPrefab(plantCollections, cell.PlantLevel, hash.c, hash.d);
		if (prefab)
		{
			if (otherPrefab && hash.c < usedHash)
			{
				prefab = otherPrefab;
			}
		}
		else if (otherPrefab)
		{
			prefab = otherPrefab;
		}
		else
		{
			return;
		}
		Transform instance = Instantiate(prefab);
		position.y += instance.localScale.y * 0.5f;
		instance.localPosition = HexMetrics.Perturb(position);
		instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
		//使用随机值（Hash）来旋转物体，使物体多样化
		instance.SetParent(container, false);
	}

	public void AddWall(EdgeVertices near, HexCell nearCell,EdgeVertices far, HexCell farCell,
		bool hasRiver, bool hasRoad)
	{
		if (nearCell.Walled != farCell.Walled &&
			!nearCell.IsUnderwater && !farCell.IsUnderwater &&
			nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
		{//每一个分段处添加一处城墙，如果在水底或是临近悬崖就不渲染			
			AddWallSegment(near.v1, far.v1, near.v2, far.v2);
			if (hasRiver || hasRoad)
			{
				AddWallCap(near.v2, far.v2);
				AddWallCap(far.v4, near.v4);
				// Leave a gap.
			}
			else
			{
				AddWallSegment(near.v2, far.v2, near.v3, far.v3);
				AddWallSegment(near.v3, far.v3, near.v4, far.v4);
			}
			AddWallSegment(near.v4, far.v4, near.v5, far.v5);
		}
	}
	public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
	{
		roadCenter1 = HexMetrics.Perturb(roadCenter1);
		roadCenter2 = HexMetrics.Perturb(roadCenter2);
		Transform instance = Instantiate(bridge);
		instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
		instance.forward = roadCenter2 - roadCenter1;
		float length = Vector3.Distance(roadCenter1, roadCenter2);
		instance.localScale = new Vector3(
			1f, 1f, length * (1f / HexMetrics.bridgeDesignLength)
		);
		instance.SetParent(container, false);
	}
	public void AddWall(Vector3 c1, HexCell cell1,
	Vector3 c2, HexCell cell2,
	Vector3 c3, HexCell cell3){
		//遍历检测所有可能的组成形式，调用相应的方法。
		if (cell1.Walled)
		{
			if (cell2.Walled)
			{
				if (!cell3.Walled)
				{
					AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
				}
			}
			else if (cell3.Walled)
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
			else
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
		}
		else if (cell2.Walled)
		{
			if (cell3.Walled)
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
			else
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		}
		else if (cell3.Walled)
		{
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
	}

	void AddWallCap(Vector3 near, Vector3 far)
	{//做一个竖向的方格来封住
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.wallHeight;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);
	}//添加城墙缺口

	void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);
		point = HexMetrics.Perturb(point);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;
		Vector3 pointTop = point;
		point.y = center.y;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;

		walls.AddQuadUnperturbed(v1, point, v3, pointTop);
		walls.AddQuadUnperturbed(point, v2, pointTop, v4);
		walls.AddTriangleUnperturbed(pointTop, v3, v4);
	}//处理悬崖与城墙间的缝隙

	void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight,
		bool addTower = false)
	{
		nearLeft = HexMetrics.Perturb(nearLeft);
		farLeft = HexMetrics.Perturb(farLeft);
		nearRight = HexMetrics.Perturb(nearRight);
		farRight = HexMetrics.Perturb(farRight);
		Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
		Vector3 right = HexMetrics.WallLerp(nearRight, farRight);
		Vector3 leftThicknessOffset =
			HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		Vector3 rightThicknessOffset =
			HexMetrics.WallThicknessOffset(nearRight, farRight);

		float leftTop = left.y + HexMetrics.wallHeight;
		float rightTop = right.y + HexMetrics.wallHeight;
		//同一个六边形高度一致，但是连接处连接两个六边形，因此高度分开为两边

		Vector3 v1, v2, v3, v4;
		v1 = v3 = left - leftThicknessOffset;
		v2 = v4 = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);

		Vector3 t1 = v3, t2 = v4;

		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v2, v1, v4, v3);

		walls.AddQuadUnperturbed(t1, t2, v3, v4);

		if (addTower)
		{
			Transform towerInstance = Instantiate(wallTower);
			towerInstance.transform.localPosition = (left + right) * 0.5f;
			Vector3 rightDirection = right - left;
			rightDirection.y = 0f;
			towerInstance.transform.right = rightDirection;
			towerInstance.SetParent(container, false);
		}
	}//使用四个顶点来创建一段城墙

	void AddWallSegment(Vector3 pivot, HexCell pivotCell,
	Vector3 left, HexCell leftCell,
	Vector3 right, HexCell rightCell)
	{//使用三个方格的三个点来作为顶点使用，基本相同，但是轴心点的顶点作为两个点使用，方便一点
		if (pivotCell.IsUnderwater)
		{//如果边缘在水底，不渲染
			return;
		}

		bool hasLeftWall = !leftCell.IsUnderwater &&
			pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
		//判断左右侧墙是否在悬崖范围或是水中
		bool hasRightWall = !rightCell.IsUnderwater &&
			pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

		//只渲染两段墙中间的连接处
		if (hasLeftWall)
		{
			if (hasRightWall)
			{
				bool hasTower = false;
				if (leftCell.Elevation == rightCell.Elevation)//不渲染有高度差的Tower
				{
					HexHash hash = HexMetrics.SampleHashGrid(
						(pivot + left + right) * (1f / 3f)
					);
					hasTower = hash.e < HexMetrics.wallTowerThreshold;
				}
				AddWallSegment(pivot, left, pivot, right, hasTower);
			}
			else if (leftCell.Elevation < rightCell.Elevation)
			{
				AddWallWedge(pivot, left, right);
			}
			else
			{
				AddWallCap(pivot, left);
			}
		}
		else if (hasRightWall)
		{
			if (rightCell.Elevation < leftCell.Elevation)
			{
				AddWallWedge(right, pivot, left);
			}
			else
			{
				AddWallCap(right, pivot);
			}
		}

	}//连结城墙之间的角

	public void AddSpecialFeature(HexCell cell, Vector3 position)
	{
		Transform instance = Instantiate(special[cell.SpecialIndex - 1]);
		instance.localPosition = HexMetrics.Perturb(position);
		instance.SetParent(container, false);
	}

}