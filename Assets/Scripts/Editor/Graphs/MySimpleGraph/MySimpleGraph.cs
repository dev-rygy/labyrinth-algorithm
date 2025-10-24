using System;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;

[Graph(AssetExtension)]
[Serializable]
class MySimpleGraph : Graph
{
    public const string AssetExtension = "simpleg";

    [MenuItem("Assets/Create/Graph Toolkit Samples/Simple Graph", false)]
    static void CreateAssetFile()
    {
        GraphDatabase.PromptInProjectBrowserToCreateNewAsset<MySimpleGraph>();
    }
}
