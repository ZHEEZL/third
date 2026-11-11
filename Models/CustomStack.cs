using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace third.Models;

public class StackNode<T>
{
    public T Value { get; set; }
    public StackNode<T>? Next { get; set; }

    public StackNode(T value, StackNode<T>? next = null)
    {
        Value = value;
        Next = next;
    }
}

/// <summary>
/// Часть 1: Стек на базе СВОЕГО связного списка.
/// Поддерживает операции: Push(elem), Pop(), Top(), isEmpty(), Print().
/// </summary>
public class CustomStack<T>
{
    private StackNode<T>? _head;
    private int _count;

    public int Count => _count;

    public void Push(T elem)
    {
        _head = new StackNode<T>(elem, _head);
        _count++;
    }

    public T Pop()
    {
        if (_head == null)
            throw new InvalidOperationException("Стек пуст (невозможно выполнить Pop).");

        T val = _head.Value;
        _head = _head.Next;
        _count--;
        return val;
    }

    public T Top()
    {
        if (_head == null)
            throw new InvalidOperationException("Стек пуст (невозможно выполнить Top).");

        return _head.Value;
    }

    public bool IsEmpty() => _head == null;

    public string Print()
    {
        if (_head == null)
            return "[Стек пуст]";

        var sb = new StringBuilder();
        var current = _head;
        while (current != null)
        {
            sb.Append(current.Value);
            if (current.Next != null)
                sb.Append(" -> ");
            current = current.Next;
        }
        return sb.ToString();
    }

    public void Clear()
    {
        _head = null;
        _count = 0;
    }

    /// <summary>
    /// Возвращает элементы стека снизу вверх (для наглядной отрисовки стакана)
    /// </summary>
    public List<T> ToBottomUpList()
    {
        var list = new List<T>();
        var current = _head;
        while (current != null)
        {
            list.Add(current.Value);
            current = current.Next;
        }
        list.Reverse();
        return list;
    }

    /// <summary>
    /// Возвращает элементы стека сверху вниз (от вершины)
    /// </summary>
    public List<T> ToTopDownList()
    {
        var list = new List<T>();
        var current = _head;
        while (current != null)
        {
            list.Add(current.Value);
            current = current.Next;
        }
        return list;
    }
}

public class StackBenchmarkResult
{
    public string DatasetName { get; set; } = "";
    public int OperationCount { get; set; }
    public string Composition { get; set; } = "";
    public double ReadTimeMs { get; set; }
    public double ExecutionTimeMs { get; set; }
    public double TotalTimeMs => ReadTimeMs + ExecutionTimeMs;
}

public static class StackCommandProcessor
{
    /// <summary>
    /// Выполняет команды из строки формата input.txt:
    /// 1,elem - Push(elem), 2 - Pop(), 3 - Top(), 4 - isEmpty(), 5 - Print()
    /// </summary>
    public static (string Log, double ExecMs) ExecuteCommands(
        string rawInput,
        CustomStack<string> stack,
        bool verboseLog = true)
    {
        var sb = new StringBuilder();
        var sw = Stopwatch.StartNew();

        var tokens = rawInput.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int step = 1;

        foreach (var token in tokens)
        {
            if (token.StartsWith("1,"))
            {
                string elem = token.Substring(2);
                stack.Push(elem);
                if (verboseLog)
                    sb.AppendLine($"[{step}] 1,{elem} -> Push(\"{elem}\"): элемент добавлен на вершину стека");
            }
            else
            {
                switch (token)
                {
                    case "2":
                        if (stack.IsEmpty())
                        {
                            if (verboseLog)
                                sb.AppendLine($"[{step}] 2 -> Pop(): Стек пуст!");
                        }
                        else
                        {
                            var popped = stack.Pop();
                            if (verboseLog)
                                sb.AppendLine($"[{step}] 2 -> Pop(): извлечен элемент \"{popped}\"");
                        }
                        break;

                    case "3":
                        if (stack.IsEmpty())
                        {
                            if (verboseLog)
                                sb.AppendLine($"[{step}] 3 -> Top(): Стек пуст!");
                        }
                        else
                        {
                            var top = stack.Top();
                            if (verboseLog)
                                sb.AppendLine($"[{step}] 3 -> Top(): вершина стека = \"{top}\"");
                        }
                        break;

                    case "4":
                        bool empty = stack.IsEmpty();
                        if (verboseLog)
                            sb.AppendLine($"[{step}] 4 -> isEmpty(): {(empty ? "true (пуст)" : "false (не пуст)")}");
                        break;

                    case "5":
                        string printed = stack.Print();
                        if (verboseLog)
                            sb.AppendLine($"[{step}] 5 -> Print(): {printed}");
                        break;

                    default:
                        if (verboseLog)
                            sb.AppendLine($"[{step}] Неизвестная команда: {token}");
                        break;
                }
            }
            step++;
        }

        sw.Stop();
        return (sb.ToString(), sw.Elapsed.TotalMilliseconds);
    }

