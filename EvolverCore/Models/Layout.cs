using System.IO;

namespace EvolverCore.Models
{
    public class Layout
    {
        internal string Name { set; get; } = string.Empty;

        internal bool DirectoryExists
        {
            get {
                string dir = Path.Combine(Globals.Instance.LayoutDirectory, Name);
                return Directory.Exists(dir);
            }
        }

        internal void CreateDirectory()
        {
            string dir = Path.Combine(Globals.Instance.LayoutDirectory, Name);
            Directory.CreateDirectory(dir);
        }

        internal bool SerializationFileExists
        {
            get
            {
                return File.Exists(SerializationFileName);
            }
        }

        internal string SerializationFileName
        {
            get
            {
                return Path.Combine(Globals.Instance.LayoutDirectory, Name, $"{Name}_layout.xml");
            }
        }

        internal bool VMSerializationFileExists
        {
            get
            {
                return File.Exists(VMSerializationFileName);
            }
        }
        internal string VMSerializationFileName
        {
            get
            {
                return Path.Combine(Globals.Instance.LayoutDirectory, Name, $"{Name}_viewmodel.xml");
            }
        }
    }
}
