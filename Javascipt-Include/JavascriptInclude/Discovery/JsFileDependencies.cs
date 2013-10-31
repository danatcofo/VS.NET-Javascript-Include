using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace JavasciptInclude.Discovery
{
    public class JsFileDependencies
    {
        static readonly ILog log = LogManager.GetLogger(typeof(JsFileDependencies));

        internal static string FindFile(string path, string from)
        {
            var current = Directory.GetCurrentDirectory();
            try
            {
                if (!string.IsNullOrWhiteSpace(from) && !path.StartsWith("~\\"))
                    Directory.SetCurrentDirectory(from);
                if (path.StartsWith("~\\"))
                {
                    path = current + path.Substring(1);
                }
                return Path.GetFullPath(path);
            }
            finally
            {
                Directory.SetCurrentDirectory(current);
            }
        }

        internal static IEnumerable<DependentFile> GetDirectDependencies(StreamReader reader, string root, int lineNumber, Func<FileInfo, bool> isCircular, FileInfo parent)
        {
            while (!reader.EndOfStream)
            {
                DependentFile file = null;
                try
                {
                    var l = reader.ReadLine();

                    if (l == null) continue;
                    var line = l.Trim();
                    if (!line.StartsWith("//")) continue;
                    line = line.Remove(0, 2).Trim();
                    if (!line.StartsWith("#include ")) continue;
                    var filename = line.Remove(0, 9).Replace("\"", "").Replace("'", "").Trim();
                    var validFilename = true;
                    foreach (var c in Path.GetInvalidPathChars())
                    {
                        var badC = filename.IndexOf(c);
                        if (badC <= -1) continue;

                        log.ErrorFormat("{3} LN:{0} CLM:{1} => Invalid character => {2}", lineNumber, badC, l, parent.Name);
                        validFilename = false;
                        file = new DependentFile
                        {
                            DependencyType = DependencyType.Error,
                            ErrorString = string.Format("{0} => Invalid character @ column {1}", l, badC),
                            ParentLineNumber = lineNumber,
                        };
                        break;
                    }
                    if (validFilename)
                    {
                        filename = FindFile(filename.Replace("/", "\\"), root);
                        var fi = new FileInfo(filename);
                        if (!fi.Exists)
                        {
                            log.ErrorFormat("{2} LN:{0} => File does not exists \"{1}\"", lineNumber, fi.Name, parent.Name);
                            file = new DependentFile
                            {
                                DependencyType = DependencyType.Error,
                                ErrorString = string.Format("{0} => File does not exists", l),
                                ParentLineNumber = lineNumber,
                            };
                        }
                        else
                        {
                            log.DebugFormat("{2} LN:{0} => Find Dependencies for {1}", lineNumber, fi.Name, parent.Name);


                            if (isCircular(fi))
                            {
                                file = new DependentFile
                                {
                                    DependencyType = DependencyType.Error,
                                    ErrorString = string.Format("{0} => circular reference detected", l)
                                };
                            }
                            else
                            {
                                file = new DependentFile
                                {
                                    DependencyType = DependencyType.Direct,
                                    Source = fi,
                                    ParentLineNumber = lineNumber,
                                };
                                file.Dependencies.AddRange(GetDirectDependencies(fi.OpenText(), fi.Directory.FullName, 0, cr =>
                                {
                                    var reference = isCircular(cr) || cr.FullName == fi.FullName;
                                    if (reference) log.ErrorFormat("{2} LN:{3} => Circular Reference detected! \"{0}\", \"{1}\"", fi.Name, cr.Name, parent.Name, lineNumber);
                                    return reference;
                                }, fi));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format("LN:{0}", lineNumber), e);
                }
                finally { ++lineNumber; }
                yield return file;
            }
        }
    }
}
