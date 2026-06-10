using LoreSoft.MathExpressions.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using static LoreSoft.MathExpressions.MathEvaluator;

namespace LoreSoft.MathExpressions
{
    /// <summary>
    /// A class representing the System.Math function expressions
    /// </summary>
    public class FunctionExpression : ExpressionBase
    {
        class FunctionDefinition
        {
            public string Name;
            public int ArgumentCount;

            public virtual decimal Execute(decimal[] numbers)
            {
                Type[] desiredMethodSignatureArgs;

                switch (ArgumentCount)
                {
                    case 1:
                        desiredMethodSignatureArgs = new[] { typeof(double) };
                        break;
                    case 2:
                        desiredMethodSignatureArgs = new[] { typeof(double), typeof(double) };
                        break;
                    default:
                        desiredMethodSignatureArgs = new[] { typeof(void) };
                        break;
                }

                string function = char.ToUpperInvariant(Name[0]) + Name.Substring(1);
                MethodInfo method = typeof(Math).GetMethod(
                    function,
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    desiredMethodSignatureArgs,
                    null);

                if (method == null)
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.CurrentCulture,
                            Resources.InvalidFunctionName, Name));
                }

                object[] parameters = new object[numbers.Length];
                for (int i = 0; i < numbers.Length; ++i)
                    parameters[i] = (double)numbers[i];

                // Array.Copy(numbers, parameters, numbers.Length);
                return (decimal)(double)method.Invoke(null, parameters);
            }
       
            public virtual EvalFlag GetFlag() { return EvalFlag.None; }
        }

        class HexFunctionDefinition : FunctionDefinition
        {
            public override decimal Execute(decimal[] numbers) { return numbers[0]; }

            public override EvalFlag GetFlag() { return EvalFlag.Hex; }
        }

        class Hex64FunctionDefinition : HexFunctionDefinition { public override EvalFlag GetFlag() { return EvalFlag.Hex64; } }

        // must be sorted
        /// <summary>The supported single argument math functions by this class.</summary>
        private static readonly string[] oneArgumentMathFunctions = new string[]
            {
                "abs", "acos", "asin", "atan", "ceiling", "cos", "cosh", "exp",
                "floor", "log", "log10", "sin", "sinh", "sqrt", "tan", "tanh"
            };

        // must be sorted
        /// <summary>The supported two argument math functions by this class.</summary>
        private static readonly string[] twoArgumentMathFunctions = new string[]
            {
                "max", "min", "pow", "round"
            };

        private static readonly Dictionary<string, FunctionDefinition> FunctionArray = new Dictionary<string, FunctionDefinition>();

        /// <summary>Initializes a new instance of the <see cref="FunctionExpression"/> class.</summary>
        /// <param name="function">The function name for this instance.</param>
        public FunctionExpression(string function) : this(function, true)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="FunctionExpression"/> class.</summary>
        /// <param name="function">The function.</param>
        /// <param name="validate">if set to <c>true</c> to validate the function name.</param>
        internal FunctionExpression(string function, bool validate)
        {
            function = function.ToLowerInvariant();

            if (validate && !IsFunction(function))
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, Resources.InvalidFunctionName, _function),
                    "function");

            _function = function;
            _flag = FunctionArray[_function].GetFlag();
            base.Evaluate = new MathEvaluate(Execute);
        }

        private string _function;

        /// <summary>Gets the name function for this instance.</summary>
        /// <value>The function name.</value>
        public string Function
        {
            get { return _function; }
        }

        private EvalFlag _flag;
        public EvalFlag Flag { get { return _flag; } }

        /// <summary>Executes the function on specified numbers.</summary>
        /// <param name="numbers">The numbers used in the function.</param>
        /// <returns>The result of the function execution.</returns>
        /// <exception cref="ArgumentNullException">When numbers is null.</exception>
        /// <exception cref="ArgumentException">When the length of numbers do not equal <see cref="ArgumentCount"/>.</exception>
        public decimal Execute(decimal[] numbers)
        {
            base.Validate(numbers);

            if (FunctionArray.TryGetValue(_function, out var function))
            {
                return function.Execute(numbers);
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                        Resources.InvalidFunctionName, _function));
            }
        }

        /// <summary>Gets the number of arguments this expression uses.</summary>
        /// <value>The argument count.</value>
        public override int ArgumentCount
        {
            get
            {
                return FunctionArray[_function].ArgumentCount;
            }
        }

        /// <summary>Determines whether the specified function name is a function.</summary>
        /// <param name="function">The function name.</param>
        /// <returns><c>true</c> if the specified name is a function; otherwise, <c>false</c>.</returns>
        public static bool IsFunction(string function)
        {
            return FunctionArray.ContainsKey(function);
        }

        /// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.</summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.</returns>
        /// <filterPriority>2</filterPriority>
        public override string ToString()
        {
            return _function;
        }

        /// <summary>
        /// Gets the function names.
        /// </summary>
        /// <returns>An array of function names.</returns>
        public static string[] GetFunctionNames()
        {
            foreach (var arg in oneArgumentMathFunctions)
                FunctionArray[arg] = new FunctionDefinition { ArgumentCount = 1, Name = arg };
            foreach (var arg in twoArgumentMathFunctions)
                FunctionArray[arg] = new FunctionDefinition { ArgumentCount = 2, Name = arg };
            FunctionArray["hex"] = new HexFunctionDefinition { ArgumentCount = 1, Name = "hex" };
            FunctionArray["hex64"] = new Hex64FunctionDefinition { ArgumentCount = 1, Name = "hex64" };
            return FunctionArray.Keys.ToArray();
        }
    }
}