using UnityEngine;
using UnityEditor;
using System.IO;

public class GradientMaterialSetup
{
    [MenuItem("Tools/グラデーションマテリアル生成")]
    public static void GenerateMaterial()
    {
        // ディレクトリの存在確認
        if (!Directory.Exists("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        string matPath = "Assets/Materials/GradientBar.mat";
        
        // シェーダーの検索
        Shader shader = Shader.Find("Custom/ThreeColorGradient");
        if (shader == null)
        {
            Debug.LogError("シェーダー 'Custom/ThreeColorGradient' が見つかりません。コンパイルを待つか、ファイルが存在することを確認してください。");
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log($"新しいマテリアルを作成しました: {matPath}");
        }
        else
        {
            mat.shader = shader;
            Debug.Log($"既存のマテリアルを更新しました: {matPath}");
        }

        // 画像に合わせて色を設定 (黄 -> オレンジ -> 赤)
        // 左: 薄いオレンジ/黄色 #FDB94E (253, 185, 78)
        // 中: オレンジ #FB6516 (251, 101, 22)
        // 右: 赤 #E80E0E (232, 14, 14)

        mat.SetColor("_ColorLeft", new Color32(253, 185, 78, 255));
        mat.SetColor("_ColorMid", new Color32(251, 101, 22, 255));
        mat.SetColor("_ColorRight", new Color32(232, 14, 14, 255));
        mat.SetFloat("_Midpoint", 0.5f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = mat;
        EditorGUIUtility.PingObject(mat);
    }
}
