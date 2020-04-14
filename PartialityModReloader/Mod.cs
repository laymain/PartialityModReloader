using System.IO;
using System.Linq;
using System.Reflection;
using Partiality.Modloader;

namespace PartialityModReloader
{
    public class Mod : PartialityMod
    {
        private static readonly string ModsFolder = Path.GetFullPath(
            new[] {new FileInfo(Assembly.GetCallingAssembly().Location).DirectoryName, "..", "..", "Mods"}.Aggregate(Path.Combine)
        );

        private Reloader _reloader;

        public Mod()
        {
            ModID = nameof(PartialityModReloader);
            Version = typeof(Mod).Assembly.GetName().Version.ToString();
            author = "Laymain";
        }

        public override void OnEnable()
        {
            _reloader = new Reloader(ModsFolder);
            base.OnEnable();
        }

        public override void OnDisable()
        {
            _reloader?.Dispose();
            _reloader = null;
            base.OnDisable();
        }
    }
}
