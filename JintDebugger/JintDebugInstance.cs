using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JintDebugger
{
    public class JintDebugInstance
    {
        public string InputJson { get; set; }  

        public string Script { get; set; }
        private IList<EditorBreakPoint> _BreakPoints = null;
        public IList<EditorBreakPoint> BreakPoints
        {
            get
            {
                if (this._BreakPoints == null)
                {
                    this._BreakPoints = new List<EditorBreakPoint>();
                }
                return this._BreakPoints;
            }
            set { this._BreakPoints = value; }
        }
    }
}
