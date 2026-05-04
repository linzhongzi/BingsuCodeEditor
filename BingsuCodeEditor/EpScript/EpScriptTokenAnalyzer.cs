using BingsuCodeEditor.AutoCompleteToken;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BingsuCodeEditor.CodeAnalyzer;

namespace BingsuCodeEditor.EpScript
{
    public class EpScriptTokenAnalyzer : TokenAnalyzer
    {

        private List<string> specialKeyword = new List<string>();

        public EpScriptTokenAnalyzer(CodeAnalyzer codeAnalyzer) : base(codeAnalyzer)
        {
            string[] sp = {
                "Enemy", "Ally", "AlliedVictory",
"AtLeast", "AtMost", "Exactly",
"All",
"SetTo", "Add", "Subtract",
"Move", "Patrol", "Attack",
"P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "CurrentPlayer", "Foes", "Allies", "NeutralPlayers", "AllPlayers", "Force1", "Force2", "Force3", "Force4", "NonAlliedVictoryPlayers",
"UnitProperty",
"Enable", "Disable", "Toggle",
"Ore", "Gas", "OreAndGas",
"Total", "Units", "Buildings", "UnitsAndBuildings", "Kills", "Razings", "KillsAndRazings", "Custom",
"Set", "Clear", "Toggle", "Random",
"Set", "Cleared", "None"};
            specialKeyword.AddRange(sp);
        }

