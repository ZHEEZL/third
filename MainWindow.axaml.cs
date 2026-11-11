using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using third.Models;

namespace third;

public partial class MainWindow : Window
{
    // Часть 1: Стек
    private readonly CustomStack<string> _interactiveStack = new();

    // Часть 2: Очереди
    private readonly CustomQueue<string> _interactiveCustomQueue = new();
    private readonly Queue<string> _interactiveStdQueue = new();

    // Часть 3: Прикладные задачи
    private readonly DoublyLinkedPlaylist _playlist = new();
    private readonly BrowserHistoryEngine _browserHistory = new();
    private readonly PrintSpoolerEngine _printSpooler = new();
    private readonly TrieAutocompleteEngine _trie = new();
    private readonly StringBuilder _spoolerHistoryLog = new();

    // Часть 4: Свой односвязный список
    private CustomLinkedList _linkedList = CustomLinkedList.FromString("10 20 30 20 40 50");

    public MainWindow()
    {
        InitializeComponent();

        EnsureDefaultFilesExist();

        // Инициализация стартового отображения всех 4 вкладок
        OnStackRunFileClick(null, new RoutedEventArgs());
        EvaluateAndDisplayRpn(PostfixInputBox.Text ?? "7 8 + 3 2 + /");

        OnQueueRunFileClick(null, new RoutedEventArgs());
        OnQueueBenchmarkClick(null, new RoutedEventArgs());

        RefreshPlaylistUi();
        RefreshBrowserUi();
        RefreshSpoolerUi("Спулер печати инициализирован.");
        RefreshTrieUi("ст");

        RefreshLinkedListUi("Исходный список L создан: [10] -> [20] -> [30] -> [20] -> [40] -> [50] -> NULL");
        OnBuildBinaryTreeClick(null, new RoutedEventArgs());
    }

