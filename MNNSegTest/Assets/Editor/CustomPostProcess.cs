using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
public class CustomPostProcess : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    #if UNITY_IOS
        [PostProcessBuild]
        static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            // Read plist
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
    
            // Update value
            PlistElementDict rootDict = plist.root;
            rootDict.SetString("NSCameraUsageDescription", "Used for AR");
    
            // Write plist
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    #endif
}
