using Godot;
using System;

[Tool]
public partial class EditorScenePostImportApplier : EditorScenePostImport
{



    public override GodotObject _PostImport(Node scene)
    {
        GD.Print("And this");
        return base._PostImport(scene);
    }


}
