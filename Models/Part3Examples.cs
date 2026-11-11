using System;
using System.Collections.Generic;
using System.Text;

namespace third.Models;

// ============================================================================
// 1. СПИСОК: Плейлист с навигацией (СВОЙ двусвязный список)
// ============================================================================
public class PlaylistNode
{
    public string TrackTitle { get; set; }
    public string Artist { get; set; }
    public string Duration { get; set; }
    public PlaylistNode? Prev { get; set; }
    public PlaylistNode? Next { get; set; }

    public PlaylistNode(string title, string artist, string duration)
    {
        TrackTitle = title;
        Artist = artist;
        Duration = duration;
    }

    public override string ToString() => $"{Artist} — {TrackTitle} ({Duration})";
}

public class DoublyLinkedPlaylist
{
    public PlaylistNode? Head { get; private set; }
    public PlaylistNode? Tail { get; private set; }
    public PlaylistNode? Current { get; private set; }
    public int Count { get; private set; }

    public DoublyLinkedPlaylist()
    {
        AddTrack("Bohemian Rhapsody", "Queen", "5:55");
        AddTrack("Smells Like Teen Spirit", "Nirvana", "5:01");
        AddTrack("Starlight", "Muse", "4:00");
        AddTrack("Lose Yourself", "Eminem", "5:26");
        AddTrack("Hotel California", "Eagles", "6:30");
        Current = Head;
    }

    public void AddTrack(string title, string artist, string duration)
    {
        var node = new PlaylistNode(title, artist, duration);
        if (Head == null)
        {
            Head = Tail = Current = node;
        }
        else
        {
            Tail!.Next = node;
            node.Prev = Tail;
            Tail = node;
        }
        Count++;
    }

    public bool MoveNext()
    {
        if (Current?.Next != null)
        {
            Current = Current.Next;
            return true;
        }
        return false;
    }

    public bool MovePrev()
    {
        if (Current?.Prev != null)
        {
            Current = Current.Prev;
            return true;
        }
        return false;
    }

    public bool RemoveCurrent()
    {
        if (Current == null) return false;

        var toRemove = Current;
        if (toRemove.Prev != null)
            toRemove.Prev.Next = toRemove.Next;
        else
            Head = toRemove.Next;

        if (toRemove.Next != null)
            toRemove.Next.Prev = toRemove.Prev;
        else
            Tail = toRemove.Prev;

        Current = toRemove.Next ?? toRemove.Prev;
        Count--;
        return true;
    }

    public void Shuffle()
    {
        if (Count <= 1) return;

        var nodes = new List<PlaylistNode>();
        var cur = Head;
        while (cur != null)
        {
            nodes.Add(cur);
            cur = cur.Next;
        }

        var rng = new Random();
        for (int i = nodes.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (nodes[i], nodes[j]) = (nodes[j], nodes[i]);
        }

        // Пересобираем ссылки двусвязного списка
        Head = nodes[0];
        Head.Prev = null;
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Prev = i > 0 ? nodes[i - 1] : null;
            nodes[i].Next = i < nodes.Count - 1 ? nodes[i + 1] : null;
        }
        Tail = nodes[^1];
        Current = Head;
    }

    public List<(string Text, bool IsCurrent)> GetSnapshot()
    {
        var list = new List<(string, bool)>();
        var cur = Head;
        int idx = 1;
        while (cur != null)
        {
            bool isCur = ReferenceEquals(cur, Current);
            string prefix = isCur ? "▶ [ИГРАЕТ] " : $"   {idx}. ";
            list.Add(($"{prefix}{cur}", isCur));
            cur = cur.Next;
            idx++;
        }
        return list;
    }
}

// ============================================================================
// 2. СТЕК (LIFO): История браузера (два стека «Назад» и «Вперёд»)
// ============================================================================
public class BrowserHistoryEngine
{
    public CustomStack<string> BackStack { get; } = new();
    public CustomStack<string> ForwardStack { get; } = new();
    public string CurrentUrl { get; private set; } = "https://avaloniaui.net";

    public BrowserHistoryEngine()
    {
        Visit("https://github.com");
        Visit("https://learn.microsoft.com/dotnet");
        Visit("https://habr.com/ru/articles/");
    }

    public void Visit(string url)
    {
        if (!string.IsNullOrEmpty(CurrentUrl))
            BackStack.Push(CurrentUrl);

        CurrentUrl = url;
        ForwardStack.Clear();
    }

