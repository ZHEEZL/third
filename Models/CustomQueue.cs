using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace third.Models;

public class QueueNode<T>
{
    public T Value { get; set; }
    public QueueNode<T>? Next { get; set; }

    public QueueNode(T value)
    {
        Value = value;
    }
}

/// <summary>
/// Часть 2: Очередь (FIFO) на базе СВОЕГО односвязного списка с указателями на голову и хвост.
/// </summary>
public class CustomQueue<T>
{
    private QueueNode<T>? _head;
    private QueueNode<T>? _tail;
    private int _count;

    public int Count => _count;

    public void Enqueue(T elem)
    {
        var newNode = new QueueNode<T>(elem);
        if (_tail == null)
        {
            _head = _tail = newNode;
        }
        else
        {
            _tail.Next = newNode;
            _tail = newNode;
        }
        _count++;
    }

    public T Dequeue()
    {
        if (_head == null)
            throw new InvalidOperationException("Очередь пуста (невозможно выполнить удаление).");

        T val = _head.Value;
        _head = _head.Next;
        if (_head == null)
            _tail = null;

        _count--;
        return val;
    }

    public T Peek()
    {
        if (_head == null)
            throw new InvalidOperationException("Очередь пуста (невозможно просмотреть первый элемент).");

        return _head.Value;
    }

    public bool IsEmpty() => _head == null;

    public string Print()
    {
        if (_head == null)
            return "[Очередь пуста]";

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
        _head = _tail = null;
        _count = 0;
    }

    public List<T> ToList()
    {
        var list = new List<T>(_count);
        var current = _head;
        while (current != null)
        {
            list.Add(current.Value);
            current = current.Next;
        }
        return list;
    }
}

public class QueueBenchmarkResult
{
    public string Category { get; set; } = "";
    public string DatasetName { get; set; } = "";
    public int OperationCount { get; set; }
    public string Composition { get; set; } = "";
    public double CustomQueueMs { get; set; }
    public double StdQueueMs { get; set; }
    public double DiffMs => Math.Round(Math.Abs(CustomQueueMs - StdQueueMs), 4);
    public string RatioInfo => StdQueueMs > 0
        ? $"{Math.Round(CustomQueueMs / StdQueueMs, 2)}x"
        : "-";
}

public static class QueueCommandProcessor
{
    public static (string Log, double CustomMs, double StdMs) ExecuteAndCompare(
        string rawInput,
        CustomQueue<string> customQueue,
        Queue<string> stdQueue,
        bool verboseLog = true)
    {
        var tokens = rawInput.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        // 1. Замер своей очереди CustomQueue
        var swCustom = Stopwatch.StartNew();
        int step = 1;
        foreach (var token in tokens)
        {
            if (token.StartsWith("1,"))
            {
                string elem = token.Substring(2);
                customQueue.Enqueue(elem);
                if (verboseLog)
                    sb.AppendLine($"[{step}] 1,{elem} -> Вставка(\"{elem}\"): добавлен в конец очереди");
            }
            else
            {
                switch (token)
                {
                    case "2":
                        if (customQueue.IsEmpty())
                        {
                            if (verboseLog) sb.AppendLine($"[{step}] 2 -> Удаление(): Очередь пуста!");
                        }
                        else
                        {
                            var removed = customQueue.Dequeue();
                            if (verboseLog) sb.AppendLine($"[{step}] 2 -> Удаление(): извлечен первый элемент \"{removed}\"");
                        }
                        break;
                    case "3":
                        if (customQueue.IsEmpty())
                        {
                            if (verboseLog) sb.AppendLine($"[{step}] 3 -> Просмотр начала(): Очередь пуста!");
                        }
                        else
                        {
                            var first = customQueue.Peek();
                            if (verboseLog) sb.AppendLine($"[{step}] 3 -> Просмотр начала(): первый элемент = \"{first}\"");
                        }
                        break;
                    case "4":
                        bool empty = customQueue.IsEmpty();
                        if (verboseLog) sb.AppendLine($"[{step}] 4 -> Пустота(): {(empty ? "true (пуста)" : "false (не пуста)")}");
                        break;
                    case "5":
                        string printed = customQueue.Print();
                        if (verboseLog) sb.AppendLine($"[{step}] 5 -> Печать(): {printed}");
                        break;
                }
            }
            step++;
        }
        swCustom.Stop();

        // 2. Замер стандартной очереди Queue<string>
        var swStd = Stopwatch.StartNew();
        foreach (var token in tokens)
        {
            if (token.StartsWith("1,"))
            {
                string elem = token.Substring(2);
                stdQueue.Enqueue(elem);
            }
            else
            {
                switch (token)
                {
                    case "2":
                        if (stdQueue.Count > 0) stdQueue.Dequeue();
                        break;
                    case "3":
                        if (stdQueue.Count > 0) _ = stdQueue.Peek();
                        break;
                    case "4":
                        _ = stdQueue.Count == 0;
                        break;
                    case "5":
                        _ = stdQueue.Count == 0 ? "[Очередь пуста]" : string.Join(" -> ", stdQueue);
                        break;
                }
            }
        }
        swStd.Stop();

        return (sb.ToString(), swCustom.Elapsed.TotalMilliseconds, swStd.Elapsed.TotalMilliseconds);
    }

