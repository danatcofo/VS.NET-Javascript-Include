using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Core;

namespace JavasciptInclude
{
    class Program
    {

        static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            log.Info("========= Starting JavaScript Include Compilation =========");

            var root = Path.GetFullPath(args.FirstOrDefault(i => !i.StartsWith("-")) ?? ".");

            Compile.Comipiler.Compile(root);

            log.Info("========= EOF =========");
            if (args.Contains("-p"))
                Console.ReadKey();
            Environment.Exit(0);
        }


    }
}