        public override Container ConatainerAnalyzer(int startindex = int.MaxValue)
        {
            IsError = false;

            Container rcontainer = new Container(codeAnalyzer);
            Container obj = null;
            Container cc = rcontainer;


            bool isinstartoffset = true;

            int currentscope = 0;
            bool forstart = false;
            string scope = "st";
            string lastblockscope = "st";
            

            List<Block> forblocks = new List<Block>();



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

                //如果在函数内部则视为函数。

                //如果您不想对一个对象进行两次操作，请充分了解它的结构。

                switch (tk.Type)
                {
                    case TOKEN_TYPE.KeyWord:
                        switch (tk.Value)
                        {
                            case "const":
                            case "var":
                                List<Block> b = BlockAnalyzer(cc, scope, tk, startindex, main: rcontainer);


                                foreach (var item in b)
                                {
                                    item.Scope = scope;


                                    if (cc.CheckIdentifier(scope, item.BlockName))
                                    {
                                        ThrowException("变量 " + item.BlockName + " 已经声明过了。", tk, 1);
                                    }


                                    if (forstart)
                                    {
                                        forblocks.Add(item);
                                    }

                                    //它应该只影响自动完成。
                                    //如果超出范围，则不包含在对象中。
                                    //if (isinstartoffset)
                                    //{
                                    //    cc.vars.Add(item);
                                    //}
                                    cc.vars.Add(item);
                                }
                                break;
                            case "foreach":
                                if (!CheckCurrentToken(TOKEN_TYPE.Symbol, "("))
                                {
                                    ThrowException("foreach 语句不正确。 必须有圆括号。", tk);
                                }
                                int foreachstart = tk.EndOffset;
                                forblocks.Clear();
                                forstart = true;
                                Block forvar = null;
                                while (true)
                                {
                                    tk = GetCurrentToken();

                                    if(tk == null)
                                    {
                                        ThrowException("foreach 变量位置必须声明标识符。", tk);
                                        break;
                                    }
                                    if(tk.Type != TOKEN_TYPE.Identifier)
                                    {
                                        ThrowException("foreach 变量位置必须声明标识符。", tk);

                                        break;
                                    }


                                    forvar = new Block(rcontainer, "const", tk.Value, tk);

                                    forvar.Scope = scope;
                                    forblocks.Add(forvar);

                                    cc.vars.Add(forvar);
                                    if (!CheckCurrentToken(TOKEN_TYPE.Symbol, ","))
                                    {
                                        break;
                                    }
                                }
                                if (foreachstart <= startindex && startindex <= tk.EndOffset)
                                {
                                    cc.cursorLocation = CursorLocation.ForEachDefine;
                                }

                                tk = GetSafeTokenIten();
                                if (tk == null) break;
                                if (tk.Type == TOKEN_TYPE.Symbol && tk.Value == ":")
                                {
                                    if (tk.StartOffset <= startindex && startindex <= tk.EndOffset + 2)
                                    {
                                        cc.cursorLocation = CursorLocation.ForFuncDefine;
                                    }
                                    tk = GetCurrentToken();
                                    tk = GetCurrentToken();

                                    if(forvar != null)
                                    {
                                        forvar.Values = IdentifierFAnalyzer(cc, scope, tk, startindex, main: rcontainer);
                                    }
                                }


                                break;
                            case "for":
                                forblocks.Clear();
                                forstart = true;

                                break;
                            case "import":
                                //import File as t;
                                //import File;
                                int importstart = tk.EndOffset;

                                tk = GetCurrentToken();
                                if(tk == null)
                                {
                                    ThrowException("导入语句没有正确结束。", tk);
                                    break;
                                }
                                if(importstart < startindex && startindex < tk.StartOffset)
                                {
                                    //目前输入空白
                                    rcontainer.cursorLocation = CursorLocation.ImportFile;
                                    break;
                                }


                                List<TOKEN> t = GetTokenListFromTarget(tk, saveIndex:false, IsNamespace: true, addSperator:true);
                                
                                if(t.Count == 0)
                                {
                                    ThrowException("导入语句没有正确结束。", tk);
                                    break;
                                }
                                int importend = 0;
                                if (t.Last().Value == ".")
                                {
                                    importend = 1;
                                    t.RemoveAt(t.Count - 1);
                                }

                                importend += t.Last().EndOffset;

                        


                                string filename = "";
                                //tk = GetCurrentToken();
                                foreach (var item in t)
                                {
                                    if(filename != "")
                                    {
                                        filename += ".";
                                    }
                                    filename += item.Value;
                                }


                                string nspace;

                                if (CheckCurrentToken(TOKEN_TYPE.Symbol, ";"))
                                {
                                    //无特殊指定的导入
                                    cc.importedNameSpaces.Add(new ImportedNameSpace(filename, ""));
                                }
                                else if(CheckCurrentToken(TOKEN_TYPE.KeyWord, "as"))
                                {
                                    //特殊指定符
                                    tk = GetCurrentToken();
                                    if (!CheckCurrentToken(TOKEN_TYPE.Symbol, ";") || tk == null)
                                    {
                                        ThrowException("导入语句没有正确结束。", tk);
                                    }
                                    else
                                    {
                                        nspace = tk.Value;
                                        cc.importedNameSpaces.Add(new ImportedNameSpace(filename, nspace));
                                    }
                                }
                                else
                                {
                                    //无效指定符
                                    ThrowException("导入语句没有正确结束。", tk);
                                }
                                if (importstart <= startindex && startindex <= importend)
                                {
                                    rcontainer.cursorLocation = CursorLocation.ImportFile;
                                }
                                //让导入管理器检查文件名。
                                //您必须提供您的姓名和文件名。
                                break;
                            case "function":
                                Function function = FunctionAnalyzer(rcontainer, startindex, scope);

                                //if(rcontainer.funcs.Find(x=> ((x.funcname == function.funcname) && (x.IsPredefine == false))) != null)
                                //{
                                // ThrowException("函数 " + function.funcname + " 重复声明。", tk, 1);
                                //}

                                if (cc.CheckIdentifier(scope, function.funcname, funcdefine:true))
                                {
                                    ThrowException("函数 " + function.funcname + " 已经声明过了。", tk, 1);
                                }

                                function.scope = scope;


                                //超出范围时不放入对象中。
                                if (isinstartoffset)
                                {
                                    if(function.cursorLocation != CursorLocation.None)
                                    {
                                        cc.cursorLocation = function.cursorLocation;
                                        rcontainer.cursorLocation = function.cursorLocation;
                                    }
                                }
                                cc.funcs.Add(function);
                                //if (!function.IsInCursor)
                                //{
                                //}
                                //添加参数
                                foreach (var item in function.args)
                                {
                                    Block bl = new Block(rcontainer, "var", item.argname, tk, item.argtype, IsArg: true);
                                    bl.Scope = scope + "." + (currentscope + 1).ToString().PadLeft(4, '0');
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
                            case "object":
                                tk = GetCurrentToken();
                                if(tk == null)
                                {
                                    cc.cursorLocation = CursorLocation.ObjectDefine;
                                    continue;
                                }
                                tk.scope = scope;
                                if (tk.StartOffset <= startindex && startindex <= tk.EndOffset)
                                {
                                    cc.cursorLocation = CursorLocation.ObjectDefine;
                                }
                                if (tk.Type != TOKEN_TYPE.Identifier)
                                {
                                    ThrowException("Object的名称必须是标识符。", tk);
                                    continue;
                                }
                                

                                string objname = tk.Value;


                                if (cc.CheckIdentifier(scope, objname))
                                {
                                    ThrowException("Object " + objname + " 已经声明过了。", tk, 1);
                                    continue;
                                }

                                if (!CheckCurrentToken(TOKEN_TYPE.Symbol, "{"))
                                {
                                    ThrowException("Object的定义必须以'{'开始。", tk);
                                    continue;
                                }
                                else
                                {
                                    scope += "." + "O" + objname;
                                }

                                obj = new Container(codeAnalyzer);
                                obj.mainname = objname;
                                obj.IsObject = true;
                                obj.cursorLocation = cc.cursorLocation;

                                cc = obj;
                                //object linkedList{
                                //    var front : linkedUnit;
                                //    var rear : linkedUnit;
                                //    var size;
                                //    function append(un : linkedUnit){
                                //        this.size++;
                                //        if(!this.front){
                                //            this.front=un;
                                //            this.rear=un;
                                //        }
                                //        else{
                                //            this.rear.next=un;
                                //            this.rear=un;
                                //        }
                                //    }
                                //};

                                break;
                        }

                        break;
                    case TOKEN_TYPE.Symbol:
                        // 符号 {} ;
                        // 定义范围，
                        switch (tk.Value)
                        {
                            case "{":
                                //定义新范围
                                currentscope++;

                                scope += "." + (currentscope).ToString().PadLeft(4, '0');


                                if (forstart)
                                {
                                    foreach (var item in forblocks)
                                    {
                                        item.Scope = scope;
                                    }

                                    forstart = false;
                                    //枪声结束
                                }
                                break;
                            case "}":
                                //恢复到之前的范围
                                int t = currentscope.ToString().Length + 1;

                                if (obj != null)
                                {
                                    if (scope == "st.O" + obj.mainname)
                                    {
                                        if (!CheckCurrentToken(TOKEN_TYPE.Symbol, ";"))
                                        {
                                            ThrowException("Object的定义必须以';'结尾。", tk);
                                        }
                                        cc = rcontainer;

                                        cc.objs.Add(obj);
                                        obj = null;
                                    }
                                }
                                if (scope == "st")
                                {
                                    //我无法关闭它，但它会关闭
                                    ThrowException("'{}'没有正确关闭。", tk);
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
                        //如果是关键字，检查是否是命名空间。

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
                ThrowException("'{}'没有正确关闭。", tk);
            }


            return rcontainer;
        }



        public List<Block> BlockAnalyzer(Container container, string scope, TOKEN ctk, int startindex, Container main = null)
        {
            List<Block> blocks = new List<Block>();

            int tindex = tokenindex;

            //var vname = 值；
            //var front: linkedUnit;
            //const vname = 值；
            string type = ctk.Value;
            string varconst = type;

            TOKEN tk;

            List<TOKEN> varvalue = null;
            while (true)
            {
                tk = GetCurrentToken();
                if (tk == null) return blocks;
                if (tk.Type != TOKEN_TYPE.Identifier)
                {
                    ThrowException("变量声明必须是标识符。", tk);
                }

                string varname = tk.Value;
                string vartype = "";
                tk.scope = scope;

                if (CheckCurrentToken(TOKEN_TYPE.Symbol, ":"))
                {
                    //当用类型声明时
                    tk = GetCurrentToken();
                    vartype = tk.Value;
                }

                blocks.Add(new Block(container, varconst, varname, tk, vartype, varvalue));
                if (CheckCurrentToken(TOKEN_TYPE.Symbol, ","))
                {
                    //如果有多个声明
                }
                else
                {
                    //如果不
                    break;
                }
            }
      




            if (CheckCurrentToken(TOKEN_TYPE.Symbol, "="))
            {
                int index = 0;
                int equalstarttokenindex = tokenindex;
                while (true)
                {
                    int starttokenindex = tokenindex;
                    tk = GetCurrentToken();
                    varvalue = IdentifierFAnalyzer(container, scope, tk, startindex, main: main);

                    int endtokenindex = tokenindex;


                    if (blocks.Count <= index)
                    {
                        if(blocks.Count == 1)
                        {
                            //元组类型
                            blocks[0].RawText = GetTextFromTokenToEndLine(equalstarttokenindex, ";");
                            break;
                        }
                        else
                        {
                            ThrowException("赋值表达式的数量不匹配。", tk);
                            break;
                        }
                    }
                    else
                    {
                        if(varvalue.Count >= 1 && varvalue[0].Value == "EUDArray")
                        {
                            blocks[index].RawText = GetTextFromTokenToEndLine(equalstarttokenindex, ";");
                        }
                        else
                        {
                            blocks[index].RawText = GetTextFromToken(starttokenindex, endtokenindex);
                        }

                        blocks[index++].Values = varvalue;
                    }

                    if(!CheckCurrentToken(TOKEN_TYPE.Symbol, ","))
                    {
                        break;
                    }
                }
               

            }
            else
            {
                if (varconst == "const")
                {
                    //对于 const，如果只有声明，则会输出错误。
                    ThrowException("const 必须在声明后赋值。", tk);
                }
            }


            //new Block(varconst, varname, vartype, varvalue);
            //const t = func();
            //const t = func1() + func2();
            //const t = (func1() + func2());

            //如果声明
            //while (!CheckCurrentToken(TOKEN_TYPE.Symbol, ";"))
            //{
            // //不在句子末尾时继续
            //    tk = GetCurrentToken();

            //}
            //句子结尾

            return blocks;
        }

        public List<TOKEN> IdentifierFAnalyzer(Container container, string scope, TOKEN ctk, int startindex, int argindex = -1, Container main = null)
        {
            List<TOKEN> tlist = new List<TOKEN>();

            if (ctk == null) return tlist;

            string fname = ctk.Value;
            ctk.scope = scope;

            int argstartindex = ctk.StartOffset;


            if (ctk.Type != TOKEN_TYPE.Identifier)
            {
                if (ctk.Type == TOKEN_TYPE.Symbol && ctk.Value == "[")
                {
                    tlist.Add(new TOKEN(0, TOKEN_TYPE.Identifier, "EUDArray", 0));
                }
                else if (ctk.Type == TOKEN_TYPE.Number)
                {
                    tlist.Add(new TOKEN(0, TOKEN_TYPE.Number, ctk.Value, 0));
                }
                return tlist;
            }
       


            tlist.Add(ctk);
            if (argindex != -1)
            {
                //如果继承
                ctk.argindex = argindex;
            }

            if(specialKeyword.IndexOf(fname) == -1)
            {
                if (fname != "this" && !container.CheckIdentifier(scope, fname))
                {
                    if (!(fname.Length != 0 && fname[0] == '@'))
                    {
                        ThrowException(fname + " 未声明。", ctk);
                    }
                }
            }

            TOKEN tk = null;//GetCurrentToken();

            if (CheckCurrentToken(TOKEN_TYPE.Symbol, "."))
            {
                //.，所以后面的标记
                tlist.AddRange(GetTokenList());
            }

            CheckFunc:
            //声明函数的开始

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
                            ThrowException(fname + " 括号未正确闭合。", tk);
                        }
                        break;
                    }
                    tk = GetCurrentToken();
                    tk.scope = scope;
                    tk.argindex = cargindex;
                    tk.funcname = tlist;
                    //tlist.Add(tk);
                    switch (tk.Type)
                    {
                        case TOKEN_TYPE.Identifier:
                            //tlist.AddRange(IdentifierFAnalyzer(container, scope, tk, startindex, cargindex, main: main));
                            IdentifierFAnalyzer(container, scope, tk, startindex, cargindex, main: main);
                            break;
                        case TOKEN_TYPE.Symbol:
                            //， （ ） 等可能存在。
                            if(tk.Value == ")")
                            {
                                innercount--;
                            }
                            else if (tk.Value == ",")
                            {
                                if (!main.innerFuncInfor.IsInnerFuncinfor &&
                                    argstartindex <= startindex &&  startindex <= tk.StartOffset)
                                {
                                    //当内部函数尚未确定时
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
                            //当内部函数尚未确定时
                            main.innerFuncInfor.IsInnerFuncinfor = true;
                            main.innerFuncInfor.argindex = cargindex;
                            main.innerFuncInfor.funcename = tlist;
                        }
                        argstartindex = tk.StartOffset;

                        //if(!CheckCurrentToken(TOKEN_TYPE.Symbol, ";"))
                        //{
                        // ThrowException("; 是必需的。", tk);
                        //}

                        break;
                    }
                }
            }

            if (CheckCurrentToken(TOKEN_TYPE.Symbol, ".", IsDirect:true))
            {
                //由于在函数之后继续，所以回到循环路径。
                tlist.AddRange(GetTokenList());
                goto CheckFunc;
            }
            //除此之外，你就out了。

            return tlist;
            //单一词元(Token)
        }