    public static List<QueueBenchmarkResult> RunBenchmarks(string tempFilePath)
    {
        var results = new List<QueueBenchmarkResult>();
        var rng = new Random(123);

        var configs = new (string Category, string Name, int Count, string Comp, Func<int, Random, string> Gen)[]
        {
            // Группа 1: Различные по длине (одинаковый сбалансированный состав)
            ("1. Разная длина", "100 операций", 100, "50% Вставка, 40% Удал., 10% Просмотр", GenBalanced),
            ("1. Разная длина", "1 000 операций", 1000, "50% Вставка, 40% Удал., 10% Просмотр", GenBalanced),
            ("1. Разная длина", "10 000 операций", 10000, "50% Вставка, 40% Удал., 10% Просмотр", GenBalanced),
            ("1. Разная длина", "50 000 операций", 50000, "50% Вставка, 40% Удал., 10% Просмотр", GenBalanced),

            // Группа 2: Одинаковая длина (20 000 оп.), но различный состав (поиск макс. расхождения)
            ("2. Одинаковая длина (20 000)", "Только проверка пустоты и начала (O(1) без аллокаций)", 20000, "5% Вставка, 95% Peek/IsEmpty", GenReadOnlyHeavy),
            ("2. Одинаковая длина (20 000)", "Чередование 1 Вставка / 1 Удаление (размер <= 1)", 20000, "50% Вставка, 50% Удаление", GenPingPong),
            ("2. Одинаковая длина (20 000)", "МАКС. РАСХОЖДЕНИЕ: Массовая вставка + частая печать (обход памяти)", 20000, "95% Вставка, 5% Печать (Cache locality vs Pointer chasing)", GenMaxDivergence),
        };

        foreach (var cfg in configs)
        {
            string data = cfg.Gen(cfg.Count, rng);
            File.WriteAllText(tempFilePath, data);
            string readData = File.ReadAllText(tempFilePath);

            var cq = new CustomQueue<string>();
            var sq = new Queue<string>();

            var (_, customMs, stdMs) = ExecuteAndCompare(readData, cq, sq, verboseLog: false);

            results.Add(new QueueBenchmarkResult
            {
                Category = cfg.Category,
                DatasetName = cfg.Name,
                OperationCount = cfg.Count,
                Composition = cfg.Comp,
                CustomQueueMs = Math.Round(customMs, 4),
                StdQueueMs = Math.Round(stdMs, 4)
            });
        }

        return results;
    }

    private static string GenBalanced(int count, Random rng)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            int r = rng.Next(100);
            if (r < 50) sb.Append($"1,val{i} ");
            else if (r < 90) sb.Append("2 ");
            else if (r < 95) sb.Append("3 ");
            else sb.Append("4 ");
        }
        return sb.ToString();
    }

    private static string GenReadOnlyHeavy(int count, Random rng)
    {
        var sb = new StringBuilder();
        sb.Append("1,init ");
        for (int i = 1; i < count; i++)
        {
            int r = rng.Next(100);
            if (r < 5) sb.Append($"1,v{i} ");
            else if (r < 55) sb.Append("3 ");
            else sb.Append("4 ");
        }
        return sb.ToString();
    }

    private static string GenPingPong(int count, Random rng)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            sb.Append((i % 2 == 0) ? $"1,x{i} " : "2 ");
        }
        return sb.ToString();
    }

    private static string GenMaxDivergence(int count, Random rng)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            // Постоянно растим очередь и каждые 50 шагов вызываем обход Print (5),
            // где связный список проигрывает массиву Queue<T> из-за аллокаций узлов и промахов кэша процессора
            if (i % 50 == 49)
                sb.Append("5 ");
            else
                sb.Append($"1,item{i} ");
        }
        return sb.ToString();
    }
}
