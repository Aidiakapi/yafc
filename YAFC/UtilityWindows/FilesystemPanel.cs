using System;
using System.Drawing;
using System.IO;
using System.Linq;
using YAFC.UI;

namespace YAFC
{
    public class FilesystemPanel : Window
    {
        private enum EntryType {Drive, Directory, ParentDirectory, CreateDirectory, File}
        
        private readonly FontString description;
        private readonly InputField location;
        private readonly InputField fileName;
        private readonly VirtualScrollList<(EntryType type, string location), EntryView> entries;
        private readonly string defaultFileNameText;
        private readonly bool allowCreate;
        private readonly string extension;
        private readonly TextButton selectButton;

        private string currentLocation;
        private string selectedResult;
        
        public FilesystemPanel(string header, string description, string button, string location, bool allowCreate, string extension, string defaultFileName)
        {
            this.description = new FontString(Font.text, description, true);
            this.location = new InputField(Font.text) {text = location};
            this.entries = new VirtualScrollList<(EntryType type, string location), EntryView>(new SizeF(10, 30), 1.5f);
            this.fileName = new InputField(Font.text) {text = defaultFileName};
            this.defaultFileNameText = defaultFileName;
            this.allowCreate = allowCreate;
            this.extension = extension;
            this.selectButton = new TextButton(Font.text, button, SelectClick);
            SetLocation(Directory.Exists(location) ? location : "");
            
            Create(header, 30f, true);
        }

        private void SelectClick()
        {
            
        }

        protected override LayoutPosition BuildContent(RenderBatch batch, LayoutPosition location)
        {
            location.Build(description, batch);
            location.Build(this.location, batch);
            location.Build(entries, batch);
            location.Build(fileName, batch);
            location.Build(selectButton, batch);
            return location;
        }

        private void SetLocation(string directory)
        {
            currentLocation = directory;
            location.text = directory;
            fileName.text = defaultFileNameText;
            if (string.IsNullOrEmpty(directory))
            {
                entries.data = Directory.GetLogicalDrives().Select(x => (EntryType.Drive, x)).ToArray();
            }
            else
            {
                if (!Directory.Exists(directory))
                {
                    location.text = currentLocation;
                    return;
                }
                var data = Directory.EnumerateDirectories(directory).Select(x => (EntryType.Directory, x));
                if (allowCreate)
                    data = data.Append((EntryType.CreateDirectory, directory));
                var parent = Directory.GetParent(directory)?.FullName ?? "";
                data = data.Prepend((EntryType.ParentDirectory, parent));
                if (extension != null)
                    data = data.Concat(Directory.GetFiles(directory, "*."+extension).Select(x => (EntryType.File, x)));
                entries.data = data.ToArray();
            }

            UpdatePossibleResult();
            entries.scroll = 0;
        }
        
        private void SelectFile(string path)
        {
            fileName.text = Path.GetFileName(path);
            UpdatePossibleResult();
        }
        
        public void UpdatePossibleResult()
        {
            if (extension == null)
            {
                selectedResult = currentLocation;
            }
            else
            {
                var filename = fileName.text;
                if (filename == "" || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    selectedResult = null;
                }
                else
                {
                    if (!filename.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase))
                        filename += "." + extension;
                    selectedResult = Path.Combine(currentLocation, filename);
                }
            }

            selectButton.interactable = selectedResult != null;
        }

        private class EntryView : SelectableElement<(EntryType type, string location)>
        {
            private readonly FontString text;

            public EntryView()
            {
                text = new FontString(Font.text);
            }

            private (Icon, string) GetDisplay()
            {
                switch (data.type)
                {
                    case EntryType.Directory: return (Icon.Folder, Path.GetFileName(data.location));
                    case EntryType.Drive: return (Icon.FolderOpen, data.location);
                    case EntryType.ParentDirectory: return (Icon.Upload, "..");
                    case EntryType.CreateDirectory: return (Icon.NewFolder, "[Create directory here]");
                    default: return (Icon.Settings, Path.GetFileName(data.location));
                }
            }
            public override void Click(RenderBatch batch)
            {
                var owner = batch.FindOwner<FilesystemPanel>();
                switch (data.type)
                {
                    case EntryType.Directory: case EntryType.Drive: case EntryType.ParentDirectory:
                        owner.SetLocation(data.location);
                        break;
                    case EntryType.File:
                        owner.SelectFile(data.location);
                        break;
                }
            }

            protected override LayoutPosition BuildContent(RenderBatch batch, LayoutPosition position)
            {
                var (icon, elementText) = GetDisplay();
                var iconArea = position.LeftRect(1f, 1f);
                batch.DrawIcon(iconArea, icon, SchemeColor.BackgroundText);
                text.BuildElement(elementText, batch, position);
                position.left -= 1f;
                position.y += 1f;
                return position;
            }
        }
    }
}