    public bool GoBack()
    {
        if (BackStack.IsEmpty()) return false;
        ForwardStack.Push(CurrentUrl);
        CurrentUrl = BackStack.Pop();
        return true;
    }

    public bool GoForward()
    {
        if (ForwardStack.IsEmpty()) return false;
        BackStack.Push(CurrentUrl);
        CurrentUrl = ForwardStack.Pop();
        return true;
    }
}

// ============================================================================
// 3. ОЧЕРЕДЬ: Спулер печати с приоритетами и отменой заданий
// ============================================================================
public class PrintJobNode
{
    public int JobId { get; set; }
    public string DocumentName { get; set; }
    public int Pages { get; set; }
    public int Priority { get; set; } // 1 - Высокий, 2 - Обычный, 3 - Низкий
    public PrintJobNode? Next { get; set; }

    public PrintJobNode(int jobId, string docName, int pages, int priority)
    {
        JobId = jobId;
        DocumentName = docName;
        Pages = pages;
        Priority = priority;
    }

    public string PriorityText => Priority switch
    {
        1 => "Высокий (Срочно)",
        2 => "Обычный",
        _ => "Низкий"
    };

    public override string ToString() =>
        $"#{JobId} [{PriorityText}] \"{DocumentName}\" ({Pages} стр.)";
}

public class PrintSpoolerEngine
{
    private PrintJobNode? _head;
    private int _nextId = 1;
    public int Count { get; private set; }

    public PrintSpoolerEngine()
    {
        EnqueueJob("Отчет_ПЗ3_Динамические_структуры.pdf", 14, 2);
        EnqueueJob("Договор_срочно.docx", 3, 1);
        EnqueueJob("Черновик_графика.png", 1, 3);
        EnqueueJob("Ведомость_группы.xlsx", 2, 1);
    }

    public PrintJobNode EnqueueJob(string docName, int pages, int priority)
    {
        var newNode = new PrintJobNode(_nextId++, docName, pages, priority);

        // Вставка в очередь согласно приоритету (1 раньше 2, 2 раньше 3; внутри одного приоритета — FIFO)
        if (_head == null || newNode.Priority < _head.Priority)
        {
            newNode.Next = _head;
            _head = newNode;
        }
        else
        {
            var cur = _head;
            while (cur.Next != null && cur.Next.Priority <= newNode.Priority)
            {
                cur = cur.Next;
            }
            newNode.Next = cur.Next;
            cur.Next = newNode;
        }

        Count++;
        return newNode;
    }

    public PrintJobNode? PrintNext()
    {
        if (_head == null) return null;
        var printed = _head;
        _head = _head.Next;
        Count--;
        return printed;
    }

    public bool CancelJob(int jobId)
    {
        if (_head == null) return false;
        if (_head.JobId == jobId)
        {
            _head = _head.Next;
            Count--;
            return true;
        }

        var cur = _head;
        while (cur.Next != null)
        {
            if (cur.Next.JobId == jobId)
            {
                cur.Next = cur.Next.Next;
                Count--;
                return true;
            }
            cur = cur.Next;
        }
        return false;
    }

    public List<PrintJobNode> GetQueueSnapshot()
    {
        var list = new List<PrintJobNode>();
        var cur = _head;
        while (cur != null)
        {
            list.Add(cur);
            cur = cur.Next;
        }
        return list;
    }
}

// ============================================================================
// 4. ДЕРЕВЬЯ: Префиксное дерево (Trie) для автодополнения поиска
// ============================================================================
public class TrieNode
{
    public Dictionary<char, TrieNode> Children { get; } = new();
    public bool IsEndOfWord { get; set; }
}

public class TrieAutocompleteEngine
{
    private readonly TrieNode _root = new();
    public int WordCount { get; private set; }

    public TrieAutocompleteEngine()
    {
        string[] initialWords =
        {
            "алгоритм", "аллокатор", "абстракция", "асимптотика",
            "стек", "список", "структура", "сегмент", "сортировка", "спулер",
            "дерево", "динамический", "двусвязный", "дейкстра",
            "очередь", "односвязный", "операция", "оценка",
            "постфиксный", "префиксный", "плейлист", "память", "поиск"
        };
        foreach (var w in initialWords)
            Insert(w);
    }

    public bool Insert(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;
        word = word.Trim().ToLowerInvariant();

        var cur = _root;
        foreach (char ch in word)
        {
            if (!cur.Children.TryGetValue(ch, out var next))
            {
                next = new TrieNode();
                cur.Children[ch] = next;
            }
            cur = next;
        }

        if (!cur.IsEndOfWord)
        {
            cur.IsEndOfWord = true;
            WordCount++;
            return true;
        }
        return false;
    }

