using Godot;
using System;
using System.Linq;

[Tool]
public partial class EditorScenePostImportApplier : EditorScenePostImport
{



    public override GodotObject _PostImport(Node scene)
    {
        GD.Print("And this");
        
        var importPlusPath = $"{GetSourceFile()}.import.plus";
        var plusImporterIo = new PlusImporterIO();

        try {
            var importScriptsPlus = plusImporterIo.Load(importPlusPath);

            if(importScriptsPlus == null) {
                return scene;
            }

            GodotObject resultObject = scene;

            foreach(var scriptPath in importScriptsPlus.Scripts) {
                var script = GD.Load<CSharpScript>(scriptPath);
                var scriptInstance = (EditorScenePostImportPlus) script.New().Obj;
                var scriptType = scriptInstance.GetType();
                var scriptName = scriptType.Name;
                var scriptImportParams = importScriptsPlus.ImportScriptsParams.SingleOrDefault(isp => isp.Name == scriptName);

                if(scriptImportParams != null) {
                    var scriptProperties = scriptType.GetProperties().Where(p => Attribute.IsDefined(p, typeof(ExportAttribute)));

                    foreach(var scriptImportParam in scriptImportParams.ImportProperties) {
                        var scriptProperty = scriptProperties.SingleOrDefault(sp => sp.Name == scriptImportParam.Name);

                        if(scriptProperty == null) {
                            GD.PrintErr($"No script parameter for \"{scriptImportParam.Name}\", skipping");
                            continue;
                        }

                        scriptProperty.SetValue(scriptInstance, scriptImportParam.Value.Obj);
                    }
                }

                resultObject = scriptInstance._PostImport(resultObject);
            }

            return resultObject;

            // foreach(var importScriptParams in importScriptsParams.ImportScriptsParams) {
            //     GD.Print("Import script: ", importScriptParams);
            //     foreach(var importScriptParam in importScriptParams.ImportProperties) {
            //         GD.Print(importScriptParam.Name, importScriptParam.Value);
            //         AddImportOption($"{importScriptParams.Name}:{importScriptParam.Name}", importScriptParam.Value);
            //     }
            // }
        } catch (SceneImportPlusException e) {
            GD.PrintErr(e.Message);
            GD.PrintErr($"Failed to import: {importPlusPath}");
            
            return base._PostImport(scene);
        }
    }


}
