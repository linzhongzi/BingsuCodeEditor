using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static BingsuCodeEditor.CodeAnalyzer;

namespace BingsuCodeEditor.AutoCompleteToken
{
    public abstract class Function
    {
        //function fname(args){}
        ///******/function fname(args){}
        /***
         * @Type
         * F
         * @Summary.ko-KR
         * [loc]에 존재하는 [player]의 [unit]을 반환합니다.
         * @param.player.ko-KR
         * 유닛의 소유 플레이어입니다.
         * @param.unit.ko-KR
         * 유닛입니다.
         * @param.loc.ko-KR
         * 로케이션입니다.
         *
         * @Summary.en-US
         * Returns the [unit] of the [player] at [loc].
         * @param.player.en-US
         * The player who owns the unit.
         * @param.unit.en-US
         * The unit.
         * @param.loc.en-US
         * The location.
         *
         * @Summary.zh-CN
         * 返回位于 [loc] 的 [player] 的 [unit]。
         * @param.player.zh-CN
         * 指定单位的拥有玩家。
         * @param.unit.zh-CN
         * 指定单位.
         * @param.loc.zh-CN
         * 指定位置.
        ***/
        //原创内容

        public Container parentcontainer;

        public string scope;
        public CodeAnalyzer.CursorLocation cursorLocation;

        public PreCompletionData preCompletion;

        public bool IsInCursor = false;
        public bool IsPredefine = false;

        public bool IsStatic = false;

        //函数名
        public string special;
        public string funcname;
        public string funcsummary;

        public string comment;

        public TOKEN StartToken;

        public List<TOKEN> returntype;

        public List<Arg> args = new List<Arg>();
        //Arg
        public class Arg
        {
            public string argname;
            public string argtype;
            public string InitValue;
            public bool IsList;
        }
        protected Dictionary<string, string> argsummary = new Dictionary<string, string>();

        public Function(Container parentcontainer, TOKEN StartToken)
        {
            this.StartToken = StartToken;
            this.parentcontainer = parentcontainer;
        }


        public string GetArgSummary(string arg)
        {
            if (argsummary.ContainsKey(arg))
            {
                return argsummary[arg];
            }
            else
            {
                return "";
            }
        }


        public string GetArgString(int argindex = -1)
        {
            string argstring = "";
            for (int i = 0; i < args.Count; i++)
            {
                if (i != 0)
                {
                    argstring += ", ";
                }

                if(argindex == i)
                {
                    argstring += "​";
                }
                argstring += args[i].argname;
                if (!string.IsNullOrEmpty(args[i].argtype))
                {
                    argstring += " : " + args[i].argtype;
                }
            }

            return argstring;
        }



        public abstract void ReadComment(string comment);
    }
}
