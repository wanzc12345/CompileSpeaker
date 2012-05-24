using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS
{
    public class SyntaxDescriptor
    {
        public char leftBracket = '(';
        public char rightBracket = ')';
        public char leftBracket2 = '\x0';
        public char rightBracket2 = '\x0';
        public readonly List<Style> styles = new List<Style>();
        public readonly List<RuleDesc> rules = new List<RuleDesc>();
        public readonly List<FoldingDesc> foldings = new List<FoldingDesc>();
    }

    public class RuleDesc
    {
        public string regex;
        public RegexOptions options = RegexOptions.None;
        public Style style;
    }

    public class FoldingDesc
    {
        public string startMarkerRegex;
        public string finishMarkerRegex;
        public RegexOptions options = RegexOptions.None;
    }
}
