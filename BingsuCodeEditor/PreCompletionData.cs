using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BingsuCodeEditor.CodeAnalyzer;

namespace BingsuCodeEditor
{
    public abstract class PreCompletionData
    {
        public CompletionWordType completionWordType;


        public PreCompletionData(CompletionWordType completionType, string name, string outputstring = "", string desc = "")
        {
            this.completionWordType = completionType;
            this.name = name;
            this._outputstring = outputstring;
            this._desc = desc;


        }

        //关键字名称
        protected string name;
        protected string _outputstring;
        protected string _desc;
        public abstract string listheader { get; }
        public abstract string outputstring { get; }
        public abstract string desc { get; }


        public double Priority;



        //自动输入预设
        public string AutoInsert;
    }
}
