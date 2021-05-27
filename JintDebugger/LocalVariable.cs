using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JintDebugger
{
    public class LocalVariable
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Value { get; set; }
        private IList<LocalVariable> _Children = null;
        public IList<LocalVariable> Children
        {
            get
            {
                if (this._Children == null)
                {
                    this._Children = new List<LocalVariable>();
                }
                return this._Children;
            }
        }
    }
}
