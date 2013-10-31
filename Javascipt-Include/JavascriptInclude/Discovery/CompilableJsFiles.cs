using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace JavasciptInclude.Discovery
{
    public class CompilableJsFiles
    {
        static readonly ILog log = LogManager.GetLogger(typeof(CompilableJsFiles));

        internal IEnumerable<JavascriptFile> EnumerateDirectory(DirectoryInfo info, string parent = "\\")
        {
            // limit to js files of with .compile.js as the extension
            var files = info.EnumerateFiles("*.compile.js").ToList();
            var directories = info.EnumerateDirectories().ToList();

            log.InfoFormat("Searching {1}{0}", info.Name, parent);
            foreach (var f in files)
            {
                var file = ProcessFile(f);
                if (file != null) yield return file;
            }
            foreach (var jsf in directories
                .SelectMany(d => EnumerateDirectory(d, string.Format("{1}{0}\\", info.Name, parent))))
                yield return jsf;
        }

        internal JavascriptFile ProcessFile(FileInfo f)
        {
            using (var reader = f.OpenText())
            {
                var firstLine = reader.ReadLine();
                if (firstLine == null || !firstLine.Trim().StartsWith("// #compile")) return null;

                var destinationDir = firstLine.Remove(0, 11).Trim();
                var destinationName = "";
                // writing to the same folder as source
                if (string.IsNullOrWhiteSpace(destinationDir))
                {
                    // ReSharper disable PossibleNullReferenceException
                    destinationDir = f.Directory.FullName;
                    // ReSharper restore PossibleNullReferenceException
                    destinationName = string.Format("{0}.js", f.Name.Substring(0, f.Name.Length - 11));
                }
                // output location provided
                else
                {
                    destinationDir = Path.GetFullPath(destinationDir);
                    destinationName = Path.GetFileName(destinationDir);
                    // output file was not provided
                    if (!string.IsNullOrWhiteSpace(destinationName) && destinationName.EndsWith(".js"))
                        destinationDir = destinationDir.Remove(destinationDir.Length - (destinationName.Length + 1));
                    // output file was provided
                    else
                        destinationName = string.Format("{0}.js", f.Name.Substring(0, f.Name.Length - 11));
                }

                var fullDestination = string.Format("{0}\\{1}", destinationDir, destinationName);
                log.InfoFormat("{0} => {1}", f.Name, destinationName);

                var file = new JavascriptFile
                {
                    Source = f,
                    Target = new FileInfo(fullDestination),
                    DependencyType = DependencyType.Uknown,
                };

                file.Dependencies.AddRange(JsFileDependencies.GetDirectDependencies(reader, f.Directory.FullName, 1, cr => cr.FullName == f.FullName, f).Where(i => i != null));
                return file;
            }
        }

        public IEnumerable<JavascriptFile> GetFilesToCompile(string path)
        {
            if (path.EndsWith(".js"))
            {
                return EnumerateDirectory(new FileInfo(path));
            }
            return EnumerateDirectory(new DirectoryInfo(path));

        }

        internal IEnumerable<JavascriptFile> EnumerateDirectory(FileInfo fileInfo)
        {
            yield return ProcessFile(fileInfo);
        }
    }
}
