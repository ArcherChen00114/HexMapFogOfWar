using UnityEngine;

public struct HexHash
{
	//Hash值现在需要两个而不是一个
	//Why-需要通过赋予多一个Hash值，使用第二个数值来省略部分特征物，增加多样性

	public float a, b, c,d ,e;

	public static HexHash Create()
	{
		HexHash hash;
		hash.a = Random.value * 0.999f;
		hash.b = Random.value * 0.999f;
		hash.c = Random.value * 0.999f;
		hash.d = Random.value * 0.999f;
		hash.e = Random.value * 0.999f;
		return hash;
	}
}