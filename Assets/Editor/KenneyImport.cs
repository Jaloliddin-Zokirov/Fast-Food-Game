using UnityEditor;

// Resources/Models dagi Kenney modellarini import qilish sozlamalari.
// Odamchalar Legacy animatsiya bilan (Animation komponenti orqali oson ijro etiladi), qolganlari animatsiyasiz.
public class KenneyImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Resources/Models/")) return;

        var importer = (ModelImporter)assetImporter;
        importer.importCameras = false;
        importer.importLights = false;

        if (assetPath.Contains("/Characters/"))
        {
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
        }
        else
        {
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
        }
    }
}
