#if TOOLS
using Godot;
using System;

[Tool]
public partial class EditorScenePostImportPlusPlugin : EditorPlugin
{

	private static EditorScenePostImportPluginPlus _editorScenePostImportPluginPlus;

	public override void _EnterTree()
	{
		GD.Print("Enter tree");
		if(_editorScenePostImportPluginPlus == null) {
            _editorScenePostImportPluginPlus = new EditorScenePostImportPluginPlus();
        }

        AddScenePostImportPlugin(_editorScenePostImportPluginPlus);
	}

	public override void _ExitTree()
	{
		GD.Print("Exit tree");
		RemoveScenePostImportPlugin(_editorScenePostImportPluginPlus);
	}
}
#endif
