using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace third.Models;

public class RpnStep
{
    public int StepNumber { get; set; }
    public string Token { get; set; } = "";
    public string ActionDescription { get; set; } = "";
    public List<string> StackSnapshotBottomUp { get; set; } = new();
    public string StackFormatted => StackSnapshotBottomUp.Count == 0
        ? "[пусто]"
        : string.Join(" | ", StackSnapshotBottomUp);
}

public class RpnEvaluationResult
{
    public double Result { get; set; }
    public string PostfixExpression { get; set; } = "";
    public List<RpnStep> Steps { get; set; } = new();
    public int TokenCount { get; set; }
    public int PushCount { get; set; }
    public int PopCount { get; set; }
    public int MaxStackDepth { get; set; }
    public string ComplexityAnalysis { get; set; } = "";
}

/// <summary>
/// Часть 1: Перевод инфиксной записи в постфиксную (ОПЗ) и вычисление постфиксного выражения
/// с использованием СВОЕГО стека CustomStack&lt;T&gt;.
/// Поддерживаемые операции: +, -, *, :, /, ^, ln, cos, sin, sqrt, скобки ( ).
/// </summary>
public static class RpnCalculator
{
    private static bool IsFunction(string token) =>
        token.Equals("ln", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("cos", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("sin", StringComparison.OrdinalIgnoreCase) ||
        token.Equals("sqrt", StringComparison.OrdinalIgnoreCase);

    private static bool IsBinaryOperator(string token) =>
        token is "+" or "-" or "*" or ":" or "/" or "^";

    private static int GetPrecedence(string op) => op.ToLowerInvariant() switch
    {
        "+" or "-" => 1,
        "*" or ":" or "/" => 2,
        "^" => 3,
        "ln" or "cos" or "sin" or "sqrt" => 4,
        _ => 0
    };

    private static bool IsRightAssociative(string op) =>
        op == "^" || IsFunction(op);

    /// <summary>
    /// Разбивает инфиксную строку на токены (числа, операторы, функции, скобки)
    /// </summary>
    public static List<string> TokenizeInfix(string infix)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < infix.Length)
        {
            char c = infix[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // Число (или унарный минус в начале/после скобки/оператора)
            if (char.IsDigit(c) || c == '.' || c == ',' ||
                (c == '-' && (tokens.Count == 0 || tokens[^1] == "(" || IsBinaryOperator(tokens[^1])) &&
                 i + 1 < infix.Length && (char.IsDigit(infix[i + 1]) || infix[i + 1] == '.')))
            {
                var sb = new StringBuilder();
                if (c == '-')
                {
                    sb.Append('-');
                    i++;
                }
                while (i < infix.Length && (char.IsDigit(infix[i]) || infix[i] == '.' || infix[i] == ','))
                {
                    sb.Append(infix[i] == ',' ? '.' : infix[i]);
                    i++;
                }
                tokens.Add(sb.ToString());
                continue;
            }

            // Имя функции (ln, sin, cos, sqrt)
            if (char.IsLetter(c))
            {
                var sb = new StringBuilder();
                while (i < infix.Length && char.IsLetter(infix[i]))
                {
                    sb.Append(char.ToLowerInvariant(infix[i]));
                    i++;
                }
                tokens.Add(sb.ToString());
                continue;
            }

            // Операторы и скобки
            if ("+-*:^/()".IndexOf(c) >= 0)
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            throw new ArgumentException($"Недопустимый символ '{c}' в позиции {i}.");
        }
        return tokens;
    }

    /// <summary>
    /// Перевод формулы из инфиксной записи в постфиксную (алгоритм сортировочной станции Дейкстры)
    /// с использованием СВОЕГО стека CustomStack&lt;string&gt;.
    /// </summary>
    public static string InfixToPostfix(string infix)
    {
        var tokens = TokenizeInfix(infix);
        var output = new List<string>();
        var opStack = new CustomStack<string>();

        foreach (var token in tokens)
        {
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                output.Add(token);
            }
            else if (IsFunction(token))
            {
                opStack.Push(token);
            }
            else if (token == "(")
            {
                opStack.Push(token);
            }
            else if (token == ")")
            {
                while (!opStack.IsEmpty() && opStack.Top() != "(")
                {
                    output.Add(opStack.Pop());
                }
                if (opStack.IsEmpty())
                    throw new ArgumentException("Несогласованные скобки: отсутствует открывающая '('.");

                opStack.Pop(); // удаляем "("

                if (!opStack.IsEmpty() && IsFunction(opStack.Top()))
                {
                    output.Add(opStack.Pop());
                }
            }
            else if (IsBinaryOperator(token))
            {
                while (!opStack.IsEmpty() && opStack.Top() != "(")
                {
                    string topOp = opStack.Top();
                    int topPrec = GetPrecedence(topOp);
                    int currPrec = GetPrecedence(token);

                    if (topPrec > currPrec || (topPrec == currPrec && !IsRightAssociative(token)))
                    {
                        output.Add(opStack.Pop());
                    }
                    else
                    {
                        break;
                    }
                }
                opStack.Push(token);
            }
            else
            {
                throw new ArgumentException($"Неизвестный токен: {token}");
            }
        }

        while (!opStack.IsEmpty())
        {
            string top = opStack.Pop();
            if (top is "(" or ")")
                throw new ArgumentException("Несогласованные скобки в выражении.");
            output.Add(top);
        }

        return string.Join(" ", output);
    }

