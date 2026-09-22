using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

// 与 WPF Shapes.Path 无引用冲突，仅为可读性取别名
using IoPath = System.IO.Path;

namespace GeneralControl.Controls;

/// <summary>
/// 设计期辅助：把控件实例上的属性变更写回项目源 *.xaml。
/// 仅 DEBUG 编译且附加调试器时生效，其余环境为纯空操作。
///
/// 【定位规则】
/// 1. 控件有 x:Name → 在所有 xaml 中找"同类型名 + 该 Name"的标签；
/// 2. 无名 → 按"类型名 + 指纹属性集合"唯一定位（指纹 = 改动前的旧值）；
/// 3. 命中 0 个或多个候选一律放弃并输出日志，绝不误改其他标签。
///
/// 【防抖】所有控件共享一个 0.5s 防抖计时器，停止操作后一次性批量写回；
/// 同一控件的多次调度只保留最新状态。
///
/// 【BOM】读入时剥掉 BOM 字节、按有无 BOM 决定写出编码，避免每次叠加
/// （历史上曾因此把文件开头堆出十几个 BOM 导致 XAML 解析报错）。
/// </summary>
internal static class GcDesignPersist
{
    /// <summary>
    /// 可视化编辑能力总开关：仅 DEBUG 编译且附加调试器时可用；
    /// 直接运行 exe（发布版 / 无调试器）时恒为 false，编辑手柄与交互全部禁用。
    /// </summary>
    public static bool IsEditingSupported
    {
#if DEBUG
        get => Debugger.IsAttached;
#else
        get => false;
#endif
    }

    /// <summary>某控件成功写回后触发（参数：控件、本次写入的属性集），供调用方同步定位指纹。</summary>
    public static event Action<FrameworkElement, IReadOnlyList<KeyValuePair<string, string>>>? WrittenBack;

    private sealed class PendingItem
    {
        public required List<KeyValuePair<string, string>> Updates;
        public required List<KeyValuePair<string, string>> Fingerprints;
    }

    private static readonly DispatcherTimer Timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private static readonly Dictionary<FrameworkElement, PendingItem> Pending = new();

    static GcDesignPersist()
    {
        Timer.Tick += (_, _) =>
        {
            Timer.Stop();
            Flush();
        };
    }

    /// <summary>调度一次回写。fingerprints 用于无名控件的标签定位（传改动前的旧值）。</summary>
    public static void Schedule(
        FrameworkElement element,
        List<KeyValuePair<string, string>> updates,
        List<KeyValuePair<string, string>> fingerprints)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            return;
        }

        Pending[element] = new PendingItem { Updates = updates, Fingerprints = fingerprints };
        Timer.Stop();
        Timer.Start();
#endif
    }

    private static void Flush()
    {
        // 快照后清空；写回期间若又有新改动会开启下一轮防抖
        var items = new List<(FrameworkElement El, PendingItem Item)>();
        foreach (var kv in Pending)
        {
            items.Add((kv.Key, kv.Value));
        }

        Pending.Clear();

        foreach (var (el, item) in items)
        {
            WriteNow(el, item);
        }
    }

    private static void WriteNow(FrameworkElement el, PendingItem item)
    {
        try
        {
            var projectDir = FindProjectDir();
            if (projectDir is null)
            {
                Debug.WriteLine("[GlassDesign] 未找到项目目录，跳过 XAML 回写");
                return;
            }

            var typeName = el.GetType().Name;
            var tagPattern = $@"<(\w+:)?{Regex.Escape(typeName)}\b";
            var label = typeName + (el.Name.Length > 0 ? $"#{el.Name}" : string.Empty);

            // 收集全部候选标签（跨所有 xaml），要求唯一命中才写
            var candidates = new List<(string File, Encoding Enc, int Start, int End, string Updated)>();
            foreach (var file in EnumerateProjectXamlFiles(projectDir))
            {
                var text = ReadTextPreserveBom(file.FullName, out var enc);
                foreach (Match m in Regex.Matches(text, tagPattern))
                {
                    var end = text.IndexOf("/>", m.Index, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        continue;
                    }

                    end += 2;
                    var tag = text.Substring(m.Index, end - m.Index);
                    if (!TagMatches(tag, el.Name, item.Fingerprints))
                    {
                        continue;
                    }

                    var updated = tag[..^2].TrimEnd();
                    foreach (var (name, value) in item.Updates)
                    {
                        updated = Regex.IsMatch(updated, $@"{Regex.Escape(name)}\s*=\s*""[^""]*""")
                            ? Regex.Replace(updated, $@"{Regex.Escape(name)}\s*=\s*""[^""]*""",
                                _ => $"{name}=\"{value}\"")
                            : $"{updated} {name}=\"{value}\"";
                    }

                    updated += " />";
                    candidates.Add((file.FullName, enc, m.Index, end, updated));
                }
            }

            switch (candidates.Count)
            {
                case 1:
                {
                    var (path, enc, start, end, updated) = candidates[0];
                    var text = ReadTextPreserveBom(path, out _);
                    text = text.Remove(start, end - start).Insert(start, updated);
                    File.WriteAllText(path, text, enc);
                    Debug.WriteLine($"[GlassDesign] 已写回 {IoPath.GetFileName(path)} : {label}");
                    WrittenBack?.Invoke(el, item.Updates);
                    break;
                }
                case 0:
                    Debug.WriteLine($"[GlassDesign] 未在 *.xaml 定位到 {label}，跳过回写");
                    break;
                default:
                    Debug.WriteLine($"[GlassDesign] {label} 命中 {candidates.Count} 个候选标签，为安全起见跳过回写");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[GlassDesign] XAML 回写失败: " + ex.Message);
        }
    }

    /// <summary>标签匹配：有名字按名字；无名时必须同时包含全部指纹属性旧值。</summary>
    private static bool TagMatches(
        string tag,
        string name,
        List<KeyValuePair<string, string>> fingerprints)
    {
        if (!string.IsNullOrEmpty(name))
        {
            return Regex.IsMatch(tag, $@"x:Name\s*=\s*""{Regex.Escape(name)}""");
        }

        foreach (var f in fingerprints)
        {
            if (!Regex.IsMatch(tag, $@"{Regex.Escape(f.Key)}\s*=\s*""{Regex.Escape(f.Value)}"""))
            {
                return false;
            }
        }

        return true;
    }

    private static DirectoryInfo? FindProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
            {
                return dir;
            }
        }

        return null;
    }

    private static IEnumerable<FileInfo> EnumerateProjectXamlFiles(DirectoryInfo projectDir)
    {
        var all = projectDir.GetFiles("*.xaml", SearchOption.AllDirectories);
        foreach (var f in all)
        {
            var p = f.FullName.ToLowerInvariant();
            if (!p.Contains("\\obj\\") && !p.Contains("\\bin\\"))
            {
                yield return f;
            }
        }
    }

    private static string ReadTextPreserveBom(string path, out Encoding encoding)
    {
        var bytes = File.ReadAllBytes(path);

        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        encoding = new UTF8Encoding(hasBom);

        // 关键：解码时剥掉 BOM 字节，否则写回时编码器再补一个，逐次叠加损坏文件
        return Encoding.UTF8.GetString(hasBom ? bytes[3..] : bytes);
    }
}



