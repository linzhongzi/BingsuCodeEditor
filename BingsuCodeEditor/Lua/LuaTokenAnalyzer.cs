using BingsuCodeEditor.AutoCompleteToken;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BingsuCodeEditor.CodeAnalyzer;

namespace BingsuCodeEditor.Lua
{
    public class LuaTokenAnalyzer : TokenAnalyzer
    {
        public LuaTokenAnalyzer(CodeAnalyzer codeAnalyzer) : base(codeAnalyzer)
        {
        }

        public override Container ConatainerAnalyzer(int startindex = int.MaxValue)
        {
            IsError = false;

            Container rcontainer = new Container(codeAnalyzer);
            Container cc = rcontainer;


            bool isinstartoffset = true;

            int currentscope = 0;
            string scope = "st";
            string lastblockscope = "st";
            

            TOKEN tk = null;

            while (!IsEndOfList())
            {
                tk = GetCurrentToken();
                if(tk == null)
                {
                    break;
                }
                tk.scope = scope;
                if (tk.StartOffset > startindex && isinstartoffset)
                {
                    isinstartoffset = false;
                    lastblockscope = scope;
                    //break;
                }

                //如果是在里面就好了函数。

                //如果您不想对一个对象进行两次操作，请充分了解它的结构。

                switch (tk.Type)
                {
                    case TOKEN_TYPE.KeyWord:
                        switch (tk.Value)
                        {
                            case "function":

                                string lastscope = scope;

                                currentscope++;
                                scope += "." + (currentscope).ToString().PadLeft(4, '0');


                                Function function = FunctionAnalyzer(rcontainer, startindex, scope);

                                //if(rcontainer.funcs.Find(x=> ((x.funcname == function.funcname) && (x.IsPredefine == false))) != null)
                                //{
                                //    ThrowException("函数 " + function.funcname + " 声明为重复。", tk, 1);
                                //}

                                if (cc.CheckIdentifier(lastscope, function.funcname))
                                {
                                    ThrowException("함수 " + function.funcname + "는 이미 선언되어 있습니다.", tk, 1);
                                }

                                function.scope = lastscope;


                   
                                if (function.cursorLocation != CursorLocation.None)
                                {
                                    cc.cursorLocation = function.cursorLocation;
                                    rcontainer.cursorLocation = function.cursorLocation;
                                }

                                cc.funcs.Add(function);
                                //添加参数
                                foreach (var item in function.args)
                                {
                                    Block bl = new Block(rcontainer, "var", item.argname, tk, item.argtype, IsArg: true);
                                    bl.Scope = scope;
                                    cc.vars.Add(bl);
                                }



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
                                break;
                            case "if":
                            case "while":
                            case "repeat":
                            case "for":
                                //定义新范围
                                currentscope++;
                                scope += "." + (currentscope).ToString().PadLeft(4, '0');

                                break;
                            case "end":
                            case "until":
                                //恢复到之前的范围
                                int t = currentscope.ToString().Length + 1;

                         
                                if (scope == "st")
                                {
                                    //我无法关闭它，但它会关闭
                                    ThrowException("'end'등 스코프가 마무리되지 않았습니다.", tk);
                                }
                                else
                                {
                                    int lastlen = scope.Split('.').Last().Length + 1;

                                    scope = scope.Remove(scope.Length - lastlen, lastlen);
                                    //currentscope = int.Parse(scope.Split('.').Last());
                                }
   

                                break;
                 
                        }

                        break;
                    case TOKEN_TYPE.Identifier:
                        //如果是关键字，检查是否是名称空间命名空间。

                        //在这里做你的分析函数。
                        //对于每个TOKEN，检查当前词元(Token)所属的位置。
                        //，分成两半，检查是正面还是背面。
                        IdentifierFAnalyzer(cc, scope, tk, startindex, main: rcontainer);
                        break;
                    default:


                        break;
                }
            }


            //范围清理
            rcontainer.currentScope = lastblockscope;
            
            if (scope != "st")
            {
                ThrowException("'end'등 스코프가 마무리되지 않았습니다.", tk);
            }


            return rcontainer;
        }



