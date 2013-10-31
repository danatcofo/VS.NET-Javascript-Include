using System.Collections.Generic;
using System.IO;

namespace JavasciptInclude
{
    public class JavascriptFile: DependentFile
    {
        public FileInfo Target { get; set; }
    }

    public class DependentFile
    {
        public FileInfo Source { get; set; }
        private readonly List<DependentFile> _dependencies = new List<DependentFile>();
        public List<DependentFile> Dependencies { get { return _dependencies; } }
        public int CompileOrder { get; set; }
        public DependencyType DependencyType { get; set; }
        public string ErrorString { get; set;}
        public int ParentLineNumber { get; set; }

    }

    public enum DependencyType
    {
        Uknown, // hasn't been processed yet
        Direct, // needed by the parent 
        None, // no dependencies
        Error, // unable to process it
    }

    public class FileReference
    {
        public DependentFile File { get; set; }
        public int Level { get; set; }
    }
}