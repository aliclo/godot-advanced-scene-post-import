using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

public partial class EditorScenePostImportPluginPlus : EditorScenePostImportPlugin
{

    // private static System.Collections.Generic.Dictionary<string, List<EditorScenePostImportPlus>> _importScripts = new System.Collections.Generic.Dictionary<string, List<EditorScenePostImportPlus>>();
    // private static System.Collections.Generic.Dictionary<EditorScenePostImportPlus, List<CustomImportProperty>> _props = new System.Collections.Generic.Dictionary<EditorScenePostImportPlus, List<CustomImportProperty>>();
    // private static string _path;
    private static string _importPlusPath;

    public override void _GetImportOptions(string path)
    {
        // _path = path;
        _importPlusPath = $"{path}.import.plus";
        
        GD.Print("Getting import options ", path);
        AddImportOption("Scripts", new Array<EditorScenePostImportPlus>());

        var plusImporterIo = new PlusImporterIO();

        try {
            var importScriptsPlus = plusImporterIo.Load(_importPlusPath);

            if(importScriptsPlus == null) {
                return;
            }

            foreach(var importScriptParams in importScriptsPlus.ImportScriptsParams) {
                GD.Print("Import script: ", importScriptParams);
                foreach(var importScriptParam in importScriptParams.ImportProperties) {
                    GD.Print(importScriptParam.Name, importScriptParam.Value);
                    AddImportOption($"{importScriptParams.Name}/{importScriptParam.Name}", importScriptParam.Value);
                }
            }
        } catch (SceneImportPlusException e) {
            GD.PrintErr(e.Message);
            GD.PrintErr($"Failed to import: {_importPlusPath}");
        }

        // var exists = _importScripts.TryGetValue(path, out List<EditorScenePostImportPlus> importScripts);
        // GD.Print("Does it exist: ", exists);

        // if(exists) {
        //     foreach(var importScript in importScripts) {
        //         GD.Print("Import script: ", importScript);
        //         foreach(var importProp in importScript.GetImportProps()) {
        //             GD.Print(importProp.Name, importProp.DefaultValue);
        //             AddImportOption(importProp.Name, importProp.DefaultValue);
        //         }
        //     }
        // }
    }

    // public override void _PostProcess(Node scene)
    // {
    //     GD.Print("Post process this");
    // }

    public override void _PreProcess(Node scene)
    {
        var scripts = (Array<CSharpScript>) GetOptionValue("Scripts");
        var importScripts = scripts.Select(s => (EditorScenePostImportPlus) s.New().Obj).ToList();
        // _importScripts[_path] = importScripts;
        GD.Print("About to process");

        var propertyNames = importScripts[0].GetType().GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(ExportAttribute)));

        GD.Print("Properties!: ", string.Join(", ", propertyNames.Select(p => $"{p.Name}:{p.PropertyType.IsAssignableTo(typeof(Variant))}")));

        var defaultImportScriptsParams = importScripts.Select(s => new ImportScriptParams() {
            Name = s.GetType().Name,
            ImportProperties = s.GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(ExportAttribute))).Select(ip => {
                var optionKey = $"{s.GetType().Name}/{ip.Name}";
                Variant value = GetOptionValue(optionKey);

                if(value.Obj == null) {
                    value = (Variant)typeof(Variant).GetMethod("From").MakeGenericMethod(ip.PropertyType).Invoke(null, new[] { ip.GetValue(s) });
                    GD.Print($"Using default value instead {optionKey} = {value}");
                }

                // if(value == null) {
                //     value = ip.Value;
                // }

                return new CustomImportProperty() {
                    Name = ip.Name,
                    Value = value
                };
            }).ToList()
        });

        // List<ImportScriptParams> importScriptsParams;

        // if(File.Exists(_importPlusPath)) {
        //     importScriptsParams = JsonSerializer.Deserialize<List<ImportScriptParams>>(File.ReadAllText(_importPlusPath));
        // } else {
        //     importScriptsParams = new List<ImportScriptParams>();
        // }

        // foreach(var defaultImportScriptParams in defaultImportScriptsParams) {
        //     var importScriptParams = importScriptsParams.SingleOrDefault()
        // }

        var importScriptsPlus = new ImportScriptsPlus() {
            Scripts = scripts.Select(s => s.ResourcePath).ToList(),
            ImportScriptsParams = defaultImportScriptsParams.ToList()
        };

        var plusImporterIo = new PlusImporterIO();
        plusImporterIo.Save(_importPlusPath, importScriptsPlus);

        // foreach(var importScript in importScripts) {
        //     GD.Print("Import script: ", importScript);
        //     foreach(var importProp in importScript.GetImportProps()) {
        //         GD.Print(importProp.Name, importProp.DefaultValue);
        //         GD.Print("Got value for ", importProp.Name, GetOptionValue(importProp.Name));
        //     }
        // }
    }



}
