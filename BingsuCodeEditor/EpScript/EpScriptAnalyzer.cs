using BingsuCodeEditor.AutoCompleteToken;
using BingsuCodeEditor.Lua;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;

namespace BingsuCodeEditor.EpScript
{
    partial class EpScriptAnalyzer : CodeAnalyzer
    {
        public static ImportManager importManager;
        public static string DEFAULTFUNCFILENAME = "DEFAULTFUNCTIONLIST";
        public static Container _DefaultFuncContainer;
        public static Container DefaultFuncContainer
        {
            get
            {
                return _DefaultFuncContainer;
            }
        }

        public override ImportManager StaticImportManager
        {
            get
            {
                return EpScriptAnalyzer.importManager;
            }
        }


        public override Container GetDefaultContainer
        {
            get
            {
                return _DefaultFuncContainer;
            }
        }

        public override void SetImportManager(ImportManager importManager)
        {
            if(importManager.CodeType == CodeTextEditor.CodeType.epScript)
            {
                EpScriptAnalyzer.importManager = importManager;

                if (!importManager.IsCachedContainer(DEFAULTFUNCFILENAME))
                {
                    Container c = this.GetContainer(importManager.GetFIleContent(DEFAULTFUNCFILENAME));
                    importManager.UpdateContainer(DEFAULTFUNCFILENAME, c);
                }
                if (_DefaultFuncContainer == null)
                {
                    _DefaultFuncContainer = importManager.GetContainer(DEFAULTFUNCFILENAME);

                    foreach (var item in _DefaultFuncContainer.funcs)
                    {
                        if (!string.IsNullOrEmpty(item.comment)) item.ReadComment("ko-KR");
                        //if (!string.IsNullOrEmpty(item.comment)) item.ReadComment("zh-CN");
                    }
                }
            }
            else if (importManager.CodeType == CodeTextEditor.CodeType.Lua)
            {
                luaAnalyzer.SetImportManager(importManager);
            }
        }


        public EpScriptAnalyzer(TextEditor textEditor) : base(textEditor, false)
        {
            string[] keywords = {"object", "static", "once", "if", "else", "while", "for", "function", "foreach",
        "return", "switch", "case", "break", "var", "const", "import", "as", "continue" , "true", "True", "false", "False"};


            //[tab]for(var [i] = 0; [i] < [Length] ; [i]++)\n[tab]{\n[tab][tabonce][Content]\n[tab]}
            Template.Add("if", " ([true]) {\n[tab][tabonce][Content]\n[tab]}");
            Template.Add("while", " ([true]) {\n[tab][tabonce][Content]\n[tab]}");
            Template.Add("switch", " ([Var]) {\n[tab][tabonce][Content]\n[tab]}");
            Template.Add("for", " (var [i] = [0]; [i] < [Length]; [i]++) {\n[tab][tabonce][Content]\n[tab]}");
            Template.Add("foreach", " ([Var] : [Func]) {\n[tab][tabonce][Content]\n[tab]}");
            Template.Add("function", " [FuncName]([Arg]) {\n[tab][tabonce][Content]\n[tab]}");
            Template.Add("object", " [objname]{\n[tab][tabonce][Content]\n[tab]};");
            // // Template.Add("/***", "\n[tab] * @Type\n[tab] * F\n[tab] * @Summary.ko-KR\n[tab] * [Summary]\n[tab] * @param.args.ko-KR\n[tab]***/[Content]");
            //Template.Add("/***", "\n[tab] * @Type\n[tab] * F\n[tab] * @Summary.zh-CN\n[tab] * [Summary]\n[tab] * @param.args.zh-CN\n[tab]***/[Content]");




            foreach (var item in keywords)
            {
                //输入Token
                AddSubType(item, TOKEN_TYPE.KeyWord);

                //自动完成初始输入
                if (Template.ContainsKey(item))
                {
                    completionDatas.Add(new KewWordItem(CompletionWordType.KeyWord, item, item + "\n注意：要插入代码片段，请按 Tab 键两次。"));
                }
                else
                {
                    completionDatas.Add(new KewWordItem(CompletionWordType.KeyWord, item));
                }
            }


            FuncPreChar.Add("$");
            FuncPreChar.Add("@");


            tokenAnalyzer = new EpScriptTokenAnalyzer(this);
            secondtokenAnalyzer = new EpScriptTokenAnalyzer(this);
            codeFoldingManager = new EpScriptFoldingManager(textEditor);


            luaAnalyzer = new LuaAnalyzer(textEditor);
            luaTokenAnalyzer = new LuaTokenAnalyzer(luaAnalyzer);
        }

        public LuaAnalyzer luaAnalyzer;
        public LuaTokenAnalyzer luaTokenAnalyzer;


