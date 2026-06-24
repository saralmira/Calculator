using System;
using System.Linq.Expressions;
using System.Text;

namespace LoreSoft.MathExpressions
{
    public enum EvalFlag
    {
        None,
        Hex,
        Hex64,
        HexFloat,
        HexDouble
    }

    public class ExpressionData
    {
        public ExpressionData(string expression)
        {
            Result = 0;
            Regular = true;
            Flag = EvalFlag.None;
            Expression = new StringBuilder(expression);
        }

        public string String { get { return Expression.ToString(); } set { Expression = new StringBuilder(value); } }

        public StringBuilder Expression;
        public decimal Result;
        public bool Regular;
        public EvalFlag Flag;
    }

    internal class ExpressionHelper
    {
        private StringBuilder _s;

        private int _pos;

        private int _length;

        public ExpressionHelper(string s)
        {
            if (s == null) 
                throw new ArgumentNullException("s");
            _s = new StringBuilder(s);
            _pos = 0;
            _length = s.Length;
        }

        public ExpressionHelper(StringBuilder sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");
            _s = sb;
            _pos = 0;
            _length = sb.Length;
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            _s = null;
            _pos = 0;
            _length = 0;
        }

        public int Peek()
        {
            if (_pos == _length)
                return -1;

            return _s[_pos];
        }

        public int Read()
        {
            if (_pos == _length)
                return -1;

            return _s[_pos++];
        }

        public void Modify(char newChar)
        {
            _s[_pos] = newChar;
        }

        public void ModifyLast(char newChar)
        {
            _s[_pos - 1] = newChar;
        }

        public int Position { get { return _pos; } }
        public int Length { get { return _length; } }
        public string String { get { return _s.ToString(); } }
    }
}