    public List<string> Autocomplete(string prefix)
    {
        var results = new List<string>();
        prefix = (prefix ?? "").Trim().ToLowerInvariant();

        var cur = _root;
        foreach (char ch in prefix)
        {
            if (!cur.Children.TryGetValue(ch, out var next))
                return results; // префикс не найден
            cur = next;
        }

        CollectWords(cur, new StringBuilder(prefix), results);
        return results;
    }

    private void CollectWords(TrieNode node, StringBuilder currentPrefix, List<string> results)
    {
        if (node.IsEndOfWord)
            results.Add(currentPrefix.ToString());

        foreach (var kvp in node.Children)
        {
            currentPrefix.Append(kvp.Key);
            CollectWords(kvp.Value, currentPrefix, results);
            currentPrefix.Length--;
        }
    }
}

// ============================================================================
// ДОПОЛНИТЕЛЬНО (Скриншот 2024): Построение бинарного дерева из ABD*G***CE**FH**J**
// ============================================================================
public class BinaryTreeNode
{
    public char Value { get; set; }
    public BinaryTreeNode? Left { get; set; }
    public BinaryTreeNode? Right { get; set; }

    public BinaryTreeNode(char value)
    {
        Value = value;
    }
}

public static class SerializedBinaryTree
{
    /// <summary>
    /// Строит бинарное дерево по префиксной строке с символом '*' для NULL
    /// Пример со скриншота: ABD*G***CE**FH**J**
    /// </summary>
    public static BinaryTreeNode? ParsePreOrder(string serialized)
    {
        int index = 0;
        return ParseHelper(serialized.Trim(), ref index);
    }

    private static BinaryTreeNode? ParseHelper(string s, ref int idx)
    {
        if (idx >= s.Length)
            return null;

        char c = s[idx++];
        if (c == '*')
            return null;

        var node = new BinaryTreeNode(c);
        node.Left = ParseHelper(s, ref idx);
        node.Right = ParseHelper(s, ref idx);
        return node;
    }

    public static string FormatTreeAscii(BinaryTreeNode? root)
    {
        if (root == null) return "[Пустое дерево (NULL)]";
        var sb = new StringBuilder();
        BuildAscii(root, "", true, "КОРЕНЬ: ", sb);
        return sb.ToString();
    }

    private static void BuildAscii(BinaryTreeNode? node, string indent, bool isLast, string branchLabel, StringBuilder sb)
    {
        sb.Append(indent);
        sb.Append(isLast ? "└── " : "├── ");
        sb.Append(branchLabel);

        if (node == null)
        {
            sb.AppendLine("NULL (*)");
            return;
        }

        string leftInfo = node.Left != null ? node.Left.Value.ToString() : "NULL";
        string rightInfo = node.Right != null ? node.Right.Value.ToString() : "NULL";
        sb.AppendLine($"[{node.Value}]  (Left: {leftInfo} | Right: {rightInfo})");

        string childIndent = indent + (isLast ? "    " : "│   ");
        if (node.Left != null || node.Right != null)
        {
            BuildAscii(node.Left, childIndent, false, "L: ", sb);
            BuildAscii(node.Right, childIndent, true, "R: ", sb);
        }
    }

    public static string GetTraversalsReport(BinaryTreeNode? root)
    {
        var pre = new List<char>();
        var inOrder = new List<char>();
        var post = new List<char>();

        void Pre(BinaryTreeNode? n) { if (n == null) return; pre.Add(n.Value); Pre(n.Left); Pre(n.Right); }
        void In(BinaryTreeNode? n) { if (n == null) return; In(n.Left); inOrder.Add(n.Value); In(n.Right); }
        void Post(BinaryTreeNode? n) { if (n == null) return; Post(n.Left); Post(n.Right); post.Add(n.Value); }

        Pre(root);
        In(root);
        Post(root);

        var sb = new StringBuilder();
        sb.AppendLine($"• Прямой обход (Pre-order, К-Л-П):   {string.Join(" -> ", pre)}");
        sb.AppendLine($"• Центрированный (In-order, Л-К-П):  {string.Join(" -> ", inOrder)}");
        sb.AppendLine($"• Обратный обход (Post-order, Л-П-К): {string.Join(" -> ", post)}");
        return sb.ToString();
    }
}
