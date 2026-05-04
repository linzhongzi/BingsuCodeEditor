using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BingsuCodeEditor.CodeAnalyzer;

namespace BingsuCodeEditor.AutoCompleteToken
{
    public class Block
    {
        //变量、数组、对象等等。

        //对于对象来说，容器是相连的。
        public Container ParentContainer;

        public string Scope;


        //const, var
        public string BlockDefine;

        //对象
        public string BlockType;

        //变量名
        public string BlockName;

        public bool IsArg;

        public string RawText;

        public List<CodeAnalyzer.TOKEN> Values;

        public TOKEN StartToken;

        public PreCompletionData PreCompletion;

        public Block(Container parentcontainer, string blockdefine, string blockname, TOKEN StartToken, string blocktype = "", List<CodeAnalyzer.TOKEN> values = null, bool IsArg = false)
        {
            this.StartToken = StartToken;
            this.ParentContainer = parentcontainer;
            this.BlockDefine = blockdefine;
            this.BlockType = blocktype;
            this.BlockName = blockname;
            this.Values = values;
            this.IsArg = IsArg;

            if(blockdefine == "var")
            {
                PreCompletion = new ObjectItem(CompletionWordType.Variable, blockname, block:this);
            }
            else
            {
                PreCompletion = new ObjectItem(CompletionWordType.Const, blockname, block: this);
            }
        }
    }
}
