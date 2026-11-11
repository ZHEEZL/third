using System;
using System.Collections.Generic;
using System.Text;

namespace third.Models;

public class IntListNode
{
    public int Value { get; set; }
    public IntListNode? Next { get; set; }

    public IntListNode(int value, IntListNode? next = null)
    {
        Value = value;
        Next = next;
    }
}

/// <summary>
/// Часть 4: СВОЙ односвязный список целых чисел и все 12 алгоритмов работы со ссылками.
/// </summary>
public class CustomLinkedList
{
    public IntListNode? Head { get; private set; }

    public CustomLinkedList() { }

    public CustomLinkedList(IEnumerable<int> values)
    {
        foreach (var v in values)
            AddLast(v);
    }

    public static CustomLinkedList FromString(string text)
    {
        var list = new CustomLinkedList();
        var parts = text.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (int.TryParse(p.Trim(), out int val))
                list.AddLast(val);
        }
        return list;
    }

    public void AddLast(int val)
    {
        var newNode = new IntListNode(val);
        if (Head == null)
        {
            Head = newNode;
            return;
        }
        var cur = Head;
        while (cur.Next != null)
            cur = cur.Next;
        cur.Next = newNode;
    }

    public bool IsEmpty => Head == null;

    public string FormatList()
    {
        if (Head == null) return "[Пустой список]";
        var sb = new StringBuilder();
        var cur = Head;
        while (cur != null)
        {
            sb.Append($"[{cur.Value}]");
            if (cur.Next != null)
                sb.Append(" -> ");
            else
                sb.Append(" -> NULL");
            cur = cur.Next;
        }
        return sb.ToString();
    }

    public List<int> ToList()
    {
        var res = new List<int>();
        var cur = Head;
        while (cur != null)
        {
            res.Add(cur.Value);
            cur = cur.Next;
        }
        return res;
    }

    // ------------------------------------------------------------------------
    // 1. Переворот списка L (изменение ссылок в обратном порядке)
    // ------------------------------------------------------------------------
    public void Reverse()
    {
        IntListNode? prev = null;
        IntListNode? current = Head;
        while (current != null)
        {
            IntListNode? next = current.Next;
            current.Next = prev;
            prev = current;
            current = next;
        }
        Head = prev;
    }

    // ------------------------------------------------------------------------
    // 2а. Перенос последнего элемента непустого списка L в его начало
    // ------------------------------------------------------------------------
    public void MoveLastToFirst()
    {
        if (Head == null || Head.Next == null)
            return;

        IntListNode prev = Head;
        IntListNode last = Head.Next;
        while (last.Next != null)
        {
            prev = last;
            last = last.Next;
        }

        prev.Next = null;
        last.Next = Head;
        Head = last;
    }

    // ------------------------------------------------------------------------
    // 2б. Перенос первого элемента непустого списка L в его конец
    // ------------------------------------------------------------------------
    public void MoveFirstToLast()
    {
        if (Head == null || Head.Next == null)
            return;

        IntListNode first = Head;
        Head = Head.Next;
        first.Next = null;

        IntListNode cur = Head;
        while (cur.Next != null)
            cur = cur.Next;
        cur.Next = first;
    }

    // ------------------------------------------------------------------------
    // 3. Определение количества различных элементов списка целых чисел
    // ------------------------------------------------------------------------
    public int CountDistinct()
    {
        int distinctCount = 0;
        for (var cur = Head; cur != null; cur = cur.Next)
        {
            bool seenBefore = false;
            for (var check = Head; check != cur; check = check!.Next)
            {
                if (check!.Value == cur.Value)
                {
                    seenBefore = true;
                    break;
                }
            }
            if (!seenBefore)
                distinctCount++;
        }
        return distinctCount;
    }

    // ------------------------------------------------------------------------
    // 4. Удаление из списка L неуникальных элементов (дубликатов)
    // Оставляет только первое вхождение каждого числа (или удаляет все повторы)
    // ------------------------------------------------------------------------
    public void RemoveDuplicatesKeepFirst()
    {
        for (var cur = Head; cur != null; cur = cur.Next)
        {
            var runner = cur;
            while (runner.Next != null)
            {
                if (runner.Next.Value == cur.Value)
                    runner.Next = runner.Next.Next;
                else
                    runner = runner.Next;
            }
        }
    }

    /// <summary>
    /// Альтернативная трактовка п.4: удалить вообще все элементы, встречающиеся более 1 раза
    /// </summary>
    public void RemoveAllNonUniqueStrict()
    {
        var dummy = new IntListNode(0, Head);
        var prev = dummy;
        var cur = Head;

        while (cur != null)
        {
            int count = 0;
            for (var p = Head; p != null; p = p.Next)
            {
                if (p.Value == cur.Value) count++;
            }

            if (count > 1)
                prev.Next = cur.Next;
            else
                prev = cur;

            cur = cur.Next;
        }
        Head = dummy.Next;
    }

    // ------------------------------------------------------------------------
    // 5. Вставка списка самого в себя вслед за первым вхождением числа x
    // ------------------------------------------------------------------------
    public bool InsertSelfAfterFirstX(int x)
    {
        if (Head == null) return false;

        // Сначала находим первое вхождение x
        IntListNode? target = Head;
        while (target != null && target.Value != x)
            target = target.Next;

        if (target == null)
            return false;

        // Создаем полную копию исходных узлов списка, чтобы не образовать бесконечный цикл
        IntListNode? copyHead = null;
        IntListNode? copyTail = null;
        for (var cur = Head; cur != null; cur = cur.Next)
        {
            var newNode = new IntListNode(cur.Value);
            if (copyHead == null)
                copyHead = copyTail = newNode;
            else
            {
                copyTail!.Next = newNode;
                copyTail = newNode;
            }
        }

        if (copyHead != null && copyTail != null)
        {
            copyTail.Next = target.Next;
            target.Next = copyHead;
        }
        return true;
    }

    // ------------------------------------------------------------------------
    // 6. Вставка в упорядоченный по неубыванию список L нового элемента E
    // с сохранением упорядоченности
    // ------------------------------------------------------------------------
    public void InsertOrdered(int e)
    {
        // Если список не отсортирован, предварительно упорядочим его по неубыванию
        SortAscending();

        var newNode = new IntListNode(e);
        if (Head == null || e <= Head.Value)
        {
            newNode.Next = Head;
            Head = newNode;
            return;
        }

        var cur = Head;
        while (cur.Next != null && cur.Next.Value < e)
            cur = cur.Next;

        newNode.Next = cur.Next;
        cur.Next = newNode;
    }

    public void SortAscending()
    {
        if (Head == null || Head.Next == null) return;
        bool swapped;
        do
        {
            swapped = false;
            var cur = Head;
            while (cur.Next != null)
            {
                if (cur.Value > cur.Next.Value)
                {
                    (cur.Value, cur.Next.Value) = (cur.Next.Value, cur.Value);
                    swapped = true;
                }
                cur = cur.Next;
            }
        } while (swapped);
    }

    // ------------------------------------------------------------------------
    // 7. Удаление из списка L всех элементов E, если таковые имеются
    // ------------------------------------------------------------------------
    public int RemoveAll(int e)
    {
        int removed = 0;
        while (Head != null && Head.Value == e)
        {
            Head = Head.Next;
            removed++;
        }

        var cur = Head;
        while (cur != null && cur.Next != null)
        {
            if (cur.Next.Value == e)
            {
                cur.Next = cur.Next.Next;
                removed++;
            }
            else
            {
                cur = cur.Next;
            }
        }
        return removed;
    }

    // ------------------------------------------------------------------------
    // 8. Вставка в список L нового элемента F перед первым вхождением элемента E
    // ------------------------------------------------------------------------
    public bool InsertBeforeFirst(int e, int f)
    {
        if (Head == null) return false;

        if (Head.Value == e)
        {
            Head = new IntListNode(f, Head);
            return true;
        }

        var cur = Head;
        while (cur.Next != null && cur.Next.Value != e)
            cur = cur.Next;

        if (cur.Next == null)
            return false;

        cur.Next = new IntListNode(f, cur.Next);
        return true;
    }

    // ------------------------------------------------------------------------
    // 9. Дописывание к списку L списка E (оба содержат целые числа)
    // ------------------------------------------------------------------------
    public void AppendList(CustomLinkedList listE)
    {
        for (var cur = listE.Head; cur != null; cur = cur.Next)
        {
            AddLast(cur.Value);
        }
    }

    // ------------------------------------------------------------------------
    // 10. Разбиение списка на два по первому вхождению заданного числа X.
    // Если этого числа нет, второй список пуст, а первый не изменится.
    // ------------------------------------------------------------------------
    public CustomLinkedList SplitByFirst(int x)
    {
        var secondList = new CustomLinkedList();
        if (Head == null) return secondList;

        var cur = Head;
        while (cur != null && cur.Value != x)
            cur = cur.Next;

        if (cur == null)
            return secondList; // числа нет -> второй пуст, первый без изменений

        // Разрезаем список сразу после первого вхождения x
        secondList.Head = cur.Next;
        cur.Next = null;
        return secondList;
    }

    // ------------------------------------------------------------------------
    // 11. Удвоение списка (приписывание в конец списка себя самого)
    // ------------------------------------------------------------------------
    public void DuplicateSelf()
    {
        if (Head == null) return;

        IntListNode? copyHead = null;
        IntListNode? copyTail = null;
        IntListNode? origTail = null;

        for (var cur = Head; cur != null; cur = cur.Next)
        {
            origTail = cur;
            var node = new IntListNode(cur.Value);
            if (copyHead == null)
                copyHead = copyTail = node;
            else
            {
                copyTail!.Next = node;
                copyTail = node;
            }
        }

        if (origTail != null)
            origTail.Next = copyHead;
    }

    // ------------------------------------------------------------------------
    // 12. Обмен местами двух элементов списка, заданных пользователем
    // (перестановка ссылок узлов для первых вхождений val1 и val2)
    // ------------------------------------------------------------------------
    public bool SwapElements(int val1, int val2)
    {
        if (val1 == val2) return true;

        IntListNode? prevX = null, currX = Head;
        while (currX != null && currX.Value != val1)
        {
            prevX = currX;
            currX = currX.Next;
        }

        IntListNode? prevY = null, currY = Head;
        while (currY != null && currY.Value != val2)
        {
            prevY = currY;
            currY = currY.Next;
        }

        if (currX == null || currY == null)
            return false;

        if (prevX != null)
            prevX.Next = currY;
        else
            Head = currY;

        if (prevY != null)
            prevY.Next = currX;
        else
            Head = currX;

        IntListNode? temp = currY.Next;
        currY.Next = currX.Next;
        currX.Next = temp;

        return true;
    }
}
