using Godot;
using System;
using System.Collections.Generic;

public abstract partial class EditorScenePostImportPlus : GDScript
{

    public abstract GodotObject _PostImport(Node scene);

    public abstract List<CustomImportProperty> GetImportProps();

}