        public override bool AutoInsert(string text)
        {
            if(textEditor.SelectionLength == 0)
            {
                if (text == "Oem1")
                {
                    string linetext = LineDirectString(true);

                    if (linetext.IndexOf("for") == -1)
                    {
                        if(GetDirectText(0) == ")")
                        {
                            textEditor.CaretOffset += 1;
                            DirectInsetTextFromCaret(";", 0, true);
                            return true;
                        }

                    }

                }
                //if(text == "*")
                //{
                //    TOKEN tk = GetToken(0);

                //    if(tk.Value == "/")
                //    {
                //        textEditor.SelectedText = "*/";
                //        textEditor.SelectionLength = 0;
                //    }
                //}
            }
            else
            {
                
                //用“”等包裹起来。
                //int len = textEditor.SelectionLength;

                //switch (text)
                //{
                //    case "\"":
                //        textEditor.SelectionLength = 0;
                //        textEditor.SelectedText = "\"";
                //        textEditor.SelectionStart += len + 1;
                //        textEditor.SelectedText = "\"";
                //        textEditor.SelectionStart -= len + 2;
                //        textEditor.SelectionLength = len + 2;
                //        break;
                //}


            }
            return false;
        }

        public override bool AutoRemove()
        {
            return false;
        }



        public override TOKEN TokenBlockAnalyzer(string text, int index, out int outindex, int caretoffset)
        {
            int sindex = index;
            char t = text[index];
            int tlen = text.Length;
            string block = t.ToString();


            outindex = -1;
            if (t == '"')
            {
                block = "";
                //LineCommnet 重复直到换行(\r)

                bool IsSpec = false;
                do
                {
                    block += t.ToString();
                    index++;
                    if (index >= tlen)
                    {
                        break;
                    }
                    t = text[index];


                    if (t == '"' && !IsSpec)
                    {
                        break;
                    }

                    if (t == '\\')
                    {
                        IsSpec = true;
                    }
                    else
                    {
                        IsSpec = false;
                    }


                } while (index < tlen);
                block += '"';

                TOKEN_TYPE type = TOKEN_TYPE.String;

                TOKEN token = new TOKEN(sindex, type, block, caretoffset);
                outindex = index;
                return token;
            }
            else if (t == '/')
            {
                if (index + 1 >= tlen)
                {
                    return null;
                }
                char nt = text[index + 1];

                if (nt == '/')
                {
                    //LineCommnet 重复直到换行(\r)
                    index++;
                    if (index >= tlen)
                    {
                        return null;
                    }
                    t = text[index];
                    do
                    {
                        block += t.ToString();
                        index++;
                        if (index >= tlen)
                        {
                            break;
                        }
                        t = text[index];
                    } while (index < tlen && (t != '\n'));
                    index--;


                    TOKEN_TYPE type = TOKEN_TYPE.LineComment;

                    block = block.Replace("\r", "");

                    TOKEN token = new TOKEN(sindex, type, block, caretoffset);
                    outindex = index;
                    return token;
                }
                else if (nt == '*')
                {
                    // MulitComment */重复直到出现
                    char lastchar = ' ';
                    t = text[++index];
                    do
                    {
                        block += t.ToString();
                        index++;
                        if (index >= tlen)
                        {
                            //其实是一个错误...
                            break;
                        }
                        if(lastchar == '*' && t == '/')
                        {
                            //注释
                            break;
                        }


                        lastchar = t;
                        t = text[index];
                    } while (index < tlen);
                    index--;


                    TOKEN_TYPE type = TOKEN_TYPE.Comment;

                    //block = block.Replace("\r", "");

                    TOKEN token = new TOKEN(sindex, type, block, caretoffset);
                    outindex = index;
                    return token;
                }
            }
            else if (t == '<')
            {
                if (index + 1 >= tlen)
                {
                    return null;
                }
                char nt = text[index + 1];

                if (nt == '?')
                {
                    //重复直到出现 MulitComment */
                    char lastchar = ' ';
                    index++;
                    if (index>= tlen)
                    {
                        return null;
                    }
                    t = text[index];
                    do
                    {
                        block += t.ToString();
                        index++;
                        if (index >= tlen)
                        {
                            //其实是一个错误...
                            break;
                        }
                        if (lastchar == '?' && t == '>')
                        {
                            //注释
                            break;
                        }


                        lastchar = t;
                        t = text[index];
                    } while (index < tlen);
                    index--;


                    TOKEN_TYPE type = TOKEN_TYPE.Special;

                    //block = block.Replace("\r", "");

                    TOKEN token = new TOKEN(sindex, type, block, caretoffset);
                    outindex = index;
                    return token;
                }
            }

            return null;
        }


       


