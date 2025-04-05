using Godot;
using System;
using System.Collections.Generic;

public partial class MyImporter : EditorScenePostImportPlus
{
    public override List<CustomImportProperty> GetImportProps()
    {
        return new List<CustomImportProperty>() {new CustomImportProperty() {Name = "test", Value = false}};
    }

    public override GodotObject _PostImport(Node scene)
    {
        throw new NotImplementedException();
    }

}