        public List<TOKEN> IdentifierFAnalyzer(Container container, string scope, TOKEN ctk, int startindex, int argindex = -1, Container main = null)
        {
            string fname = ctk.Value;
            ctk.scope = scope;

            int argstartindex = ctk.StartOffset;
            List<TOKEN> tlist = new List<TOKEN>();

       


            tlist.Add(ctk);
            if (argindex != -1)
            {
                //如果您收到副本继承
                ctk.argindex = argindex;
            }

            if(!container.CheckIdentifier(scope, fname))
            {
                Block block = new Block(container, "var", fname, ctk);
                block.Scope = scope;
                container.vars.Add(block);
                //ThrowException(fname + "未声明。", ctk);
            }

            TOKEN tk = null;//GetCurrentToken();

            if (CheckCurrentToken(TOKEN_TYPE.Symbol, "."))
            {
                //.，所以后面的标记
                tlist.AddRange(GetTokenList());
                //tk = GetCurrentToken();
            }

            CheckFunc:
            //宣告命运的开始函数
            if (CheckCurrentToken(TOKEN_TYPE.Symbol, "("))
            {
                int innercount = 1;

                int cargindex = 0;
             
                //继续直到innercount变为0
                while (true)
                {
                    if (IsEndOfList())
                    {
                        if(tk != null)
                        {
                            ThrowException(fname + " 괄호가 정상적으로 닫히지 않았습니다.", tk);
                        }
                        break;
                    }
                    tk = GetCurrentToken();
                    tk.scope = scope;
                    tk.argindex = cargindex;
                    tk.funcname = tlist;

                    switch (tk.Type)
                    {
                        case TOKEN_TYPE.Identifier:
                            IdentifierFAnalyzer(container, scope, tk, startindex, cargindex, main:main);
                            break;
                        case TOKEN_TYPE.Symbol:
                            //， （ ）， ETC。
                            if(tk.Value == ")")
                            {
                                innercount--;
                            }
                            else if (tk.Value == ",")
                            {
                                if (!main.innerFuncInfor.IsInnerFuncinfor &&
                                    argstartindex <= startindex &&  startindex <= tk.StartOffset)
                                {
                                    //当内部结果尚未确定时函数
                                    main.innerFuncInfor.IsInnerFuncinfor = true;
                                    main.innerFuncInfor.argindex = cargindex;
                                    main.innerFuncInfor.funcename = tlist;
                                }
                                argstartindex = tk.StartOffset;
                                cargindex++;
                            }
                            else if (tk.Value == "(")
                            {
                                innercount++;
                            }
                            break;
                    }
                    if (innercount == 0)
                    {
                        if (!main.innerFuncInfor.IsInnerFuncinfor &&
                            argstartindex <= startindex && startindex <= tk.EndOffset)
                        {
                            //当内部结果尚未确定时函数
                            main.innerFuncInfor.IsInnerFuncinfor = true;
                            main.innerFuncInfor.argindex = cargindex;
                            main.innerFuncInfor.funcename = tlist;
                        }
                        argstartindex = tk.StartOffset;
                        break;
                    }
                }
            }

            if (CheckCurrentToken(TOKEN_TYPE.Symbol, ".", IsDirect:true))
            {
                //由于在函数之后继续，所以回到循环路线。
                tlist.AddRange(GetTokenList());
                goto CheckFunc;
            }
            //除此之外，你就out了。

            return tlist;
            //单一词元(Token)
        }




        public Function FunctionAnalyzer(Container container, int startindex, string scope)
        {
            Function function = new LuaFunction(container, null);

            TOKEN commenttoken = GetCommentTokenIten(-2);

            if(commenttoken != null)
            {
                function.comment = commenttoken.Value;
            }

            TOKEN tk = GetCurrentToken();
            if (tk == null) return function;
            function.StartToken = tk;

            tk.scope = scope;
            CursorLocation cl = CursorLocation.None;
            int argstartoffset = tk.EndOffset;
            int argendoffset = 0;
            string funcname = "";
            int findex = CurrentInedx;


            if (tk.Type != TOKEN_TYPE.Identifier)
            {
            

                ThrowException("함수의 이름에는 식별자가 와야 합니다.", tk);
                goto EndLabel;
            }

            funcname += tk.Value;
            function.funcname = funcname;



            if (!CheckCurrentToken(TOKEN_TYPE.Symbol, "("))
            {
                ThrowException("함수의 이름 다음에는 인자선언이 와야 합니다.", tk);
                goto EndLabel;
            }





            while (true)
            {
                Function.Arg arg = new Function.Arg();


                if (CheckCurrentToken(TOKEN_TYPE.Symbol, "*"))
                {
                    //基因型
                    arg.IsList = true;
                }
                tk = GetCurrentToken();
                tk.scope = scope;


                if(tk.Type == TOKEN_TYPE.Identifier)
                {
                    string argname = tk.Value;

                    arg.argname = argname;

                }
                else
                {
                    if(function.args.Count == 0)
                    {
                        if (tk.Type != TOKEN_TYPE.Symbol)
                        {
                            //警告必须是无条件的。
                            argendoffset = tk.EndOffset;
                            ThrowException("잘못된 인자 선언입니다. )가 와야합니다.", tk);
                            goto EndLabel;
                        }
                        if (tk.Value == ")")
                        {
                            argendoffset = tk.EndOffset;
                            break;
                        }
                        //也许没有什么争论。
                    }

                    argendoffset = tk.EndOffset;
                    ThrowException("잘못된 인자 선언입니다. 인자 이름이 와야 합니다.", tk);
                    goto EndLabel;
                }

             
                tk = GetCurrentToken();
                

                if (tk.Value == ")")
                {
                    argendoffset = tk.EndOffset;
                    function.args.Add(arg);
                    break;
                }
                else if (tk.Value == ",")
                {

                }
                else if (tk.Value == "=")
                {
                    tk = GetCurrentToken();
                    arg.InitValue = tk.Value;
                }
                


                function.args.Add(arg);
            }

            findex = CurrentInedx;

      
            EndLabel:


            if (cl == CursorLocation.None)
            {
                if (argstartoffset <= startindex && startindex <= argendoffset)
                {
                    cl = CursorLocation.FunctionArgName;
                }
            }

            function.cursorLocation = cl;




            function.preCompletion = new ObjectItem(CompletionWordType.Function, funcname, function: function);

            CurrentInedx = findex;

            return function;
        }

    
    }
}