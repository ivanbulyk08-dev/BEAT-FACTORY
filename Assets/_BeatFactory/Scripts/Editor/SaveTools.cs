using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>Menu: Tools > Beat Factory > Save > ...</summary>
    public static class SaveTools
    {
        [MenuItem("Tools/Beat Factory/Save/Open Save Folder")]
        static void OpenFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath + "/");

        [MenuItem("Tools/Beat Factory/Save/Delete Save File")]
        static void DeleteSave()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Delete Save", "Stop Play Mode first (otherwise the game saves again when you stop).", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Delete Save", "Delete all Beat Factory progress on this computer?", "Delete", "Cancel"))
                return;

            new SaveManager().Delete();
            Debug.Log("[Beat Factory] Save deleted from " + Application.persistentDataPath);
        }

        [MenuItem("Tools/Beat Factory/Save/Print Save File")]
        static void PrintSave()
        {
            string path = new SaveManager().FilePath;
            Debug.Log(File.Exists(path) ? File.ReadAllText(path) : "[Beat Factory] No save file at " + path);
        }
    }
}
