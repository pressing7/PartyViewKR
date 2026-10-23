// Only the configuration marker interface is needed by these headless tests.
// The production project uses Dalamud's real interface.
namespace Dalamud.Configuration;
public interface IPluginConfiguration
{
    int Version { get; set; }
}
