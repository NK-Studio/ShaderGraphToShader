using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;

namespace NKStudio
{
    public static class ShaderGraphConverter
    {
        private const string TargetExtension = ".shadergraph";

        /// <summary>
        /// UnityEditor.ShaderGraph.ShaderGraphImporterEditor으로부터 코드를 가져와서 수정함.
        /// </summary>
        [MenuItem("Assets/Shader Graph to Code Generate")]
        private static void CodeGenerate()
        {
            string targetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fileExtension = Path.GetExtension(targetPath);

            if (!fileExtension.Equals(TargetExtension))
            {
                Debug.LogWarning("It is not a shader file.");
                return;
            }

            string selectAssetName = Path.GetFileNameWithoutExtension(targetPath);
            string directoryPath = Path.GetDirectoryName(targetPath);

            GraphData graphData = GetGraphData(targetPath);
            Generator generator = new Generator(graphData, null, GenerationMode.ForReals, selectAssetName, humanReadable: true);

            // AssetDatabase.CreateAsset으로는 Shader 파일을 생성할 수 없음
            // generator.generatedShader : String, File을 생성하기 전 추가적인 장난질 가능
            File.WriteAllText($"{directoryPath}/{selectAssetName}.shader", generator.generatedShader);
            AssetDatabase.ImportAsset($"{directoryPath}/{selectAssetName}.shader");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Shader>($"{directoryPath}/{selectAssetName}.shader");
        }

        /// <summary>
        /// UnityEditor.ShaderGraph.ShaderGraphImporterEditor.OnInspectorGUI.GetGraphData을 가져와서 수정함.
        /// </summary>
        private static GraphData GetGraphData(string assetPath)
        {
            string textGraph = File.ReadAllText(assetPath, Encoding.UTF8);
            GraphObject graphObject = ScriptableObject.CreateInstance<GraphObject>();
            graphObject.hideFlags = HideFlags.HideAndDontSave;
            bool isSubGraph;
            string extension = Path.GetExtension(assetPath).Replace(".", "");
            switch (extension)
            {
                case ShaderGraphImporter.Extension:
                    isSubGraph = false;
                    break;
                case ShaderGraphImporter.LegacyExtension:
                    isSubGraph = false;
                    break;
                case ShaderSubGraphImporter.Extension:
                    isSubGraph = true;
                    break;
                default:
                    throw new Exception($"Invalid file extension {extension}");
            }
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            graphObject.graph = new GraphData {
                assetGuid = assetGuid,
                isSubGraph = isSubGraph,
                messageManager = null
            };
            MultiJson.Deserialize(graphObject.graph, textGraph);
            graphObject.graph.OnEnable();
            graphObject.graph.ValidateGraph();
            return graphObject.graph;
        }
    }
}
