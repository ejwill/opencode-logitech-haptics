namespace Loupedeck
{
    using System;
    using System.IO;
    using System.Reflection;

    public abstract class Plugin
    {
        public virtual Boolean UsesApplicationApiOnly => false;
        public virtual Boolean HasNoApplication => false;
        public PluginLogFile Log { get; } = new();
        public Assembly Assembly => this.GetType().Assembly;
        public PluginEvents PluginEvents { get; } = new();
        public ActionEditorCommandCollection ActionEditorCommands { get; } = new();
        public Boolean TryGetPluginSetting(String settingName, out String settingValue)
        {
            settingValue = null;
            return false;
        }
        public void SetPluginSetting(String settingName, String settingValue, Boolean backupOnline) { }
        public virtual void Load() { }
        public virtual void Unload() { }
    }

    public sealed class PluginEvents
    {
        public void AddEvent(String name, String displayName, String description) { }
        public void RaiseEvent(String name) { }
    }

    public abstract class PluginDynamicCommand
    {
        protected PluginDynamicCommand(String displayName, String description, String groupName) { }
        public Plugin Plugin { get; set; } = new StubPlugin();
        protected virtual Boolean OnLoad() => true;
        protected abstract void RunCommand(String actionParameter);
        private sealed class StubPlugin : Plugin { }
    }

    public abstract class ActionEditorCommand
    {
        protected ActionEditorCommand() { }
        public String Name { get; set; }
        public String DisplayName { get; set; }
        public String GroupName { get; set; }
        public String Description { get; set; }
        public ActionEditor ActionEditor { get; } = new();
        public Plugin Plugin { get; set; } = new StubPlugin();
        protected abstract Boolean RunCommand(ActionEditorActionParameters actionParameters);
        private sealed class StubPlugin : Plugin { }
    }

    public sealed class ActionEditorCommandCollection
    {
        public void AddAction(ActionEditorCommand action) { }
        public void Clear() { }
    }

    public sealed class ActionEditor
    {
        public void AddControlEx(ActionEditorControl control) { }
        public event EventHandler<ActionEditorListboxItemsRequestedEventArgs> ListboxItemsRequested;
        public event EventHandler<ActionEditorControlsStateRequestedEventArgs> ControlsStateRequested;
        public event EventHandler<ActionEditorControlValueChangedEventArgs> ControlValueChanged;
    }

    public abstract class ActionEditorControl
    {
        protected ActionEditorControl(String name, String labelText) { }
        public ActionEditorControl SetDefaultValue(Boolean value) => this;
    }

    public sealed class ActionEditorCheckbox : ActionEditorControl
    {
        public ActionEditorCheckbox(String name, String labelText) : base(name, labelText) { }
    }

    public sealed class ActionEditorListbox : ActionEditorControl
    {
        public ActionEditorListbox(String name, String labelText, String description) : base(name, labelText) { }
    }

    public sealed class ActionEditorListboxItemsRequestedEventArgs : EventArgs
    {
        public String ControlName { get; set; }
        public void AddItem(String name, String displayName, String description) { }
        public void SetSelectedItemName(String name) { }
    }

    public sealed class ActionEditorControlsStateRequestedEventArgs : EventArgs
    {
        public ActionEditorState ActionEditorState { get; } = new();
    }

    public sealed class ActionEditorControlValueChangedEventArgs : EventArgs
    {
        public String ControlName { get; set; }
        public ActionEditorState ActionEditorState { get; } = new();
    }

    public sealed class ActionEditorState
    {
        private readonly Dictionary<String, String> _values = new();
        public String GetControlValue(String name) => this._values.TryGetValue(name, out var value) ? value : null;
        public void SetValue(String name, String value) => this._values[name] = value;
        public void SetDisplayName(String value) { }
    }

    public sealed class ActionEditorActionParameters
    {
        public Boolean TryGetBoolean(String name, out Boolean value)
        {
            value = true;
            return false;
        }
        public Boolean TryGetString(String name, out String value)
        {
            value = null;
            return false;
        }
    }

    public abstract class ClientApplication
    {
        protected abstract String GetProcessName();
        protected abstract String GetBundleName();
        public abstract ClientApplicationStatus GetApplicationStatus();
    }

    public enum ClientApplicationStatus { Unknown }

    public sealed class PluginLogFile
    {
        public void Verbose(String text) { }
        public void Verbose(Exception ex, String text) { }
        public void Info(String text) { }
        public void Info(Exception ex, String text) { }
        public void Warning(String text) { }
        public void Warning(Exception ex, String text) { }
        public void Error(String text) { }
        public void Error(Exception ex, String text) { }
    }

    public sealed class BitmapImage { }

    public static class LoupedeckExtensions
    {
        public static void CheckNullArgument(this Object value, String name)
        {
            if (value is null) throw new ArgumentNullException(name);
        }

        public static String[] GetFilesInFolder(this Assembly assembly, String folderName) => Array.Empty<String>();
        public static String FindFileOrThrow(this Assembly assembly, String fileName) => fileName;
        public static String[] FindFiles(this Assembly assembly, String regexPattern) => Array.Empty<String>();
        public static Stream GetStream(this Assembly assembly, String resourceName) => Stream.Null;
        public static String ReadTextFile(this Assembly assembly, String resourceName) => String.Empty;
        public static Byte[] ReadBinaryFile(this Assembly assembly, String resourceName) => Array.Empty<Byte>();
        public static BitmapImage ReadImage(this Assembly assembly, String resourceName) => new();
        public static void ExtractFile(this Assembly assembly, String resourceName, String filePathName) { }
    }
}