    /// <summary>
    /// Вычисление выражения, записанного в постфиксной записи (ОПЗ),
    /// с пошаговой трассировкой стека и расчетом оценки сложности алгоритма.
    /// </summary>
    public static RpnEvaluationResult EvaluatePostfix(string postfix)
    {
        var rawTokens = postfix.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var stack = new CustomStack<double>();
        var steps = new List<RpnStep>();

        int pushCount = 0;
        int popCount = 0;
        int maxDepth = 0;
        int stepNum = 1;

        foreach (var rawToken in rawTokens)
        {
            // Игнорируем случайно попавшие закрывающие скобки, если они есть в файле по условию «)»
            string token = rawToken.Trim();
            if (token == ")" || token == "(")
                continue;

            string normalizedNumber = token.Replace(',', '.');
            if (double.TryParse(normalizedNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
            {
                stack.Push(num);
                pushCount++;
                if (stack.Count > maxDepth) maxDepth = stack.Count;

                steps.Add(new RpnStep
                {
                    StepNumber = stepNum++,
                    Token = token,
                    ActionDescription = $"Push({num.ToString(CultureInfo.InvariantCulture)}) в стек",
                    StackSnapshotBottomUp = FormatStack(stack)
                });
            }
            else if (IsBinaryOperator(token))
            {
                if (stack.Count < 2)
                    throw new InvalidOperationException($"Недостаточно операндов в стеке для бинарной операции '{token}'.");

                double b = stack.Pop();
                double a = stack.Pop();
                popCount += 2;

                double res = token switch
                {
                    "+" => a + b,
                    "-" => a - b,
                    "*" => a * b,
                    ":" or "/" => b == 0 ? throw new DivideByZeroException("Деление на ноль!") : a / b,
                    "^" => Math.Pow(a, b),
                    _ => throw new InvalidOperationException($"Неизвестная операция {token}")
                };

                stack.Push(res);
                pushCount++;
                if (stack.Count > maxDepth) maxDepth = stack.Count;

                steps.Add(new RpnStep
                {
                    StepNumber = stepNum++,
                    Token = token,
                    ActionDescription = $"Pop({FormatNum(b)}), Pop({FormatNum(a)}) -> {FormatNum(a)} {token} {FormatNum(b)} = {FormatNum(res)} -> Push({FormatNum(res)})",
                    StackSnapshotBottomUp = FormatStack(stack)
                });
            }
            else if (IsFunction(token))
            {
                if (stack.IsEmpty())
                    throw new InvalidOperationException($"Недостаточно операндов в стеке для унарной функции '{token}'.");

                double a = stack.Pop();
                popCount++;

                double res = token.ToLowerInvariant() switch
                {
                    "ln" => a <= 0 ? throw new ArgumentException("Аргумент ln должен быть > 0") : Math.Log(a),
                    "cos" => Math.Cos(a),
                    "sin" => Math.Sin(a),
                    "sqrt" => a < 0 ? throw new ArgumentException("Аргумент sqrt должен быть >= 0") : Math.Sqrt(a),
                    _ => throw new InvalidOperationException($"Неизвестная функция {token}")
                };

                stack.Push(res);
                pushCount++;
                if (stack.Count > maxDepth) maxDepth = stack.Count;

                steps.Add(new RpnStep
                {
                    StepNumber = stepNum++,
                    Token = token,
                    ActionDescription = $"Pop({FormatNum(a)}) -> {token}({FormatNum(a)}) = {FormatNum(res)} -> Push({FormatNum(res)})",
                    StackSnapshotBottomUp = FormatStack(stack)
                });
            }
            else
            {
                throw new ArgumentException($"Неизвестный токен в постфиксной записи: '{token}'");
            }
        }

        if (stack.IsEmpty())
            throw new InvalidOperationException("Выражение пусто.");
        if (stack.Count > 1)
            throw new InvalidOperationException($"После вычисления в стеке осталось {stack.Count} элементов (ожидался 1). Проверьте корректность записи.");

        double finalResult = stack.Pop();
        popCount++;

        int n = steps.Count;
        var complexitySb = new StringBuilder();
        complexitySb.AppendLine($"• Количество токенов в выражении (N): {n}");
        complexitySb.AppendLine($"• Выполнено операций Push (O(1)): {pushCount}");
        complexitySb.AppendLine($"• Выполнено операций Pop (O(1)): {popCount}");
        complexitySb.AppendLine($"• Суммарное число элементарных операций со стеком: {pushCount + popCount} <= 3N");
        complexitySb.AppendLine($"• Максимальная глубина стека (память S): {maxDepth} элементов");
        complexitySb.AppendLine($"• Временная сложность алгоритма: T(N) = O(N) — линейная (каждый токен обрабатывается ровно 1 раз за O(1)).");
        complexitySb.AppendLine($"• Пространственная сложность: M(N) = O(N) в худшем случае (фактически использовано {maxDepth} узлов).");

        return new RpnEvaluationResult
        {
            Result = finalResult,
            PostfixExpression = postfix,
            Steps = steps,
            TokenCount = n,
            PushCount = pushCount,
            PopCount = popCount,
            MaxStackDepth = maxDepth,
            ComplexityAnalysis = complexitySb.ToString()
        };
    }

    private static List<string> FormatStack(CustomStack<double> stack)
    {
        var list = stack.ToBottomUpList();
        var formatted = new List<string>(list.Count);
        foreach (var d in list)
            formatted.Add(FormatNum(d));
        return formatted;
    }

    private static string FormatNum(double d) =>
        Math.Round(d, 4).ToString(CultureInfo.InvariantCulture);
}
