namespace SmartDocs.Api.Services;

/// <summary>Stockage des fichiers sur le disque du serveur (noms générés, jamais ceux de l'utilisateur).</summary>
public class FileStorage
{
    private readonly string _root;

    public FileStorage(IConfiguration config, IHostEnvironment env)
    {
        _root = Path.GetFullPath(config["Storage:Path"] ?? "Storage", env.ContentRootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream source, string extension)
    {
        var name = $"{Guid.NewGuid():N}.{extension}";
        await using var target = File.Create(Path.Combine(_root, name));
        await source.CopyToAsync(target);
        return name;
    }

    public string SaveBytes(byte[] data, string extension)
    {
        var name = $"{Guid.NewGuid():N}.{extension}";
        File.WriteAllBytes(Path.Combine(_root, name), data);
        return name;
    }

    /// <summary>Copie un fichier existant (restauration d'une version) et retourne le nouveau nom, ou null.</summary>
    public string? Copy(string storedName, string extension)
    {
        var source = PathOf(storedName);
        if (source == null) return null;
        var name = $"{Guid.NewGuid():N}.{extension}";
        File.Copy(source, Path.Combine(_root, name));
        return name;
    }

    /// <summary>Supprime un fichier stocké (sans erreur s'il n'existe plus).</summary>
    public void Delete(string storedName)
    {
        var path = PathOf(storedName);
        if (path == null) return;
        try { File.Delete(path); } catch (IOException) { /* fichier verrouillé : il restera orphelin */ }
    }

    public string? PathOf(string storedName)
    {
        var path = Path.Combine(_root, Path.GetFileName(storedName));
        return File.Exists(path) ? path : null;
    }
}