        public Function FunctionAnalyzer(Container container, int startindex, string scope)
        {
            Function function = new EpScriptFunction(container, null);

            TOKEN commenttoken = GetCommentTokenIten(-2);

            if(commenttoken != null)
            {

                if (commenttoken.Type == TOKEN_TYPE.KeyWord && commenttoken.Value == "static")
                {
                    function.IsStatic = true;
                }else if (commenttoken.Type == TOKEN_TYPE.Comment)
                {
                    string[] lines = commenttoken.Value.Replace("\r", "").Split('\n');

                    string tabstr = "";
                    string result = "";
                    int index = 0;
                    foreach (var item in lines)
                    {
                        string ritem = "";
                        ritem = item;
                        if (item.IndexOf("/***") != -1 && index == 0)
                        {
                            //找到了开始部分
                            index = 1;
                        }

                        int s = item.IndexOf(" * ");
                        if (s >= 0 && index > 0)
                        {
                            if (s != 0)
                            {
                                //下一部分
                                if (tabstr == "")
                                {
                                    tabstr = item.Substring(0, s);
                                }
                                if (tabstr == item.Substring(0, tabstr.Length))
                                {
                                    //标签页部分必须相同
                                    ritem = item.Substring(tabstr.Length);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            index = +1;
                        }

                        if (item.IndexOf("***/") != -1 && index > 0)
                        {
                            //尾矿
                            if (tabstr == item.Substring(0, tabstr.Length))
                            {
                                //标签页部分必须相同
                                ritem = item.Substring(tabstr.Length);
                            }
                            index = +1;
                        }

                        result += ritem + "\n";
                    }



                    function.comment = result;
                    function.ReadComment("ko-KR");
                    //function.ReadComment("zh-CN");
                }
            }

            TOKEN tk = GetCurrentToken();
            function.StartToken = tk;
            if (tk == null) return function;
            tk.scope = scope;
            CursorLocation cl = CursorLocation.None;
            int argstartoffset = tk.EndOffset;
            int argendoffset = 0;
            string funcname = "";
            int findex = CurrentInedx;

            if (tk.Type == TOKEN_TYPE.Symbol && tk.Value == "$") //特别任务函数
            {
                funcname += "$";
                tk = GetCurrentToken();
            }

            if (tk.Type != TOKEN_TYPE.Identifier)
            {
            

                ThrowException("函数的名称必须是标识符。", tk);
                goto EndLabel;
            }

            funcname += tk.Value;
            function.funcname = funcname;



            if (!CheckCurrentToken(TOKEN_TYPE.Symbol, "("))
            {
                ThrowException("函数名称后面必须跟有参数声明。", tk);
                goto EndLabel;
            }





            while (true)
            {
                Function.Arg arg = new Function.Arg();


                if (CheckCurrentToken(TOKEN_TYPE.Symbol, "*"))
                {
                    //参数类型
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
                            ThrowException("参数声明不正确。 必须有右括号", tk);
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
                    ThrowException("参数声明不正确。必须遵循参数名称。", tk);
                    goto EndLabel;
                }

                recheck:

                tk = GetCommentTokenIten();
                if(tk.Type != TOKEN_TYPE.Symbol && tk.Type != TOKEN_TYPE.Comment)
                {
                    //警告必须是无条件的。
                    argendoffset = tk.EndOffset;
                    ThrowException("参数声明不正确。 必须遵循 '),'。", tk);
                    goto EndLabel;
                }
                else if (tk.Type == TOKEN_TYPE.Comment)
                {
                    //特殊处理型
                    arg.argtype = tk.Value.Replace("/", "").Replace("*", "");
                    tk = GetCurrentToken();
                }
                else
                {
                    tk = GetCurrentToken();
                }


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
                else if (tk.Value == ":")
                {
                    tk = GetCommentTokenIten();
                    int typestartindex = tk.StartOffset;
                    int typeendindex = tk.EndOffset;

                    if (tk.Type == TOKEN_TYPE.Identifier)
                    {
                        //普通型
                        arg.argtype = tk.Value;
                    }
                    else
                    {
                        if (typestartindex <= startindex && startindex <= typeendindex)
                        {
                            cl = CursorLocation.FunctionArgType;
                        }
                        argendoffset = tk.EndOffset;
                        ThrowException("必须声明参数类型。", tk);
                        goto EndLabel;
                    }

                    if (typestartindex <= startindex && startindex <= typeendindex)
                    {
                        cl = CursorLocation.FunctionArgType;
                    }

                    tk = GetCurrentToken();

                    TOKEN nt = GetSafeTokenIten();
                    if(nt.Type == TOKEN_TYPE.Symbol && nt.Value == "(")
                    {
                        //如果类型是函数
                        int bracecount = 1;

                        GetCurrentToken();
                        while (true)
                        {
                            nt = GetCurrentToken();

                            if (IsEndOfList())
                            {
                                ThrowException("括号未闭合。", nt);
                                goto EndLabel;
                            }

                            if (nt.Type == TOKEN_TYPE.Symbol && nt.Value == "(")
                            {
                                bracecount ++;
                            }
                            else if (nt.Type == TOKEN_TYPE.Symbol && nt.Value == ")")
                            {
                                bracecount--;
                            }
                            if(bracecount == 0)
                            {
                                break;
                            }
                        }


                    }


                    goto recheck;
                }





                function.args.Add(arg);
            }

            findex = CurrentInedx;

            if (!IsEndOfList())
            {
                int brackcount = 0;
                int funcstartoffset = 0;
                int funcendoffset = 0;


                tk = GetCurrentToken();
                tk.scope = scope;


                if (tk.Type == TOKEN_TYPE.Symbol && tk.Value == ":")
                {
                    if (IsEndOfList())
                    {
                        ThrowException("返回类型必须指定。", tk);
                        goto EndLabel;
                    }
                    List<TOKEN> tlist = GetTokenList();

                    function.returntype = tlist;

                    if (IsEndOfList())
                    {
                        ThrowException("返回类型必须指定。", tk);
                        goto EndLabel;
                    }
                    findex = CurrentInedx;
                    tk = GetCurrentToken();
                }


                if (tk.Type == TOKEN_TYPE.Symbol && tk.Value == ";")
                {
                    //就这样结束吧
                    function.IsPredefine = true;
                    goto EndLabel;
                }
                else if (tk.Type == TOKEN_TYPE.Symbol && tk.Value == "{")
                {
                    brackcount += 1;
                    funcstartoffset = tk.StartOffset;
                }
                else
                {
                    ThrowException("函数的声明是错误的。", tk);
                    goto EndLabel;
                }
                if (IsEndOfList())
                {
                    ThrowException("函数的声明是错误的。", tk);
                    goto EndLabel;
                }
                tk = GetCurrentToken();

                if (tk == null)
                {
                    ThrowException("函数的声明是错误的。", tk);
                    goto EndLabel;
                }
                tk.scope = scope;
                while (true && tk != null)
                {
                    if (tk.Type == TOKEN_TYPE.Symbol && tk.Value == "{")
                    {
                        brackcount += 1;
                    }
                    else if (tk.Type == TOKEN_TYPE.Symbol && tk.Value == "}")
                    {
                        brackcount -= 1;
                    }

                    if(brackcount == 0)
                    {
                        funcendoffset = tk.StartOffset;

                        if (funcstartoffset <= startindex && startindex <= funcendoffset)
                        {
                            function.IsInCursor = true;
                        }
                        break;
                    }

                    if (IsEndOfList())
                    {
                        ThrowException("括号未正确闭合。", tk);
                        goto EndLabel;
                    }
                    tk = GetCurrentToken();
                }
            }

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