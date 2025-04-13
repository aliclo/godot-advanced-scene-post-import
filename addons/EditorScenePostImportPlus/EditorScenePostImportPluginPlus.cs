using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

public partial class EditorScenePostImportPluginPlus : EditorScenePostImportPlugin
{

    private record CustomImportProperty {
        public string Name { get; set; }
        public Variant Value { get; set; }
    }

    private record ImportScriptParams {
        public string Name { get; set; }
        public List<CustomImportProperty> ImportProperties { get; set; }
    }

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

        List<ImportScriptParams> importScriptsParams;

        // Godot actually stores this in its own way for some reason, see editor_file_system.cpp::_reimport_group().
        // To avoid adding unknown complexity, we'll use JSON
        if(FileAccess.FileExists(_importPlusPath)) {
            System.Collections.Generic.Dictionary<string, ImportScriptParams> importScriptsParamsDict = new();

            var extendedClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(EditorScenePostImportPlus)));

            var classNames = extendedClasses.Select(c => c.Name);

            var duplicateClassNames = classNames.Where(n => classNames.Where(on => n == on).Count() > 1);

            if(duplicateClassNames.Any()) {
                GD.PrintErr($"Failed to import. Duplicate class names extending \"{nameof(EditorScenePostImportPlus)}\": ", duplicateClassNames);
                return;
            }

            using(var file = FileAccess.Open(_importPlusPath, FileAccess.ModeFlags.Read)) {
                // importScriptsParams = JsonSerializer.Deserialize<List<ImportScriptParams>>(file.GetAsText());
                while(file.GetPosition() < file.GetLength()) {
                    var parameterAssignmentLine = file.GetLine();
                    var parameterAssignment = parameterAssignmentLine.Split("=");
                    
                    var pathParameter = parameterAssignment[0];
                    var parameterValue = parameterAssignment[1];
                    
                    var parameterPathComponents = pathParameter.Split("/");
                    var scriptName = parameterPathComponents[0];
                    var parameterName = parameterPathComponents[1];

                    bool exists = importScriptsParamsDict.TryGetValue(scriptName, out ImportScriptParams importScriptParams);
                    
                    if(exists == false) {
                        importScriptParams = new ImportScriptParams() {
                            Name = scriptName,
                            ImportProperties = new List<CustomImportProperty>()
                        };

                        importScriptsParamsDict[scriptName] = importScriptParams;
                    }

                    var scriptType = extendedClasses.SingleOrDefault(c => c.Name.Equals(scriptName));

                    GD.Print("Scripts: ", string.Join(", ", extendedClasses.Select(c => c.Name)));

                    if(scriptType == null) {
                        GD.PrintErr($"Failed to find script for \"{scriptName}\", skipping");
                        continue;
                    }

                    var referencedProperty = scriptType.GetProperties()
                        .SingleOrDefault(p => Attribute.IsDefined(p, typeof(ExportAttribute)) && p.Name == parameterName);

                    if(referencedProperty == null) {
                        GD.PrintErr($"Failed to import \"{parameterName}\" for script \"{scriptName}\", it doesn't exist anymore, skipping");
                        continue;
                    }

                    importScriptParams.ImportProperties.Add(new CustomImportProperty() {
                        Name = parameterName,
                        Value = (Variant)typeof(Variant).GetMethod("From").MakeGenericMethod(referencedProperty.PropertyType).Invoke(null, new[] { Convert.ChangeType(parameterValue, referencedProperty.PropertyType) })
                    });
                }
            }

            importScriptsParams = importScriptsParamsDict.Values.ToList();
        } else {
            importScriptsParams = new List<ImportScriptParams>();
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

        foreach(var importScriptParams in importScriptsParams) {
            GD.Print("Import script: ", importScriptParams);
            foreach(var importScriptParam in importScriptParams.ImportProperties) {
                GD.Print(importScriptParam.Name, importScriptParam.Value);
                AddImportOption($"{importScriptParams.Name}:{importScriptParam.Name}", importScriptParam.Value);
            }
        }
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
                var optionKey = $"{s.GetType().Name}:{ip.Name}";
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

        // Godot actually stores this in its own way for some reason, see editor_file_system.cpp::_reimport_group().
        // To avoid adding unknown complexity, we'll use JSON
        using(var file = FileAccess.Open(_importPlusPath, FileAccess.ModeFlags.Write)) {
            // file.StoreString(JsonSerializer.Serialize(defaultImportScriptsParams));

            foreach(var defaultImportScriptParams in defaultImportScriptsParams) {
                foreach(var defaultImportScriptParam in defaultImportScriptParams.ImportProperties) {
                    file.StoreString($"{defaultImportScriptParams.Name}/{defaultImportScriptParam.Name}={defaultImportScriptParam.Value}");
                }
            }
        }

        // foreach(var importScript in importScripts) {
        //     GD.Print("Import script: ", importScript);
        //     foreach(var importProp in importScript.GetImportProps()) {
        //         GD.Print(importProp.Name, importProp.DefaultValue);
        //         GD.Print("Got value for ", importProp.Name, GetOptionValue(importProp.Name));
        //     }
        // }
    }



}
