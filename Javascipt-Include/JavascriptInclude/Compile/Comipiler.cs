using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace JavasciptInclude.Compile
{
    public class Comipiler
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Comipiler));

        public List<JavascriptFile> Files { get; private set; }
        public Dictionary<string, FileReference> fileRefs { get; private set; }
        public Dictionary<string, CompileOutput> compilation { get; private set; }

        public void Compile(IEnumerable<JavascriptFile> files)
        {
            Files = files as List<JavascriptFile> ?? files.ToList();

            BuildDependencyDictionary(Files);

            GetBaseRawSources();

            CompileRawSources();

            OutputToDestination();
        }

        private void OutputToDestination()
        {
            foreach (var javascriptFile in Files)
            {
                var source = string.Join("\n", compilation[javascriptFile.Source.FullName].Contents);
                var destination = javascriptFile.Target;

                if (!destination.Exists)
                {
                    log.DebugFormat("Creating path {0}", destination.Directory.FullName);
                    destination.Directory.Create();
                }
                log.DebugFormat("Writing File {0}", destination.FullName);
                File.WriteAllText(destination.FullName, source);
            }
        }

        private void CompileRawSources()
        {
            foreach (var key in compilation.Keys)
            {
                if (!fileRefs.ContainsKey(key) || fileRefs[key].File.Dependencies.Count == 0)
                    continue;

                var contents = compilation[key].Contents;

                foreach (var dependency in fileRefs[key].File.Dependencies)
                {
                    switch (dependency.DependencyType)
                    {
                        case DependencyType.Error:
                            contents[dependency.ParentLineNumber] = dependency.ErrorString;
                            break;
                        default:
                            contents[dependency.ParentLineNumber] = dependency.Args.Region ?
                                string.Format("// #region {0} \n{1}\n\n// #endregion {0}\n", 
                                    compilation[dependency.Source.FullName].FileName,
                                    compilation[dependency.Source.FullName].GetContents()) : 
                                compilation[dependency.Source.FullName].GetContents();
                            break;
                    }
                }
                if (contents.Length > 0 && contents[0].Trim().StartsWith("// #compile"))
                    contents[0] = string.Format("// compiled @ {0} by JavascriptInclude Compiler", DateTime.Now);

                log.DebugFormat("{0} => output: \n{1}", fileRefs[key].File.Source.Name, compilation[key].GetContents());
            }
        }

        private void GetBaseRawSources()
        {
            compilation = new Dictionary<string, CompileOutput>();

            // for logging
            StringBuilder str = new StringBuilder();


            foreach (var fileReference in fileRefs.Values.OrderByDescending(i => i.Level))
            {
                str.AppendLine(string.Format("{0}{1}",
                                             string.Format("{0}", fileReference.Level).PadRight(fileReference.Level * 2 + 5),
                                             fileReference.File.Source.FullName));

                compilation.Add(fileReference.File.Source.FullName,
                                new CompileOutput
                                    {
                                        File = fileReference.File.Source.FullName,
                                        FileName = fileReference.File.Source.Name,
                                        Contents = GetContents(fileReference.File.Source).ToArray(),
                                        Level = fileReference.Level,
                                    });
            }

            log.DebugFormat("File Dependency Order\n{0}", str);
        }

        private void BuildDependencyDictionary(List<JavascriptFile> Files)
        {
            fileRefs = Files
                .Where(i => i.DependencyType != DependencyType.Error)
                .ToDictionary(i => i.Source.FullName, i => new FileReference { File = i, Level = 0 });

            Action<IEnumerable<DependentFile>, int> processFiles = null;
            processFiles = (enumerable, level) =>
                {
                    foreach (var dependentFile in enumerable)
                    {
                        if (fileRefs.ContainsKey(dependentFile.Source.FullName))
                        {
                            var rec = fileRefs[dependentFile.Source.FullName];
                            if (rec.Level < level) rec.Level = level;
                        }
                        else
                            fileRefs.Add(dependentFile.Source.FullName,
                                         new FileReference { File = dependentFile, Level = level });
                        processFiles(dependentFile.Dependencies.Where(i => i.DependencyType != DependencyType.Error), level + 1);
                    }
                };

            foreach (var compileFiles in Files)
            {
                processFiles(compileFiles.Dependencies.Where(i => i.DependencyType != DependencyType.Error), 1);
            }
        }

        internal IEnumerable<string> GetContents(FileInfo file)
        {
            using (var reader = file.OpenText())
            {
                while (!reader.EndOfStream)
                {
                    yield return reader.ReadLine();
                }
            }
        }

        public static void Compile(string path)
        {
            var currentDir = Directory.GetCurrentDirectory();
            
            var dir = path;
            if (path.EndsWith(".js"))
            {
                if (path.EndsWith(".compile.js"))
                {
                    FileInfo fi = new FileInfo(path);
                    dir = fi.Directory.FullName;
                }
                else
                {
                    log.InfoFormat("Nothing to do");
                    return;
                }
            }
            
            log.InfoFormat("Setting Working Directory: {0}", dir);
            
            Directory.SetCurrentDirectory(dir);

            var discovery = new Discovery.CompilableJsFiles();
            var compiler = new Comipiler();

            compiler.Compile(discovery.GetFilesToCompile(path));
            
            Directory.SetCurrentDirectory(currentDir);
        }
    }

    public class CompileOutput
    {
        public string File { get; set; }
        public string FileName { get; set; }
        public string[] Contents { get; set; }
        public int Level { get; set; }
        public string GetContents()
        {
            return string.Join("\n", Contents);
        }
    }
}
