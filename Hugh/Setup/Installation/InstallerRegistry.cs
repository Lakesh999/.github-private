namespace Hugh.Setup.Installation;

/// <summary>
/// Registry that manages installers and selects the best available one for each prerequisite.
/// Installers are evaluated in priority order: WingetInstaller > ManualInstaller.
/// </summary>
public class InstallerRegistry : IInstallerRegistry
{
    private readonly IReadOnlyList<IInstaller> _installers;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallerRegistry"/> class.
    /// </summary>
    /// <param name="installers">
    /// The collection of installers to manage. These should be registered in priority order
    /// (highest priority first). Typically: WingetInstaller, then ManualInstaller.
    /// </param>
    public InstallerRegistry(IEnumerable<IInstaller> installers)
    {
        _installers = installers?.ToList() ?? throw new ArgumentNullException(nameof(installers));
    }

    /// <inheritdoc />
    public async Task<IInstaller?> GetInstallerAsync(string prerequisiteId, CancellationToken ct = default)
    {
        foreach (var installer in _installers)
        {
            if (!installer.CanInstall(prerequisiteId))
            {
                continue;
            }

            if (await installer.IsAvailableAsync(ct))
            {
                return installer;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IInstaller>> GetAvailableInstallersAsync(string prerequisiteId, CancellationToken ct = default)
    {
        var availableInstallers = new List<IInstaller>();

        foreach (var installer in _installers)
        {
            if (!installer.CanInstall(prerequisiteId))
            {
                continue;
            }

            if (await installer.IsAvailableAsync(ct))
            {
                availableInstallers.Add(installer);
            }
        }

        return availableInstallers;
    }
}
