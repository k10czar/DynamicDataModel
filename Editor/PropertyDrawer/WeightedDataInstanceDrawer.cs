using UnityEditor;

[CustomPropertyDrawer(typeof(Weighted<DataInstance>))]
public class WeightedDataInstanceDrawer : WeightedDrawer
{

}

[CustomPropertyDrawer(typeof(Weighted<UnityEngine.Color>))]
public class WeightedColorDrawer : WeightedDrawer
{

}
