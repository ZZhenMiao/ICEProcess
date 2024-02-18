using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE_Model
{
    public enum Grundlagen { FromPath, FromFileName, FromInput }

    public class Variable
    {
        public int MyProperty { get; set; }
        public Grundlagen Grundlagen { get; set; }
    }

    public class ProjectConstructor
    {



        public int MyProperty { get; set; }
    }
}