        public override object GetObjectFromName(List<string> objectname, Container startcontainer, FindType findType, IList<ICompletionData> data = null, string scope = "st")
        {
            //imported1.var1;
            //imported1.const1.object1;
            //const1.object1;
            //maincontainer.vars[0].

            //寻找参考文献，如果找不到，请忽略。
            //if (!startcontainer.CheckIdentifier(scope, objectname[0]))
            //{
            //    return null;
            //}
            if (objectname.Count == 0) return null;


            Container ccon = startcontainer;
            Container objcon = null;
            string folderpath = folder;
            int index = 0;
            string lscope = scope;
            while (true)
            {
                string objname = objectname[index];

                if (objname == null) objname = "";
                //lua输出处理器函数

                if (objname.Length != 0 && objname[0] == '@')
                {
                    //如果是 lua函数
                    if(findType == FindType.Func)
                    {
                        string realfunname = objname.Replace("@", "");
                        return luaAnalyzer.GetDefaultContainer.funcs.Find(x => (x.funcname == realfunname && lscope.Contains(x.scope)));
                    }
                }




                if (scope.IndexOf("st.O") != -1)
                {
                    List<string> tname = new List<string>();
                    tname.Add(scope.Split('.')[1].Substring(1));
                    object _obj = GetObjectFromName(tname, ccon, FindType.Obj);

                    if (_obj != null)
                    {
                        objcon = (Container)_obj;
                    }

                    if (objname == "this")
                    {
                        //当出现这个时，un 是不允许的。

                        lscope = objcon.GetInitObjectNameSpacee();
                        index++;
                        if (index == objectname.Count)
                        {
                            if (findType == FindType.AutoComplete)
                            {
                                //由于这是最后一部分，因此请输入容器的所有内容。
                                if (objcon != null) objcon.GetAllItems(data, objcon.GetInitObjectNameSpacee(), noargFlag:true);
                            }
                            break;
                        }
                        continue;
                    }
                }





                Block var = null;
                Container obj = null;
                Function func = null;
                ImportedNameSpace importedNameSpace = null;



                if (objcon != null)
                {
                    var = objcon.vars.Find(x => (x.BlockName == objname && lscope.Contains(x.Scope)));
                    obj = objcon.objs.Find(x => (x.mainname == objname));
                    func = objcon.funcs.Find(x => (x.funcname == objname && lscope.Contains(x.scope)));
                    importedNameSpace = ccon.importedNameSpaces.Find(x => (x.shortname == objname));
                }
                else
                {
                    var = ccon.vars.Find(x => (x.BlockName == objname && lscope.Contains(x.Scope)));
                    obj = ccon.objs.Find(x => (x.mainname == objname));
                    func = ccon.funcs.Find(x => (x.funcname == objname && lscope.Contains(x.scope)));
                    importedNameSpace = ccon.importedNameSpaces.Find(x => (x.shortname == objname));
                }

                if(EpScriptAnalyzer.DefaultFuncContainer != null)
                {
                    if (var == null)
                    {
                        var = EpScriptAnalyzer.DefaultFuncContainer.vars.Find(x => (x.BlockName == objname && lscope.Contains(x.Scope)));
                    }
                    if (obj == null)
                    {
                        obj = EpScriptAnalyzer.DefaultFuncContainer.objs.Find(x => (x.mainname == objname));
                    }
                    if (func == null)
                    {
                        func = EpScriptAnalyzer.DefaultFuncContainer.funcs.Find(x => (x.funcname == objname && lscope.Contains(x.scope)));
                    }
                }
              
                //if (objname == "this" && objectname.Count == 1)
                //{
                // //如果是自引用
                //    if (findType == FindType.AutoComplete)
                //    {
                // //这是最后一部分，所以输入容器的所有内容。
                //        ccon.GetAllItems(data, "st.O" + startcontainer.mainname);
                //    }
                //}


                if (importedNameSpace != null)
                {
                    string nsfile = importedNameSpace.mainname;
                    string pullpath = importManager.GetPullPath(nsfile, folderpath);

                    if (importManager.IsFileExist(pullpath))
                    {
                        if (!importManager.IsCachedContainer(pullpath))
                        {
                            //如果文件已被修改
                            importManager.UpdateContainer(pullpath, GetContainer(importManager.GetFIleContent(pullpath)));
                        }

                        ccon = importManager.GetContainer(pullpath);

                        if (pullpath.IndexOf(".") != -1)
                        {
                            folderpath = pullpath.Substring(0, pullpath.LastIndexOf("."));
                        }
                        else
                        {
                            folderpath = "";
                        }

                        lscope = "st";//初始化范围
                        index++;
                        if (index == objectname.Count)
                        {
                            if (findType == FindType.AutoComplete)
                            {
                                //由于这是最后一部分，因此请输入容器的所有内容。
                                ccon.GetAllItems(data, lscope);
                            }else if (findType == FindType.All)
                            {
                                return importedNameSpace;
                            }

                            break; 
                        }
                    }
                    else
                    {
                        return null;
                    }

                }
                else if (func != null)
                {
                    if (findType == FindType.Func || findType == FindType.All)
                    {
                        //如果最后有差异
                        if (index + 1 == objectname.Count)
                        {
                            //如果您正在寻找解决方案，只需返回此函数即可。哈哈。
                            return func;
                        }

                        break;
                    }
                    else if (findType == FindType.AutoComplete)
                    {
                        //如果是自动完成，则注入相应数字的返回类型函数。
                        string rtype = "";

                        if(func.returntype != null)
                        {
                            foreach (var item in func.returntype)
                            {
                                if (rtype != "") rtype += ",";
                                rtype += item.Value;
                            }
                        }
           

                        if (rtype == "") return null;

                        List<string> tname = new List<string>();
                        tname.AddRange(rtype.Split('.'));
                        object _obj = GetObjectFromName(tname, ccon, FindType.Obj);

                        if (_obj != null)
                        {
                            objcon = (Container)_obj;
                        }
                        else
                        {
                            return null;
                        }


                        if (index + 1 == objectname.Count)
                        {
                            //如果是最后一个命令，则返回对象的元素。
                            objcon.GetAllItems(data, objcon.GetInitObjectNameSpacee(), noargFlag: true);
                            break;
                        }

                    }


                        return null;
                }
                else if (obj != null)
                {
                    if (findType == FindType.Obj)
                    {
                        if (index + 2 == objectname.Count)
                        {
                            switch(objectname[index + 1])
                            {
                                case "cast":
                                case "alloc":
                                    return obj;
                            }
                        }
                    }
                        



                    if (index + 1 == objectname.Count)
                    {
                        if (findType == FindType.AutoComplete)
                        {
                            //由于这是最后一部分，因此请输入容器的所有内容。
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "cast")));
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "alloc")));
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "free")));
                            foreach (var t in obj.funcs.FindAll(x => (x.IsStatic && obj.GetInitObjectNameSpacee().Contains(x.scope)))){

                                data.Add(new CodeCompletionData(t.preCompletion));
                            }
                        }
                        else if (findType == FindType.Obj || findType == FindType.All)
                        {
                            return obj;
                        }
                        else if (findType == FindType.Func)
                        {
                            //如果符合预期，则调用构造函数
                            return obj.funcs.Find(x => (x.funcname == "constructor" && obj.GetInitObjectNameSpacee().Contains(x.scope)));
                        }


                        break;
                    }
                    else
                    {
                        if (index + 1 < objectname.Count)
                        {
                            string last = objectname[index + 1];

                            if (last == "alloc" || last == "cast")
                            {
                                if (index + 2 == objectname.Count)
                                {
                                    //如果最后一个是 alloc 或cast
                                    if (findType == FindType.AutoComplete)
                                    {
                                        obj.GetAllItems(data, obj.GetInitObjectNameSpacee(), noargFlag: true);
                                        break;
                                    }else if (findType == FindType.Func)
                                    {
                                        if(last == "alloc")
                                        {
                                            //获取构造函数
                                            func = obj.funcs.Find(x => (x.funcname == "constructor" && obj.GetInitObjectNameSpacee().Contains(x.scope)));
                                            return func;
                                        }
                                    }
                                    return null;
                                }
                                else
                                {
                                    //引用一个对象。
                                    lscope = obj.GetInitObjectNameSpacee();
                                    objcon = obj;
                                    index += 2;
                                }
                            }
                            else
                            {
                                //它可能是静态的函数
                                return obj.funcs.Find(x => (x.funcname == last && obj.GetInitObjectNameSpacee().Contains(x.scope)));
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else if (var != null)
                {
                    //参考变量的定义检查类型。
                    //如果是 obj，则 obj 的成员 ex var t = a.b.c();如果是这种情况，您需要搜索 a.b.c变量。
                    //var.values
                    //就 obj 而言，由于 obj 是一个容器...您可以在查找数字的过程中更进一步函数。
                    object _obj;

        
                    if (!string.IsNullOrEmpty(var.BlockType))
                    {
                        List<string> list = new List<string>();

                        list.Add(var.BlockType);


                        //如果指定类型
                        if (ccon.IsObject)
                        {
                            //如果是物体，就得到外面去寻找。

                        }
                        _obj = GetObjectFromName(list, ccon, FindType.Obj);

                        if(_obj == null && EpScriptAnalyzer.DefaultFuncContainer != null)
                        {
                            _obj = GetObjectFromName(list, EpScriptAnalyzer.DefaultFuncContainer, FindType.Obj);
                        }


                        if (objcon != null && var.BlockType == "selftype")
                        {
                            _obj = objcon;
                        }
                    }
                    else
                    {
                        //其他的
                        _obj = GetObjectFromName(var.Values, ccon, FindType.Obj);
                        if (_obj == null && EpScriptAnalyzer.DefaultFuncContainer != null)
                        {
                            _obj = GetObjectFromName(var.Values, EpScriptAnalyzer.DefaultFuncContainer, FindType.Obj);
                        }
                        if (_obj == null)
                        {
                            //函数可能存在
                            _obj = GetObjectFromName(var.Values, ccon, FindType.Func);
                            if (_obj == null && EpScriptAnalyzer.DefaultFuncContainer != null)
                            {
                                _obj = GetObjectFromName(var.Values, EpScriptAnalyzer.DefaultFuncContainer, FindType.Func);
                            }
                            if (_obj != null)
                            {
                                Function funcobject = (Function)_obj;
                                List<string> list = new List<string>();

                                if (funcobject.returntype != null)
                                {
                                    foreach (var item in funcobject.returntype)
                                    {
                                        list.Add(item.Value);
                                    }
                                }
                             
                                //读取返回值
                                _obj = GetObjectFromName(list, ccon, FindType.Obj);
                            }
                        }
                        //如果返回函数，则检查函数的返回值以找到容器。
                        if (_obj != null)
                        {
                            if (typeof(Function).IsInstanceOfType(_obj))
                            {
                                Function funcobject = (Function)_obj;
                                List<string> list = new List<string>();

                                if (funcobject.returntype != null)
                                {
                                    foreach (var item in funcobject.returntype)
                                    {
                                        list.Add(item.Value);
                                    }
                                }

                                //读取返回值
                                _obj = GetObjectFromName(list, ccon, FindType.Obj);
                            }
                        }
                    }


                    if (_obj != null)
                    {
                        Container varobject = (Container)_obj;
                        ccon = varobject;
                        lscope = "st.O" + varobject.mainname;//初始化范围
                        index++;
                        if (index == objectname.Count)
                        {
                            if (findType == FindType.AutoComplete)
                            {
                                //由于这是最后一部分，因此请输入容器的所有内容。
                                ccon.GetAllItems(data, ccon.GetInitObjectNameSpacee());
                            }
                            else if (findType == FindType.All)
                            {
                                return _obj;
                            }


                            break;
                        }
                    }
                    else
                    {
                        //或者使用下面的一个
                        if (index + 1 == objectname.Count)
                        {
                            if (findType == FindType.AutoComplete)
                            {
                                //由于这是最后一部分，因此请输入容器的所有内容。
                                data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "getValueAddr")));
                            }
                            else if (findType == FindType.All)
                            {
                                return var;
                            }

                            break;
                        }
                        else
                        {
                            return null;
                        }
                    }

                }
                else
                {
                    return null;
                }
            }
            return null;
        }




        /// <summary>
        /// 通过名称获取文件
        /// </summary>
        public override object GetObjectFromName(List<TOKEN> tokenlist, Container startcontainer, FindType findType, IList<ICompletionData> data = null,  string scope = "st")
        {
            if (tokenlist == null) return null;

            List<string> strs = new List<string>();

            foreach (var item in tokenlist)
            {
                strs.Add(item.Value);
            }

            return GetObjectFromName(strs, startcontainer, findType, data, scope);
        }


        public override void Apply(string text, int caretoffset)
        {
            base.Apply(text, caretoffset);

            TOKEN _t = GetToken(0);
            if (_t != null && _t.Type == TOKEN_TYPE.Special)
            {
                //lua函数
                luaAnalyzer.Apply(_t.Value, caretoffset - _t.StartOffset);

                if (luaAnalyzer.maincontainer.innerFuncInfor.IsInnerFuncinfor)
                {
                    List<TOKEN> tlist = luaAnalyzer.maincontainer.innerFuncInfor.funcename;
                    tlist.First().Value = "@" + tlist.First().Value;
                    tlist.First().StartOffset += _t.StartOffset;
                    maincontainer.innerFuncInfor.IsInnerFuncinfor= luaAnalyzer.maincontainer.innerFuncInfor.IsInnerFuncinfor;
                    maincontainer.innerFuncInfor.funcename = tlist;
                    maincontainer.innerFuncInfor.argindex = luaAnalyzer.maincontainer.innerFuncInfor.argindex;
                }
            }
        }


        public override bool GetCompletionList(IList<ICompletionData> data, bool IsNameSpaceOpen = false)
        {
            string scope = maincontainer.currentScope;

            if (string.IsNullOrEmpty(scope)) scope = "st";

            TOKEN _t = GetToken(0);
            if(_t != null && _t.Type == TOKEN_TYPE.Special)
            {
                //lua函数
                luaAnalyzer.GetCompletionList(data, IsNameSpaceOpen);
                return true;
            }


            Container container = maincontainer;
            Container objcontainer = null;


            if (scope.IndexOf("st.O") != -1)
            {
                //string[] scopes = scope.Split('.');
                //string initscope = scopes[0] + "." + scopes[1];

                List<string> tname = new List<string>();
                tname.Add(scope.Split('.')[1].Substring(1));
                object _obj = GetObjectFromName(tname, maincontainer, FindType.Obj, scope: "st");

                if (_obj != null)
                {
                    objcontainer = (Container)_obj;
                }
            }


            switch (cursorLocation)
            {
                case CursorLocation.FunctionName:
                    data.Add(new CodeCompletionData(new KewWordItem(CompletionWordType.Variable, "onPluginStart")));
                    data.Add(new CodeCompletionData(new KewWordItem(CompletionWordType.Variable, "beforeTriggerExec")));
                    data.Add(new CodeCompletionData(new KewWordItem(CompletionWordType.Variable, "afterTriggerExec")));
                    data.Add(new CodeCompletionData(new KewWordItem(CompletionWordType.Variable, "constructor")));
                    data.Add(new CodeCompletionData(new KewWordItem(CompletionWordType.Variable, "destructor")));

                    return true;
                case CursorLocation.ImportFile:
                    //显示文件
                    if (importManager != null)
                    {
                        if (!IsNameSpaceOpen)
                        {
                            string fname = "";

                            TOKEN ctkn = GetToken(0, TOKEN.Side.Right);
                            List<TOKEN> t = tokenAnalyzer.GetTokenListFromTarget(ctkn, true);

                            List<string> strs = new List<string>();
                            for (int i = 0; i < t.Count - 1; i++)
                            {
                                strs.Add(t[i].Value);

                                fname += t[i].Value;
                                fname += ".";
                            }


                            if(fname == "")
                            {
                                foreach (var item in importManager.GetImportedFileList())
                                {
                                    data.Add(new CodeCompletionData(new ImportFileItem(CompletionWordType.nameSpace, item)));
                                }
                                return true;
                            }
                            else
                            {
                                foreach (var item in importManager.GetImportedFileList())
                                {
                                    string tstr = item;
                                    if (tstr.StartsWith(fname))
                                    {
                                        tstr = tstr.Replace(fname, "");
                                        data.Add(new CodeCompletionData(new ImportFileItem(CompletionWordType.nameSpace, tstr)));
                                    }
                                }
                                return true;
                            }
                        }
                    }
                    break;
            }

                       
            //TODO：使用分析的标记创建自动完成功能。
            if (IsNameSpaceOpen)
            {                
                TOKEN ctkn = GetToken(0, TOKEN.Side.Right);
                if(ctkn == null) return true;

                List<TOKEN> t = tokenAnalyzer.GetTokenListFromTarget(ctkn, true);
                //imported1.var1;
                //imported1.const1.object1;
                //const1.object1;
                //maincontainer.vars[0].

                //Item.cast(inven[i]).
                //在本例中，Item.cast 被发送到 t。
                //我们需要找到 Item.cast函数的返回类型。
                //t.Add(new TOKEN(0, TOKEN_TYPE.Identifier, "Item", 0));
                //t.Add(new TOKEN(0, TOKEN_TYPE.Identifier, "cast", 0));
                if (t.Count == 0) return true;


                List<string> strs = new List<string>();

                string fname = "";
                foreach (var item in t)
                {
                    strs.Add(item.Value);

                    fname += item.Value;
                    fname += ".";
                }

                string last = GetDirectText(0);
                if(cursorLocation == CursorLocation.ImportFile && importManager != null)
                {
                    bool IsImport = false;
                    List<string> filelist = importManager.GetImportedFileList(maincontainer.folderpath);
                    List<string> autocmpfilelist = new List<string>();
                    foreach (var item in filelist)
                    {
                        if (item.StartsWith(fname))
                        {
                            //如果它们匹配
                            IsImport = true;
                            autocmpfilelist.Add(item.Substring(fname.Length));
                        }
                    }
                    if (IsImport)
                    {
                        foreach (var item in autocmpfilelist)
                        {
                            data.Add(new CodeCompletionData(new ImportFileItem(CompletionWordType.nameSpace, item)));
                        }
                    }
                }
              


                GetObjectFromName(strs, container, FindType.AutoComplete, data, scope);


                //foreach (var item in t)
                //{
                //    PreCompletionData preCompletionData = new ImportFileItem(CompletionWordType.nameSpace, item.Value);
                //    data.Add(new CodeCompletionData(preCompletionData));
                //}

                return true;
            }
            switch (cursorLocation)
            {
                case CursorLocation.FunctionArgType:
                case CursorLocation.VarTypeDefine:
                    if(cursorLocation == CursorLocation.FunctionArgType)
                    {
                        foreach (var item in EpScriptDefaultCompletionData.GetCompletionKeyWordList())
                        {
                            data.Add(item);
                        } 
                    }

                    foreach (var item in container.objs)
                    {
                        data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Variable, item.mainname)));
                    }
                    foreach (var item in DefaultFuncContainer.objs)
                    {
                        data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Variable, item.mainname)));
                    }

                    return true;
                case CursorLocation.ForFuncDefine:
                    //data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "EUDLoopUnit2")));
                    //data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "EUDLoopNewUnit")));
                    //data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "UnitGroup.cploop")));
                    //data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "EUDLoopPlayer")));
                    container.GetAllItems(data, scope);
                    DefaultFuncContainer.GetAllItems(data, "st");


                    return true;
            }

            if (maincontainer.innerFuncInfor.IsInnerFuncinfor)
            {
                //参数
                Function func = (Function)GetObjectFromName(maincontainer.innerFuncInfor.funcename, maincontainer, FindType.Func, scope:scope);
                if (func != null)
                {
                    if(func.args.Count <= maincontainer.innerFuncInfor.argindex)
                    {
                        return true;
                    }
                    string argtype = func.args[maincontainer.innerFuncInfor.argindex].argtype;

                    foreach (var item in EpScriptDefaultCompletionData.GetCompletionDataList(argtype))
                    {
                        data.Add(item);
                    }
                }
            }
            else
            {
                if (base.GetCompletionList(data)) return true;
            }




            container.GetAllItems(data, scope);
            if(objcontainer != null)
            {
                data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Const, "this")));
                objcontainer.GetAllItems(data, scope);
            }
            DefaultFuncContainer.GetAllItems(data, "st");


            Container con = luaAnalyzer.GetDefaultContainer;
            foreach (var item in con.funcs.FindAll(x => "st".Contains(x.scope)))
            {
                CodeCompletionData ccdata = new CodeCompletionData(item.preCompletion);
                ccdata.preText = "@";

                data.Add(ccdata);
            }

            return true;
        }



        //Analyzer错误分析

        public override void TokenAnalyze(int caretoffset = int.MaxValue)
        {
            //TODO：词元(Token)分析逻辑 
            // 直接访问tokens进行分析。

            //分析名称空间后，添加词元(Token)。 
            // GetTokens(Context, -1) 获取并分析命名空间。

            //保存最新的命名空间并检查文件是否被修改。

            //ResetCompletionData(CompletionWordType.Function);


            //Action
            //Condiction
            //Function
            //通用函数

            //nameSpace
            //Const
            //Variable
            //对象们

            //Setting(Property)

            //ArgType
            //KeyWord
            //Special


            //需要保存函数和对象的元素。


            //光标位置 记下当前位置。
            CursorLocation cl = CursorLocation.None;


            //TOKEN clefttoken = GetToken(0, TOKEN.Side.Left);

            TOKEN ctoken = GetToken(0);
            TOKEN btoken = GetToken(-1);
            TOKEN bbtoken = GetToken(-2);

            if(ctoken != null)
            {
                if(ctoken.Type == TOKEN_TYPE.Symbol && (ctoken.Value == ";" || ctoken.Value == ")"))
                {
                    ctoken = GetToken(-1);
                    btoken = GetToken(-2);
                    bbtoken = GetToken(-3);
                }
                if (ctoken.Type == TOKEN_TYPE.KeyWord && ctoken.Value == "function")
                {
                    cl = CursorLocation.FunctionName;
                }
            }

            if (btoken != null)
            {
                if(btoken.Type == TOKEN_TYPE.KeyWord)
                {
                    switch(btoken.Value)
                    {
                        case "var":
                        case "const":
                        case "as":
                            cl = CursorLocation.VarName;
                            break;
                        case "function":
                            cl = CursorLocation.FunctionName;
                            break;
                        case "object":
                            cl = CursorLocation.ObjectDefine;
                            break;
                    }
                }
            }
            

            if(cl == CursorLocation.None)
            {
                if (ctoken != null && ctoken.Type == TOKEN_TYPE.Symbol && ctoken.Value == ":")
                {
                    cl = CursorLocation.VarTypeDefine;
                }
                if (btoken != null && btoken.Type == TOKEN_TYPE.Symbol && btoken.Value == ":")
                {
                    cl = CursorLocation.VarTypeDefine;
                }
                //if(ctoken != null)
                //{
                //    int argpos;
                //    tokenAnalyzer.GetWritedFunction(ctoken, out argpos);
                //}
            }


            cursorLocation = cl;


            //词元(Token)分析中使用的元素
            tokenAnalyzer.Init(Tokens);

            try
            {
                maincontainer = tokenAnalyzer.ConatainerAnalyzer(caretoffset);
                //RefershImportContainer(maincontainer);

                if (maincontainer.cursorLocation != CursorLocation.None)
                {
                    cursorLocation = maincontainer.cursorLocation;
                }
            }
            catch (Exception e)
            {
                TOKEN errortoken = tokenAnalyzer.GetLastToken;
                if(!(errortoken is null))
                {
                    tokenAnalyzer.ThrowException(e.ToString(), tokenAnalyzer.GetLastToken);
                }
                //tokenAnalyzer.ErrorMessage;

                //return;
            }
            if (tokenAnalyzer.IsError)
            {
                //词元(Token)解析错误
            }


            tokenAnalyzer.Complete(textEditor);
        }

        public override string GetTooiTipText(TOKEN token)
        {
            switch (token.Type)
            {
                case TOKEN_TYPE.Identifier:
                    break;
                default:
                    return "";
            }


            List<TOKEN> t = tokenAnalyzer.GetTokenListFromTarget(token, true);

            string rstr = "";
            foreach (var item in t)
            {
                if(rstr != "")
                {
                    rstr += ".";
                }

                rstr += item.Value;
            }

            object o = GetObjectFromName(t, maincontainer, FindType.All, null, token.scope);

            if(o != null)
            {
                switch (o.GetType().Name)
                {
                    case "Block":
                        Block var = (Block) o;
                        rstr = var.BlockDefine + " " + rstr;
                        if (!string.IsNullOrEmpty(var.BlockType))
                        {
                            rstr += ":" + var.BlockType;
                        }

                        if (var.Values != null && var.Values.Count != 0)
                        {
                            string v = "";
                            foreach (var item in var.Values)
                            {
                                if (v != "")
                                {
                                    //v += ".";
                                }

                                v += item.Value;
                            }

                            rstr += " = " + v;
                        }
                        if (var.IsArg)
                        {
                            rstr = "(范围) " + rstr;
                        }

                        break;
                    case "Container":
                        Container obj = (Container)o;
                        if(rstr == obj.mainname)
                        {
                            rstr = "object " + rstr;
                        }
                        else
                        {
                            rstr = rstr + ":" + obj.mainname;
                        }
                        break;
                    case "EpScriptFunction":
                        Function func = (Function)o;
                        rstr = GetFuncToolTip(t).Trim();
                        break;
                    case "ImportedNameSpace":
                        ImportedNameSpace importedNameSpace = (ImportedNameSpace)o;
                        rstr = "import " + importedNameSpace.mainname + " as " + importedNameSpace.shortname;
                        break;
                }
            }

            //Block var = null;
            //Container obj = null;
            //Function func = null;
            //ImportedNameSpace importedNameSpace = null;
            return rstr;
        }

        public override void SetCommentLine(int start, int end, string intend, CommentType commentType)
        {
            int startLine = textEditor.Document.GetLineByOffset(start).LineNumber - 1;
            int endLine = textEditor.Document.GetLineByOffset(end).LineNumber - 1;


            int minstartindexcount = int.MaxValue;

            for (int i = startLine; i <= endLine; i++)
            {
                string line = textEditor.Document.GetText(textEditor.Document.Lines[i].Offset, textEditor.Document.Lines[i].TotalLength);

                if (line.Trim() == "") continue;

                int cstartindex = line.IndexOf(line.TrimStart());
                if(cstartindex != 0)
                {
                    if (minstartindexcount > cstartindex)
                    {
                        minstartindexcount = cstartindex;
                    }
                }

            }
            if (minstartindexcount == int.MaxValue) minstartindexcount = 0;


            textEditor.Document.BeginUpdate();
            for (int i = startLine; i <= endLine; i++)
            {
                int startoffset = textEditor.Document.Lines[i].Offset;
                string line = textEditor.Document.GetText(startoffset, textEditor.Document.Lines[i].TotalLength);

                if (line.Trim() == "") continue;

                bool iscomment = line.Substring(minstartindexcount).StartsWith("//");
                switch (commentType)
                {
                    case CommentType.Set:
                        DirectInsetText("//", startoffset + minstartindexcount);
                        break;
                    case CommentType.Clear:
                        if (iscomment)
                        {
                            DirectRemoveText(2, startoffset + minstartindexcount);
                        }
                        break;
                    case CommentType.Toggle:
                        if (iscomment)
                        {
                            DirectRemoveText(2, startoffset + minstartindexcount);
                        }
                        else
                        {
                            DirectInsetText("//", startoffset + minstartindexcount);
                        }
                        break;
                }
            }
            textEditor.Document.EndUpdate();
        }
    }
}