    private static string GetFilePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, fileName);

    private static void EnsureDefaultFilesExist()
    {
        string inputPath = GetFilePath("input.txt");
        if (!File.Exists(inputPath))
            File.WriteAllText(inputPath, "3 4 1,56 1,7 1,cat 2 5 4");

        string rpnPath = GetFilePath("rpn_input.txt");
        if (!File.Exists(rpnPath))
            File.WriteAllText(rpnPath, "7 8 + 3 2 + /");

        string listsPath = GetFilePath("lists_input.txt");
        if (!File.Exists(listsPath))
            File.WriteAllText(listsPath, "10 20 30 40\n50 60 70 80");
    }

    // =========================================================================
    // ВКЛАДКА 1: ЧАСТЬ 1 — СТЕК И ПОСТФИКСНАЯ ЗАПИСЬ (ОПЗ)
    // =========================================================================
    private void RefreshStackStatus()
    {
        StackVisualStatus.Text = $"Текущий стек (от вершины, элементов: {_interactiveStack.Count}): {_interactiveStack.Print()}";
    }

    private void AppendStackLog(string line)
    {
        StackOutputLog.Text = line + Environment.NewLine + (StackOutputLog.Text ?? "");
        RefreshStackStatus();
    }

    private void OnStackPushClick(object? sender, RoutedEventArgs e)
    {
        string elem = (StackPushInput.Text ?? "").Trim();
        if (string.IsNullOrEmpty(elem)) elem = "0";
        _interactiveStack.Push(elem);
        AppendStackLog($"[Ручной ввод] 1,{elem} -> Push(\"{elem}\") выполнен. Вершина = \"{elem}\"");
    }

    private void OnStackPopClick(object? sender, RoutedEventArgs e)
    {
        if (_interactiveStack.IsEmpty())
        {
            AppendStackLog("[Ручной ввод] 2 -> Pop(): Стек пуст!");
            return;
        }
        string val = _interactiveStack.Pop();
        AppendStackLog($"[Ручной ввод] 2 -> Pop(): извлечен элемент \"{val}\"");
    }

    private void OnStackTopClick(object? sender, RoutedEventArgs e)
    {
        if (_interactiveStack.IsEmpty())
        {
            AppendStackLog("[Ручной ввод] 3 -> Top(): Стек пуст!");
            return;
        }
        AppendStackLog($"[Ручной ввод] 3 -> Top(): вершина стека = \"{_interactiveStack.Top()}\"");
    }

    private void OnStackIsEmptyClick(object? sender, RoutedEventArgs e)
    {
        AppendStackLog($"[Ручной ввод] 4 -> isEmpty(): {(_interactiveStack.IsEmpty() ? "true (пуст)" : "false (не пуст)")}");
    }

    private void OnStackPrintClick(object? sender, RoutedEventArgs e)
    {
        AppendStackLog($"[Ручной ввод] 5 -> Print(): {_interactiveStack.Print()}");
    }

    private void OnStackClearClick(object? sender, RoutedEventArgs e)
    {
        _interactiveStack.Clear();
        AppendStackLog("[Ручной ввод] Стек очищен.");
    }

    private void OnStackRunFileClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string path = GetFilePath("input.txt");
            File.WriteAllText(path, StackFileInputBox.Text ?? "");

            var swRead = Stopwatch.StartNew();
            string content = File.ReadAllText(path);
            swRead.Stop();

            _interactiveStack.Clear();
            var (log, execMs) = StackCommandProcessor.ExecuteCommands(content, _interactiveStack, verboseLog: true);

            var sb = new StringBuilder();
            sb.AppendLine($"=== ЧТЕНИЕ И ВЫПОЛНЕНИЕ ФАЙЛА input.txt ===");
            sb.AppendLine($"Файл: {path}");
            sb.AppendLine($"Содержимое: {content.Trim()}");
            sb.AppendLine($"Время считывания из файла: {swRead.Elapsed.TotalMilliseconds:F4} мс");
            sb.AppendLine($"Время выполнения операций стеком: {execMs:F4} мс");
            sb.AppendLine($"Суммарное время: {(swRead.Elapsed.TotalMilliseconds + execMs):F4} мс");
            sb.AppendLine(new string('-', 56));
            sb.Append(log);

            StackOutputLog.Text = sb.ToString();
            RefreshStackStatus();
        }
        catch (Exception ex)
        {
            StackOutputLog.Text = $"Ошибка: {ex.Message}";
        }
    }

    private void OnStackBenchmarkClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string tempPath = GetFilePath("stack_benchmark_input.txt");
            var results = StackCommandProcessor.RunBenchmarks(tempPath);

            var sb = new StringBuilder();
            sb.AppendLine("=== ОТЧЕТ: ЗАМЕРЫ ВРЕМЕНИ СТЕКА ДЛЯ РАЗЛИЧНЫХ НАБОРОВ В input.txt ===");
            sb.AppendLine($"{"Набор данных",-38} | {"Опер.",7} | {"Чтение(мс)",10} | {"Выполн.(мс)",11} | {"Всего(мс)",10}");
            sb.AppendLine(new string('-', 88));
            foreach (var r in results)
            {
                sb.AppendLine($"{r.DatasetName,-38} | {r.OperationCount,7} | {r.ReadTimeMs,10:F4} | {r.ExecutionTimeMs,11:F4} | {r.TotalTimeMs,10:F4}");
            }
            sb.AppendLine(new string('-', 88));
            sb.AppendLine("Вывод для отчета:");
            sb.AppendLine("1) Операции Push, Pop, Top, isEmpty в нашем стеке на связном списке выполняются за O(1).");
            sb.AppendLine("2) Время растет линейно O(N) от числа команд в input.txt.");
            sb.AppendLine("3) При частом вызове операции 5 (Print) время увеличивается до O(N^2), так как Print обходит все узлы списка.");

            StackOutputLog.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            StackOutputLog.Text = $"Ошибка замеров: {ex.Message}";
        }
    }

    // Часть 1.2: Инфикс -> Постфикс и Вычисление ОПЗ
    private void OnPresetInfix1Click(object? sender, RoutedEventArgs e)
    {
        InfixInputBox.Text = "(7 + 8) / (3 + 2)";
        OnConvertInfixClick(sender, e);
    }

    private void OnPresetInfix2Click(object? sender, RoutedEventArgs e)
    {
        InfixInputBox.Text = "2 + 2";
        OnConvertInfixClick(sender, e);
    }

    private void OnPresetInfix3Click(object? sender, RoutedEventArgs e)
    {
        InfixInputBox.Text = "sqrt(16) + 2 ^ 3 - ln(1) + cos(0)";
        OnConvertInfixClick(sender, e);
    }

    private void OnConvertInfixClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string infix = InfixInputBox.Text ?? "";
            string postfix = RpnCalculator.InfixToPostfix(infix);
            PostfixInputBox.Text = postfix;

            // Сохраняем в rpn_input.txt и сразу вычисляем
            File.WriteAllText(GetFilePath("rpn_input.txt"), postfix);
            EvaluateAndDisplayRpn(postfix);
        }
        catch (Exception ex)
        {
            RpnResultBanner.Text = $"Ошибка перевода: {ex.Message}";
            RpnResultBanner.Foreground = Brushes.Crimson;
        }
    }

    private void OnEvaluatePostfixClick(object? sender, RoutedEventArgs e)
    {
        string postfix = PostfixInputBox.Text ?? "";
        File.WriteAllText(GetFilePath("rpn_input.txt"), postfix);
        EvaluateAndDisplayRpn(postfix);
    }

    private void OnEvaluatePostfixFileClick(object? sender, RoutedEventArgs e)
    {
        string path = GetFilePath("rpn_input.txt");
        if (File.Exists(path))
        {
            string postfix = File.ReadAllText(path).Trim();
            PostfixInputBox.Text = postfix;
            EvaluateAndDisplayRpn(postfix);
        }
    }

    private void EvaluateAndDisplayRpn(string postfix)
    {
        try
        {
            var eval = RpnCalculator.EvaluatePostfix(postfix);
            RpnResultBanner.Text = $"Итоговый результат вычисления ОПЗ: {Math.Round(eval.Result, 6).ToString(CultureInfo.InvariantCulture)}";
            RpnResultBanner.Foreground = new SolidColorBrush(Color.Parse("#059669"));

            // Строим визуальные стаканы стека (как на схеме в задании 7 8 + 3 2 + /)
            var stepCards = new List<Control>();
            foreach (var step in eval.Steps)
            {
                stepCards.Add(CreateRpnStepCard(step));
            }
            RpnStepsVisualList.ItemsSource = stepCards;

            var sb = new StringBuilder();
            sb.AppendLine("=== ОЦЕНКА СЛОЖНОСТИ АЛГОРИТМА ВЫЧИСЛЕНИЯ ПОСТФИКСНОЙ ЗАПИСИ ===");
            sb.Append(eval.ComplexityAnalysis);
            sb.AppendLine();
            sb.AppendLine("=== ПОШАГОВАЯ ТРАССИРОВКА СТЕКА ===");
            foreach (var s in eval.Steps)
            {
                sb.AppendLine($"Шаг {s.StepNumber,2} | Токен: '{s.Token,-4}' | {s.ActionDescription}");
                sb.AppendLine($"       | Стек (снизу вверх): [{s.StackFormatted}]");
            }
            RpnComplexityAndLogBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            RpnResultBanner.Text = $"Ошибка вычисления: {ex.Message}";
            RpnResultBanner.Foreground = Brushes.Crimson;
        }
    }

    private static Control CreateRpnStepCard(RpnStep step)
    {
        var stackContainer = new StackPanel
        {
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        // Отрисовываем элементы стека сверху вниз (чтобы вершина была сверху стакана, а дно внизу, как на рисунке)
        for (int i = step.StackSnapshotBottomUp.Count - 1; i >= 0; i--)
        {
            var itemBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = new TextBlock
                {
                    Text = step.StackSnapshotBottomUp[i],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#0F172A"))
                }
            };
            stackContainer.Children.Add(itemBorder);
        }

        // Дно стакана
        stackContainer.Children.Add(new Border
        {
            Height = 4,
            Background = new SolidColorBrush(Color.Parse("#475569")),
            CornerRadius = new CornerRadius(2)
        });

        var outer = new Border
        {
            Width = 76,
            Background = new SolidColorBrush(Color.Parse("#F1F5F9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#CBD5E1")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*"),
                Children =
                {
                    new TextBlock
                    {
                        [Grid.RowProperty] = 0,
                        Text = $"{step.Token} ↓",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontWeight = FontWeight.Bold,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#2563EB")),
                        Margin = new Thickness(0, 0, 0, 4)
                    },
                    new Border
                    {
                        [Grid.RowProperty] = 1,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Child = stackContainer
                    }
                }
            }
        };

        return outer;
    }

    // =========================================================================
    // ВКЛАДКА 2: ЧАСТЬ 2 — ОЧЕРЕДЬ (СВОЯ НА СПИСКЕ VS СТАНДАРТНАЯ QUEUE<T>)
    // =========================================================================
    private void RefreshQueueStatus()
    {
        QueueVisualStatus.Text = $"Своя очередь (элементов: {_interactiveCustomQueue.Count}): {_interactiveCustomQueue.Print()}";
    }

    private void AppendQueueLog(string line)
    {
        QueueOutputLog.Text = line + Environment.NewLine + (QueueOutputLog.Text ?? "");
        RefreshQueueStatus();
    }

    private void OnQueueEnqueueClick(object? sender, RoutedEventArgs e)
    {
        string val = (QueueEnqueueInput.Text ?? "").Trim();
        if (string.IsNullOrEmpty(val)) val = "0";
        _interactiveCustomQueue.Enqueue(val);
        _interactiveStdQueue.Enqueue(val);
        AppendQueueLog($"[Ручной ввод] 1,{val} -> Вставка(\"{val}\") в конец обеих очередей.");
    }

    private void OnQueueDequeueClick(object? sender, RoutedEventArgs e)
    {
        if (_interactiveCustomQueue.IsEmpty())
        {
            AppendQueueLog("[Ручной ввод] 2 -> Удаление(): Очередь пуста!");
            return;
        }
        string v1 = _interactiveCustomQueue.Dequeue();
        if (_interactiveStdQueue.Count > 0) _interactiveStdQueue.Dequeue();
        AppendQueueLog($"[Ручной ввод] 2 -> Удаление(): извлечен первый элемент \"{v1}\"");
    }

    private void OnQueuePeekClick(object? sender, RoutedEventArgs e)
    {
        if (_interactiveCustomQueue.IsEmpty())
        {
            AppendQueueLog("[Ручной ввод] 3 -> Просмотр начала(): Очередь пуста!");
            return;
        }
        AppendQueueLog($"[Ручной ввод] 3 -> Просмотр начала(): первый элемент = \"{_interactiveCustomQueue.Peek()}\"");
    }

    private void OnQueueIsEmptyClick(object? sender, RoutedEventArgs e)
    {
        AppendQueueLog($"[Ручной ввод] 4 -> Пустота(): {(_interactiveCustomQueue.IsEmpty() ? "true (пуста)" : "false (не пуста)")}");
    }

    private void OnQueuePrintClick(object? sender, RoutedEventArgs e)
    {
        AppendQueueLog($"[Ручной ввод] 5 -> Печать(): {_interactiveCustomQueue.Print()}");
    }

    private void OnQueueClearClick(object? sender, RoutedEventArgs e)
    {
        _interactiveCustomQueue.Clear();
        _interactiveStdQueue.Clear();
        AppendQueueLog("[Ручной ввод] Обе очереди очищены.");
    }

    private void OnQueueRunFileClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string path = GetFilePath("input.txt");
            File.WriteAllText(path, QueueFileInputBox.Text ?? "");

            var swRead = Stopwatch.StartNew();
            string raw = File.ReadAllText(path);
            swRead.Stop();

            _interactiveCustomQueue.Clear();
            _interactiveStdQueue.Clear();

            var (log, customMs, stdMs) = QueueCommandProcessor.ExecuteAndCompare(
                raw, _interactiveCustomQueue, _interactiveStdQueue, verboseLog: true);

            var sb = new StringBuilder();
            sb.AppendLine("=== ВЫПОЛНЕНИЕ ФАЙЛА input.txt НА ДВУХ РЕАЛИЗАЦИЯХ ОЧЕРЕДИ ===");
            sb.AppendLine($"Время чтения файла:                {swRead.Elapsed.TotalMilliseconds:F4} мс");
            sb.AppendLine($"Своя очередь (связный список):     {customMs:F4} мс");
            sb.AppendLine($"Стандартная Queue<T> (массив):     {stdMs:F4} мс");
            sb.AppendLine(new string('-', 58));
            sb.Append(log);

            QueueOutputLog.Text = sb.ToString();
            RefreshQueueStatus();
        }
        catch (Exception ex)
        {
            QueueOutputLog.Text = $"Ошибка: {ex.Message}";
        }
    }

    private void OnQueueBenchmarkClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string tempPath = GetFilePath("queue_benchmark_input.txt");
            var results = QueueCommandProcessor.RunBenchmarks(tempPath);

            var sb = new StringBuilder();
            sb.AppendLine("=== СРАВНИТЕЛЬНЫЙ ОТЧЕТ: СВОЯ ОЧЕРЕДЬ (СПИСОК) VS СТАНДАРТНАЯ QUEUE<T> ===");
            sb.AppendLine();
            sb.AppendLine($"{"Набор операций",-56} | {"Своя(мс)",9} | {"Queue<T>",9} | {"Разница",9} | {"Во ск.раз",9}");
            sb.AppendLine(new string('-', 104));

            string lastCat = "";
            foreach (var r in results)
            {
                if (r.Category != lastCat)
                {
                    sb.AppendLine($"[{r.Category}]");
                    lastCat = r.Category;
                }
                string title = $"  {r.DatasetName}";
                if (title.Length > 56) title = title.Substring(0, 56);
                sb.AppendLine($"{title,-56} | {r.CustomQueueMs,9:F4} | {r.StdQueueMs,9:F4} | {r.DiffMs,9:F4} | {r.RatioInfo,9}");
            }

            sb.AppendLine(new string('-', 104));
            sb.AppendLine();
            sb.AppendLine("АНАЛИТИЧЕСКИЕ ВЫВОДЫ ДЛЯ ОТЧЕТА:");
            sb.AppendLine("1. Наборы РАЗЛИЧНОЙ ДЛИНЫ (100 .. 50 000 операций):");
            sb.AppendLine("   Обе реализации демонстрируют линейную асимптотику O(N), так как вставка в хвост и удаление");
            sb.AppendLine("   из головы занимают O(1). Однако стандартный класс Queue<T> быстрее в среднем в 1.5 - 3 раза.");
            sb.AppendLine();
            sb.AppendLine("2. Наборы ОДИНАКОВОЙ ДЛИНЫ (20 000 операций), но РАЗЛИЧНОГО СОСТАВА:");
            sb.AppendLine("   • Минимальное расхождение наблюдается при операциях просмотра начала (3) и пустоты (4),");
            sb.AppendLine("     где не происходит выделения памяти.");
            sb.AppendLine("   • МАКСИМАЛЬНОЕ РАСХОЖДЕНИЕ достигается на наборе «Массовая вставка + периодическая печать (5)»:");
            sb.AppendLine("     - Своя очередь на связном списке при каждой вставке выделяет новый объект QueueNode в куче (GC pressure),");
            sb.AppendLine("       а при печати идет по разрозненным указателям в памяти (pointer chasing, промахи кэша CPU).");
            sb.AppendLine("     - Стандартная Queue<T> хранит элементы в непрерывном кольцевом массиве (высокая кэш-локальность CPU),");
            sb.AppendLine("       поэтому обход и вставка выполняются значительно быстрее.");

            QueueBenchmarkReportBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            QueueBenchmarkReportBox.Text = $"Ошибка бенчмарка: {ex.Message}";
        }
    }

    // =========================================================================
    // ВКЛАДКА 3: ЧАСТЬ 3 — ПРАКТИЧЕСКОЕ ПРИМЕНЕНИЕ СТРУКТУР ДАННЫХ
    // =========================================================================

    // 3.1. Плейлист (Двусвязный список)
    private void RefreshPlaylistUi()
    {
        if (_playlist.Current == null)
        {
            PlaylistNowPlayingText.Text = "[Плейлист пуст]";
            PlaylistLinksInfoText.Text = "Prev: NULL | Next: NULL";
        }
        else
        {
            PlaylistNowPlayingText.Text = _playlist.Current.ToString();
            string prev = _playlist.Current.Prev != null ? _playlist.Current.Prev.TrackTitle : "NULL (Начало списка)";
            string next = _playlist.Current.Next != null ? _playlist.Current.Next.TrackTitle : "NULL (Конец списка)";
            PlaylistLinksInfoText.Text = $"← Prev: {prev}   |   Next: {next} →";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Всего треков в двусвязном списке: {_playlist.Count}");
        sb.AppendLine("HEAD");
        sb.AppendLine(" ↓");

        var cur = _playlist.Head;
        while (cur != null)
        {
            bool isCur = ReferenceEquals(cur, _playlist.Current);
            string marker = isCur ? " ★ [ТЕКУЩИЙ ТРЕК]" : "";
            string pName = cur.Prev != null ? cur.Prev.TrackTitle : "NULL";
            string nName = cur.Next != null ? cur.Next.TrackTitle : "NULL";

            sb.AppendLine($"┌──────────────────────────────────────────────────────────────┐");
            sb.AppendLine($"│ {cur}{marker}");
            sb.AppendLine($"│ Ссылки узла: [Prev: {pName}] <===> [Next: {nName}]");
            sb.AppendLine($"└──────────────────────────────────────────────────────────────┘");
            if (cur.Next != null)
                sb.AppendLine("       ⇅ (двусвязный переход Prev / Next)");
            cur = cur.Next;
        }
        sb.AppendLine(" ↓");
        sb.AppendLine("TAIL (NULL)");

        PlaylistVisualBox.Text = sb.ToString();
    }

    private void OnPlaylistPrevClick(object? sender, RoutedEventArgs e)
    {
        _playlist.MovePrev();
        RefreshPlaylistUi();
    }

    private void OnPlaylistNextClick(object? sender, RoutedEventArgs e)
    {
        _playlist.MoveNext();
        RefreshPlaylistUi();
    }

    private void OnPlaylistShuffleClick(object? sender, RoutedEventArgs e)
    {
        _playlist.Shuffle();
        RefreshPlaylistUi();
    }

    private void OnPlaylistRemoveClick(object? sender, RoutedEventArgs e)
    {
        _playlist.RemoveCurrent();
        RefreshPlaylistUi();
    }

    private void OnPlaylistAddClick(object? sender, RoutedEventArgs e)
    {
        string artist = (NewTrackArtistBox.Text ?? "").Trim();
        string title = (NewTrackTitleBox.Text ?? "").Trim();
        string dur = (NewTrackDurationBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(artist)) artist = "Unknown Artist";
        if (string.IsNullOrEmpty(title)) title = "Untitled Track";
        if (string.IsNullOrEmpty(dur)) dur = "3:30";

        _playlist.AddTrack(title, artist, dur);
        RefreshPlaylistUi();
    }

    // 3.2. История браузера (Два стека LIFO)
    private void RefreshBrowserUi()
    {
        BrowserCurrentUrlText.Text = _browserHistory.CurrentUrl;
        BrowserBackBtn.IsEnabled = !_browserHistory.BackStack.IsEmpty();
        BrowserForwardBtn.IsEnabled = !_browserHistory.ForwardStack.IsEmpty();

        var sb = new StringBuilder();
        sb.AppendLine($"ТЕКУЩАЯ ОТКРЫТАЯ СТРАНИЦА:");
        sb.AppendLine($"  🌐 {_browserHistory.CurrentUrl}");
        sb.AppendLine(new string('=', 60));

        sb.AppendLine($"СТЕК «НАЗАД» (BackStack, элементов: {_browserHistory.BackStack.Count}):");
        var backItems = _browserHistory.BackStack.ToTopDownList();
        if (backItems.Count == 0)
        {
            sb.AppendLine("  [Стек пуст]");
        }
        else
        {
            for (int i = 0; i < backItems.Count; i++)
            {
                string topTag = i == 0 ? " <-- ВЕРШИНА СТЕКА (Top)" : "";
                sb.AppendLine($"  [{i + 1}] {backItems[i]}{topTag}");
            }
        }

        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"СТЕК «ВПЕРЁД» (ForwardStack, элементов: {_browserHistory.ForwardStack.Count}):");
        var fwdItems = _browserHistory.ForwardStack.ToTopDownList();
        if (fwdItems.Count == 0)
        {
            sb.AppendLine("  [Стек пуст]");
        }
        else
        {
            for (int i = 0; i < fwdItems.Count; i++)
            {
                string topTag = i == 0 ? " <-- ВЕРШИНА СТЕКА (Top)" : "";
                sb.AppendLine($"  [{i + 1}] {fwdItems[i]}{topTag}");
            }
        }

        BrowserStacksVisualBox.Text = sb.ToString();
    }

    private void OnBrowserBackClick(object? sender, RoutedEventArgs e)
    {
        _browserHistory.GoBack();
        RefreshBrowserUi();
    }

    private void OnBrowserForwardClick(object? sender, RoutedEventArgs e)
    {
        _browserHistory.GoForward();
        RefreshBrowserUi();
    }

    private void OnBrowserVisitClick(object? sender, RoutedEventArgs e)
    {
        string url = (BrowserNewUrlBox.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(url))
        {
            _browserHistory.Visit(url);
            RefreshBrowserUi();
        }
    }

    private void OnBrowserQuickUrl1(object? sender, RoutedEventArgs e)
    {
        BrowserNewUrlBox.Text = "https://ru.wikipedia.org/wiki/Связный_список";
        OnBrowserVisitClick(sender, e);
    }

    private void OnBrowserQuickUrl2(object? sender, RoutedEventArgs e)
    {
        BrowserNewUrlBox.Text = "https://stackoverflow.com/questions/tagged/c%23";
        OnBrowserVisitClick(sender, e);
    }

    private void OnBrowserQuickUrl3(object? sender, RoutedEventArgs e)
    {
        BrowserNewUrlBox.Text = "https://dotnet.microsoft.com/ru-ru/";
        OnBrowserVisitClick(sender, e);
    }

    // 3.3. Спулер печати (Очередь с приоритетами)
    private void RefreshSpoolerUi(string actionMessage)
    {
        _spoolerHistoryLog.Insert(0, $"• [{DateTime.Now:HH:mm:ss}] {actionMessage}\n");

        var sb = new StringBuilder();
        sb.AppendLine($"ОЧЕРЕДЬ ПЕЧАТИ (всего заданий в очереди: {_printSpooler.Count}):");
        sb.AppendLine(new string('=', 62));

        var jobs = _printSpooler.GetQueueSnapshot();
        if (jobs.Count == 0)
        {
            sb.AppendLine("  [Очередь печати пуста — принтер свободен]");
        }
        else
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                string headTag = i == 0 ? " <-- ПЕРВЫЙ НА ПЕЧАТЬ (Head)" : "";
                sb.AppendLine($"  {i + 1}. {jobs[i]}{headTag}");
            }
        }

        sb.AppendLine(new string('-', 62));
        sb.AppendLine("ЖУРНАЛ СОБЫТИЙ СПУЛЕРА:");
        sb.Append(_spoolerHistoryLog);

        SpoolerVisualBox.Text = sb.ToString();
    }

    private void OnSpoolerAddClick(object? sender, RoutedEventArgs e)
    {
        string doc = (SpoolerDocNameBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(doc)) doc = "Документ.pdf";
        if (!int.TryParse(SpoolerPagesBox.Text, out int pages) || pages <= 0) pages = 1;
        int priority = (SpoolerPriorityCombo.SelectedIndex >= 0) ? SpoolerPriorityCombo.SelectedIndex + 1 : 2;

        var job = _printSpooler.EnqueueJob(doc, pages, priority);
        RefreshSpoolerUi($"Добавлено задание {job}");
    }

    private void OnSpoolerPrintNextClick(object? sender, RoutedEventArgs e)
    {
        var printed = _printSpooler.PrintNext();
        if (printed == null)
            RefreshSpoolerUi("Попытка печати: очередь пуста!");
        else
            RefreshSpoolerUi($"НАПЕЧАТАНО и удалено из очереди: {printed}");
    }

    private void OnSpoolerCancelClick(object? sender, RoutedEventArgs e)
    {
        if (int.TryParse(SpoolerCancelIdBox.Text, out int id))
        {
            bool ok = _printSpooler.CancelJob(id);
            RefreshSpoolerUi(ok
                ? $"Задание #{id} успешно ОТМЕНЕНО и удалено из очереди."
                : $"Задание с ID #{id} не найдено в очереди.");
        }
    }

    // 3.4. Префиксное дерево (Trie)
    private void RefreshTrieUi(string prefix)
    {
        var matches = _trie.Autocomplete(prefix);
        var sb = new StringBuilder();
        sb.AppendLine($"Всего слов в словаре Trie: {_trie.WordCount}");
        sb.AppendLine($"Поисковый префикс: \"{(string.IsNullOrEmpty(prefix) ? "[все слова]" : prefix)}\"");
        sb.AppendLine($"Найдено совпадений: {matches.Count}");
        sb.AppendLine(new string('=', 56));

        if (matches.Count == 0)
        {
            sb.AppendLine("  [Слов с таким префиксом не найдено]");
        }
        else
        {
            for (int i = 0; i < matches.Count; i++)
            {
                sb.AppendLine($"  {i + 1,2}. {matches[i]}");
            }
        }

        TrieOutputBox.Text = sb.ToString();
    }

    private void OnTriePrefixTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (TrieOutputBox != null)
            RefreshTrieUi(TriePrefixInputBox.Text ?? "");
    }

    private void OnTrieSearchClick(object? sender, RoutedEventArgs e) =>
        RefreshTrieUi(TriePrefixInputBox.Text ?? "");

    private void OnTriePresetA(object? sender, RoutedEventArgs e) { TriePrefixInputBox.Text = "а"; RefreshTrieUi("а"); }
    private void OnTriePresetSt(object? sender, RoutedEventArgs e) { TriePrefixInputBox.Text = "ст"; RefreshTrieUi("ст"); }
    private void OnTriePresetDe(object? sender, RoutedEventArgs e) { TriePrefixInputBox.Text = "де"; RefreshTrieUi("де"); }
    private void OnTriePresetPo(object? sender, RoutedEventArgs e) { TriePrefixInputBox.Text = "по"; RefreshTrieUi("по"); }
    private void OnTriePresetAll(object? sender, RoutedEventArgs e) { TriePrefixInputBox.Text = ""; RefreshTrieUi(""); }

    private void OnTrieInsertClick(object? sender, RoutedEventArgs e)
    {
        string word = (TrieNewWordBox.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(word))
        {
            _trie.Insert(word);
            RefreshTrieUi(TriePrefixInputBox.Text ?? "");
        }
    }

    // =========================================================================
    // ВКЛАДКА 4: ЧАСТЬ 4 — 12 ФУНКЦИЙ СО СВОИМ СПИСКОМ + БИНАРНОЕ ДЕРЕВО (2024)
    // =========================================================================
    private void RefreshLinkedListUi(string actionLog)
    {
        LinkedListCurrentText.Text = _linkedList.FormatList();
        LinkedListLogBox.Text = $"[{DateTime.Now:HH:mm:ss}] {actionLog}\n" +
                                $"Текущий список L: {_linkedList.FormatList()}\n" +
                                new string('-', 64) + "\n" +
                                (LinkedListLogBox.Text ?? "");
    }

    private int GetParamX() => int.TryParse(ListParamXBox.Text, out int v) ? v : 20;
    private int GetParamE() => int.TryParse(ListParamEBox.Text, out int v) ? v : 30;
    private int GetParamF() => int.TryParse(ListParamFBox.Text, out int v) ? v : 99;

    private void OnListSetClick(object? sender, RoutedEventArgs e)
    {
        _linkedList = CustomLinkedList.FromString(LinkedListInitBox.Text ?? "");
        RefreshLinkedListUi("Задан новый список L из поля ввода.");
    }

    private void OnListSetSortedClick(object? sender, RoutedEventArgs e)
    {
        LinkedListInitBox.Text = "10 20 30 40 50";
        _linkedList = CustomLinkedList.FromString("10 20 30 40 50");
        RefreshLinkedListUi("Задан упорядоченный по неубыванию список L.");
    }

    // 1. Переворот списка L
    private void OnListAlg1Reverse(object? sender, RoutedEventArgs e)
    {
        _linkedList.Reverse();
        RefreshLinkedListUi("Функция 1: Список L перевернут (изменены ссылки Next в обратном порядке).");
    }

    // 2а. Последний в начало
    private void OnListAlg2LastToFirst(object? sender, RoutedEventArgs e)
    {
        _linkedList.MoveLastToFirst();
        RefreshLinkedListUi("Функция 2а: Последний элемент списка L перенесен в начало.");
    }

    // 2б. Первый в конец
    private void OnListAlg2FirstToLast(object? sender, RoutedEventArgs e)
    {
        _linkedList.MoveFirstToLast();
        RefreshLinkedListUi("Функция 2б: Первый элемент списка L перенесен в конец.");
    }

    // 3. Количество различных элементов
    private void OnListAlg3CountDistinct(object? sender, RoutedEventArgs e)
    {
        int count = _linkedList.CountDistinct();
        RefreshLinkedListUi($"Функция 3: Количество различных (уникальных по значению) элементов в списке L = {count}.");
    }

    // 4а. Удаление дубликатов (оставить первые вхождения)
    private void OnListAlg4RemoveDupsKeepOne(object? sender, RoutedEventArgs e)
    {
        _linkedList.RemoveDuplicatesKeepFirst();
        RefreshLinkedListUi("Функция 4а: Из списка L удалены повторные вхождения элементов.");
    }

    // 4б. Удаление всех неуникальных элементов полностью
    private void OnListAlg4RemoveAllNonUnique(object? sender, RoutedEventArgs e)
    {
        _linkedList.RemoveAllNonUniqueStrict();
        RefreshLinkedListUi("Функция 4б: Из списка L полностью удалены все элементы, встречавшиеся более 1 раза.");
    }

    // 5. Вставка списка самого в себя после первого вхождения X
    private void OnListAlg5InsertSelfAfterX(object? sender, RoutedEventArgs e)
    {
        int x = GetParamX();
        bool ok = _linkedList.InsertSelfAfterFirstX(x);
        RefreshLinkedListUi(ok
            ? $"Функция 5: Копия списка L вставлена вслед за первым вхождением X = {x}."
            : $"Функция 5: Число X = {x} не найдено в списке L (список не изменился).");
    }

    // 6. Вставка элемента E в упорядоченный по неубыванию список L
    private void OnListAlg6InsertOrdered(object? sender, RoutedEventArgs e)
    {
        int el = GetParamE();
        _linkedList.InsertOrdered(el);
        RefreshLinkedListUi($"Функция 6: Элемент E = {el} вставлен в упорядоченный список L с сохранением порядка.");
    }

    // 7. Удаление из списка L всех элементов E
    private void OnListAlg7RemoveAllE(object? sender, RoutedEventArgs e)
    {
        int el = GetParamE();
        int removed = _linkedList.RemoveAll(el);
        RefreshLinkedListUi($"Функция 7: Удалено вхождений элемента E = {el}: {removed} шт.");
    }

    // 8. Вставка F перед первым вхождением E
    private void OnListAlg8InsertFBeforeE(object? sender, RoutedEventArgs e)
    {
        int el = GetParamE();
        int f = GetParamF();
        bool ok = _linkedList.InsertBeforeFirst(el, f);
        RefreshLinkedListUi(ok
            ? $"Функция 8: Новый элемент F = {f} вставлен перед первым вхождением E = {el}."
            : $"Функция 8: Элемент E = {el} не входит в список L.");
    }

    // 9. Дописать к списку L список E (считать оба из файла lists_input.txt)
    private void OnListAlg9AppendFromFile(object? sender, RoutedEventArgs e)
    {
        string path = GetFilePath("lists_input.txt");
        var lines = File.ReadAllLines(path);
        string lineL = lines.Length > 0 ? lines[0] : "10 20 30";
        string lineE = lines.Length > 1 ? lines[1] : "40 50 60";

        _linkedList = CustomLinkedList.FromString(lineL);
        var listE = CustomLinkedList.FromString(lineE);
        string beforeL = _linkedList.FormatList();
        string strE = listE.FormatList();

        _linkedList.AppendList(listE);
        RefreshLinkedListUi($"Функция 9 (из файла lists_input.txt):\n  Считан L: {beforeL}\n  Считан E: {strE}\n  Список E дописан в конец списка L.");
    }

    // 10. Разбить список на два по первому вхождению X
    private void OnListAlg10SplitByX(object? sender, RoutedEventArgs e)
    {
        int x = GetParamX();
        var secondList = _linkedList.SplitByFirst(x);
        RefreshLinkedListUi($"Функция 10: Разбиение по первому вхождению X = {x}:\n  Первый список L1: {_linkedList.FormatList()}\n  Второй список L2: {secondList.FormatList()}");
    }

    // 11. Удвоить список (приписать в конец самого себя)
    private void OnListAlg11DuplicateSelf(object? sender, RoutedEventArgs e)
    {
        _linkedList.DuplicateSelf();
        RefreshLinkedListUi("Функция 11: Список L удвоен (в конец приписана копия самого себя).");
    }

    // 12. Поменять местами два элемента списка, заданные пользователем (X и E)
    private void OnListAlg12SwapElements(object? sender, RoutedEventArgs e)
    {
        int x = GetParamX();
        int el = GetParamE();
        bool ok = _linkedList.SwapElements(x, el);
        RefreshLinkedListUi(ok
            ? $"Функция 12: Элементы {x} и {el} поменяны местами путем перестановки ссылок узлов."
            : $"Функция 12: Один из элементов ({x} или {el}) не найден в списке L.");
    }

    // 4.3. Бонус 2024: Бинарное дерево ABD*G***CE**FH**J**
    private void OnBuildBinaryTreeClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string input = (BinaryTreeStringBox.Text ?? "ABD*G***CE**FH**J**").Trim();
            var root = SerializedBinaryTree.ParsePreOrder(input);

            var sb = new StringBuilder();
            sb.AppendLine($"Входная строка: {input}");
            sb.AppendLine("('*' обозначает пустой указатель NULL)");
            sb.AppendLine();
            sb.AppendLine("СТРУКТУРА ПОСТРОЕННОГО БИНАРНОГО ДЕРЕВА:");
            sb.Append(SerializedBinaryTree.FormatTreeAscii(root));
            sb.AppendLine();
            sb.AppendLine("ОБХОДЫ ДЕРЕВА:");
            sb.Append(SerializedBinaryTree.GetTraversalsReport(root));

            BinaryTreeOutputBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            BinaryTreeOutputBox.Text = $"Ошибка построения дерева: {ex.Message}";
        }
    }
}