    public static List<StackBenchmarkResult> RunBenchmarks(string tempFilePath)
    {
        var results = new List<StackBenchmarkResult>();
        var configs = new (string Name, int Count, string Composition, Func<int, Random, string> Gen)[]
        {
            ("Малый (Пример из задания)", 8, "Смешанный базовый", (_, _) => "3 4 1,56 1,7 1,cat 2 5 4"),
            ("Средний (1 000 оп., преобл. Push)", 1000, "80% Push, 10% Pop, 10% Top/IsEmpty", GeneratePushHeavy),
            ("Средний (1 000 оп., сбаланс. Push/Pop)", 1000, "50% Push, 40% Pop, 10% Top/IsEmpty", GenerateBalanced),
            ("Средний (1 000 оп., частый Print)", 1000, "60% Push, 20% Pop, 20% Print (O(N))", GeneratePrintHeavy),
            ("Крупный (10 000 оп., сбаланс.)", 10000, "50% Push, 40% Pop, 10% Top/IsEmpty", GenerateBalanced),
            ("Большой (50 000 оп., сбаланс.)", 50000, "50% Push, 40% Pop, 10% Top/IsEmpty", GenerateBalanced),
        };

        var rng = new Random(42);
        foreach (var cfg in configs)
        {
            string content = cfg.Gen(cfg.Count, rng);
            File.WriteAllText(tempFilePath, content);

            var swRead = Stopwatch.StartNew();
            string fileText = File.ReadAllText(tempFilePath);
            swRead.Stop();

            var testStack = new CustomStack<string>();
            var (_, execMs) = ExecuteCommands(fileText, testStack, verboseLog: false);

            results.Add(new StackBenchmarkResult
            {
                DatasetName = cfg.Name,
                OperationCount = cfg.Count,
                Composition = cfg.Composition,
                ReadTimeMs = Math.Round(swRead.Elapsed.TotalMilliseconds, 4),
                ExecutionTimeMs = Math.Round(execMs, 4)
            });
        }

        return results;
    }

    private static string GeneratePushHeavy(int count, Random rng)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            int r = rng.Next(100);
            if (r < 80) sb.Append($"1,{rng.Next(1, 1000)} ");
            else if (r < 90) sb.Append("2 ");
            else if (r < 95) sb.Append("3 ");
            else sb.Append("4 ");
        }
        return sb.ToString();
    }

    private static string GenerateBalanced(int count, Random rng)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            int r = rng.Next(100);
            if (r < 50) sb.Append($"1,item{rng.Next(1, 1000)} ");
            else if (r < 90) sb.Append("2 ");
            else if (r < 95) sb.Append("3 ");
            else sb.Append("4 ");
        }
        return sb.ToString();
    }

    private static string GeneratePrintHeavy(int count, Random rng)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            int r = rng.Next(100);
            if (r < 60) sb.Append($"1,{rng.Next(1, 500)} ");
            else if (r < 80) sb.Append("2 ");
            else sb.Append("5 ");
        }
        return sb.ToString();
    }
}
