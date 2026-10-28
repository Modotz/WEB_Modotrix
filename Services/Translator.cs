using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ERP.Web.Services;

/// <summary>
/// Penerjemah UI Indonesia ⇄ Inggris. Teks Indonesia adalah KUNCI; terjemahan Inggris ada di
/// <c>Resources/i18n/en/*.json</c> (objek { "teks Indonesia": "English text" }).
/// Kunci boleh berisi placeholder {0}, {1}, … — dipakai untuk pesan dinamis (mis. dari API):
/// "Jurnal {0} diposting." cocok dengan "Jurnal JV-261024001 diposting." → "Journal JV-261024001 posted."
/// Teks yang belum ada terjemahannya ditampilkan apa adanya (bahasa Indonesia) dan dicatat ke log.
/// Bahasa aktif = CultureInfo.CurrentUICulture (cookie bahasa, lihat LanguageController).
/// </summary>
public class Translator
{
    private readonly Dictionary<string, string> _exact = new(StringComparer.Ordinal);
    private readonly List<(Regex Pattern, string Template)> _templates = new();
    private readonly ConcurrentDictionary<string, byte> _missing = new();
    private readonly ILogger<Translator> _logger;

    public Translator(IWebHostEnvironment env, ILogger<Translator> logger)
    {
        _logger = logger;
        Load(Path.Combine(env.ContentRootPath, "Resources", "i18n", "en"));
    }

    /// <summary>"id" atau "en".</summary>
    public string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? "en" : "id";

    public bool IsEnglish => Lang == "en";

    /// <summary>Terjemahkan teks (kunci = teks Indonesia).</summary>
    public string this[string? text] => Translate(text);

    /// <summary>Terjemahkan teks berplaceholder lalu isi argumennya: T["Jurnal {0} dihapus.", no].</summary>
    public string this[string text, params object?[] args] =>
        string.Format(CultureInfo.CurrentCulture, Translate(text), args);

    /// <summary>Judul menu: title_en bila bahasa Inggris dan tersedia.</summary>
    public string Menu(string title, string? titleEn) =>
        IsEnglish && !string.IsNullOrWhiteSpace(titleEn) ? titleEn! : IsEnglish ? Translate(title) : title;

    /// <summary>Kunci yang diminta tetapi belum diterjemahkan (sejak aplikasi start).</summary>
    public IReadOnlyCollection<string> Missing => _missing.Keys.OrderBy(k => k).ToList();

    public string Translate(string? text) => Translate(text, logMissing: true);

    private string Translate(string? text, bool logMissing)
    {
        if (string.IsNullOrEmpty(text) || !IsEnglish)
            return text ?? "";

        var key = Normalize(text);

        if (_exact.TryGetValue(key, out var en))
            return en;

        foreach (var (pattern, template) in _templates)
        {
            var m = pattern.Match(key);
            if (!m.Success)
                continue;

            // Isi placeholder (angka, nama, nomor dokumen) diterjemahkan bila ada, tetapi tidak dicatat sebagai "belum ada".
            var values = m.Groups.Cast<Group>().Skip(1).Select(g => (object)Translate(g.Value, logMissing: false)).ToArray();
            return string.Format(CultureInfo.InvariantCulture, template, values);
        }

        // Teks tanpa huruf (angka, tanda baca, {0}) tidak perlu diterjemahkan.
        if (logMissing && key.Any(char.IsLetter) && !Regex.IsMatch(key, @"^\{\d+\}$") && _missing.TryAdd(key, 0))
            _logger.LogDebug("Terjemahan belum ada: {Key}", key);

        return text;
    }

    private void Load(string folder)
    {
        if (!Directory.Exists(folder))
            return;

        foreach (var file in Directory.GetFiles(folder, "*.json").OrderBy(f => f))
        {
            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? new();

                foreach (var (id, en) in map)
                {
                    var key = Normalize(id);

                    if (Regex.IsMatch(key, @"\{\d+\}"))
                    {
                        // Template: bagian literal di-escape, {n} menjadi grup tangkap.
                        var pattern = "^" + Regex.Replace(Regex.Escape(key), @"\\\{(\d+)\}", "(.+?)") + "$";
                        _templates.Add((new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline), en));
                    }
                    else
                    {
                        _exact[key] = en;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "File terjemahan tidak valid: {File}", file);
            }
        }

        // Template yang lebih spesifik (lebih panjang) dicoba lebih dulu.
        _templates.Sort((a, b) => b.Pattern.ToString().Length.CompareTo(a.Pattern.ToString().Length));
        _logger.LogInformation("Terjemahan EN dimuat: {Exact} teks, {Templates} template.", _exact.Count, _templates.Count);
    }

    private static string Normalize(string text) => Regex.Replace(text.Trim(), @"\s+", " ");
}
