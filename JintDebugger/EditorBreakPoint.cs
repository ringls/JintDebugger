using Jint.Runtime.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JintDebugger
{
    public class EditorBreakPoint
    {
        public int Line { get; private set; }
        public int Column { get; private set; }
        public EditorBreakPoint(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }
}
